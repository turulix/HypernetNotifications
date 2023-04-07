using Discord.WebSocket;
using ESI.NET;
using ESI.NET.Models.Character;
using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.Extensions;
using MongoDB.Driver;
using Timer = System.Timers.Timer;

namespace EveHypernetNotification.Services.DataCollector;

public class HypernetCollectionService
{
    private readonly EsiService _esiClient;
    private readonly MongoDbService _db;
    private readonly WebApplication _app;
    private readonly DiscordSocketClient _discord;

    private readonly Timer _hypernetTimer = new()
    {
        AutoReset = true,
        Interval = 5 * 60 * 1000
    };

    public HypernetCollectionService(EsiService esiClient, MongoDbService db, WebApplication app, DiscordSocketClient discord)
    {
        _esiClient = esiClient;
        _db = db;
        _app = app;
        _discord = discord;

        _hypernetTimer.Elapsed += async (sender, args) => { await CheckHypernetAuctions(); };
    }

    public void Start()
    {
        _hypernetTimer.Start();
    }

    public async Task CheckHypernetAuctions()
    {
        try
        {
            var tokens = await _db.TokensCollection.FindAsync(FilterDefinition<OAuthTokensDocument>.Empty);
            await tokens.ForEachAsync(async authTokens => {
                _app.Logger.LogInformation(
                    "Checking hypernet auctions for {AuthTokensCharacterName}", authTokens.CharacterName
                );

                authTokens = await _esiClient.EnsureTokenValid(authTokens);
                var authedClient = _esiClient.GetClient(await _esiClient.Verify(authTokens));

                var coreSellOrderPrice = await authedClient.GetMinSellOrderPriceAsync(52568);
                var coreBuyOrderPrice = await authedClient.GetMaxBuyOrderPriceAsync(52568);

                var notifications = await authedClient.Character.Notifications();

                var rafflesCreated = GetCreatedRaffles(notifications.Data, coreBuyOrderPrice, coreSellOrderPrice, authTokens.CharacterId);
                var rafflesFinished = GetFinishedRaffles(notifications.Data, coreBuyOrderPrice, coreSellOrderPrice, authTokens.CharacterId);
                var rafflesExpired = GetExpiredRaffles(notifications.Data, coreBuyOrderPrice, coreSellOrderPrice, authTokens.CharacterId);

                var runningAuctions = rafflesCreated
                    .ExceptByProperty(rafflesFinished, auction => auction.RaffleId)
                    .ExceptByProperty(rafflesExpired, auction => auction.RaffleId);

                var databaseDocuments = (await _db.HypernetAuctionCollection.FindAsync(auction =>
                    runningAuctions.Select(netAuction => netAuction.RaffleId).Contains(auction.RaffleId)
                )).ToList();

                var newAuctions = runningAuctions
                    .ExceptByProperty(databaseDocuments, auction => auction.RaffleId)
                    .ToList();

                foreach (var hyperNetAuction in newAuctions)
                {
                    hyperNetAuction.ItemBuyorderPrice = await authedClient.GetMaxBuyOrderPriceAsync(hyperNetAuction.TypeId);
                    hyperNetAuction.ItemSellorderPrice = await authedClient.GetMinSellOrderPriceAsync(hyperNetAuction.TypeId);
                    hyperNetAuction.FirstAppeared = DateTime.UtcNow;
                }

                if (newAuctions.Count > 0)
                {
                    if (_app.Configuration["SEND_CREATE"] == "true")
                    {
                        foreach (var hyperNetAuction in newAuctions)
                        {
                            await SendMessage(authTokens, hyperNetAuction, authedClient);
                        }
                    }

                    _app.Logger.LogInformation("Inserted {} new auctions", newAuctions.Count);
                    await _db.HypernetAuctionCollection.InsertManyAsync(newAuctions);
                }

                var openAuctions = (await _db.HypernetAuctionCollection.FindAsync(auction => auction.Status == HyperNetAuctionStatus.Created)).ToList();
                var changedAuctions = new List<HypernetAuctionDocument>();
                foreach (var hyperNetAuction in openAuctions)
                {
                    var changed = false;
                    if (rafflesFinished.Any(auction => auction.RaffleId == hyperNetAuction.RaffleId))
                    {
                        hyperNetAuction.Status = HyperNetAuctionStatus.Finished;
                        changed = true;
                    }
                    else if (rafflesExpired.Any(auction => auction.RaffleId == hyperNetAuction.RaffleId))
                    {
                        hyperNetAuction.Status = HyperNetAuctionStatus.Expired;
                        changed = true;
                    }

                    if (!changed)
                        continue;

                    changedAuctions.Add(hyperNetAuction);
                    await _db.HypernetAuctionCollection.ReplaceOneAsync(
                        auction => auction.RaffleId == hyperNetAuction.RaffleId, hyperNetAuction
                    );
                }

                _app.Logger.LogInformation("Updated status of {} auctions", changedAuctions.Count);
                foreach (var auction in changedAuctions)
                {
                    await SendMessage(authTokens, auction, authedClient);
                }
            });
        }
        catch (Exception exception)
        {
            _app.Logger.LogError(exception, "Error while checking hypernet auctions");
        }
    }

    private async Task SendMessage(OAuthTokensDocument authTokensDocument, HypernetAuctionDocument hypernetAuctionDocument, EsiClient authedClient)
    {
        if (_discord.GetGuild(authTokensDocument.GuildId) == null)
        {
            var guild = await _discord.Rest.GetGuildAsync(authTokensDocument.GuildId);
            var textChannel = await guild.GetTextChannelAsync(authTokensDocument.ChannelId);
            await textChannel.SendMessageAsync(
                embed: await Utils.GetHypernetMessageEmbedAsync(hypernetAuctionDocument, authedClient),
                components: Utils.GetComponents(hypernetAuctionDocument)
            );
        }
        else
        {
            var guild = _discord.GetGuild(authTokensDocument.GuildId);
            var textChannel = guild.GetTextChannel(authTokensDocument.ChannelId);
            await textChannel.SendMessageAsync(
                embed: await Utils.GetHypernetMessageEmbedAsync(hypernetAuctionDocument, authedClient),
                components: Utils.GetComponents(hypernetAuctionDocument)
            );
        }
    }

    #region Raffle Parsing

    private List<HypernetAuctionDocument> GetCreatedRaffles(
        IEnumerable<Notification> notificationsData,
        decimal coreBuyOrderPrice,
        decimal coreSellOrderPrice,
        long characterId
    )
    {
        return notificationsData
            .Where(notification => notification.Type == "RaffleCreated")
            .Select(notification => notification.Text.Trim().Split("\n"))
            .Select(strings =>
                strings.Select(s => s.Split(": ", 2))
                    .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
            ).Select(dictionary => HypernetAuctionDocument.FromDictionary(dictionary,
                HyperNetAuctionStatus.Created,
                coreBuyOrderPrice, coreSellOrderPrice, characterId))
            .ToList();
    }

    private List<HypernetAuctionDocument> GetFinishedRaffles(
        IEnumerable<Notification> notificationsData,
        decimal coreBuyOrderPrice,
        decimal coreSellOrderPrice,
        long characterId
    )
    {
        return notificationsData
            .Where(notification => notification.Type == "RaffleFinished")
            .Select(notification => notification.Text.Trim().Split("\n"))
            .Select(strings =>
                strings.Select(s => s.Split(": ", 2))
                    .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
            ).Select(dictionary => HypernetAuctionDocument.FromDictionary(dictionary,
                HyperNetAuctionStatus.Finished,
                coreBuyOrderPrice, coreSellOrderPrice, characterId))
            .ToList();
    }

    private List<HypernetAuctionDocument> GetExpiredRaffles(
        IEnumerable<Notification> notificationsData,
        decimal coreBuyOrderPrice,
        decimal coreSellOrderPrice,
        long characterId
    )
    {
        return notificationsData
            .Where(notification => notification.Type == "RaffleExpired")
            .Select(notification => notification.Text.Trim().Split("\n"))
            .Select(strings =>
                strings.Select(s => s.Split(": ", 2))
                    .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
            ).Select(dictionary => HypernetAuctionDocument.FromDictionary(dictionary,
                HyperNetAuctionStatus.Expired,
                coreBuyOrderPrice, coreSellOrderPrice, characterId))
            .ToList();
    }

    #endregion

}