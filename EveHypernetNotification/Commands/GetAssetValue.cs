using Discord;
using Discord.Interactions;
using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.Extensions;
using EveHypernetNotification.Services;
using EveHypernetNotification.Services.DataCollector;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace EveHypernetNotification.Commands;

[UsedImplicitly]
[Group("assets", "Get asset values")]
public class GetAssetValue : InteractionModuleBase<SocketInteractionContext>
{
    private readonly HypernetCollectionService _hypernetCollectionService;
    private readonly MongoDbService _db;
    private readonly EsiService _esiService;

    public GetAssetValue(HypernetCollectionService hypernetCollectionService, MongoDbService db, EsiService esiService)
    {
        _hypernetCollectionService = hypernetCollectionService;
        _db = db;
        _esiService = esiService;
    }

    [UsedImplicitly]
    [SlashCommand("hypternet", "Get Total Value of Hypernet Auctions")]
    public async Task Hypernet()
    {
        var runningAuctions = (await _db.HypernetAuctionCollection.FindAsync(auction => auction.Status == HyperNetAuctionStatus.Created)).ToList();

        var pricePerItem = new Dictionary<int, decimal>();
        var itemCount = new Dictionary<int, int>();

        foreach (var hyperNetAuction in runningAuctions)
        {
            if (pricePerItem.ContainsKey(hyperNetAuction.TypeId))
            {
                pricePerItem[hyperNetAuction.TypeId] += hyperNetAuction.TicketPrice * hyperNetAuction.TicketCount;
                itemCount[hyperNetAuction.TypeId] += 1;
            }
            else
            {
                pricePerItem.Add(hyperNetAuction.TypeId, hyperNetAuction.TicketPrice * hyperNetAuction.TicketCount);
                itemCount.Add(hyperNetAuction.TypeId, 1);
            }
        }

        var client = _esiService.GetClient();
        var nameMap = new Dictionary<int, string>();
        foreach (var keyValuePair in pricePerItem)
        {
            var type = await client.GetCachedType(keyValuePair.Key);
            nameMap.Add(keyValuePair.Key, type.Name);
        }

        var map = pricePerItem.Select((x) => new { Name = nameMap[x.Key], Price = x.Value, Count = itemCount[x.Key] }).ToList();

        var sortedNames = map.Select(arg => arg.Name).ToList();
        sortedNames.Sort();

        var prices = sortedNames.Select(s => {
            var item = map.First(arg => arg.Name == s);
            return $"{item.Count} x {item.Name}: {Utils.FormatBigNumber(item.Price)}";
        });

        await RespondAsync(
            embed: new EmbedBuilder()
                .WithTitle("Total Value of Hypernet Auctions")
                .WithDescription(string.Join("\n", prices))
                .AddField("Total Value", $"{Utils.FormatBigNumber(pricePerItem.Values.Sum())}")
                .WithColor(Color.Green)
                .Build(),
            ephemeral: true
        );
    }

    [UsedImplicitly]
    [SlashCommand("total", "Get Total Value of all Assets")]
    public async Task Hypernet2()
    {
        await RespondAsync("Test2");
    }
}