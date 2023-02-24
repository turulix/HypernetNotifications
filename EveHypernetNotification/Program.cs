using Discord;
using Discord.Webhook;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using EveHypernetNotification;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Type = ESI.NET.Models.Universe.Type;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string state = "sakoduasud923z4723z94uuidhf8zs87fusd";


var typeCache = new Dictionary<int, Type>();

var config = Options.Create(new EsiConfig
{
    EsiUrl = "https://esi.evetech.net/",
    DataSource = DataSource.Tranquility,
    ClientId = app.Configuration["CLIENT_ID"],
    SecretKey = app.Configuration["CLIENT_SECRET"],
    CallbackUrl = app.Configuration["CALLBACK"],
    UserAgent = "Leira Saint",
});

var esiClient = new EsiClient(config);
var mongodb = new MongoClient(app.Configuration["MONGO_URL"]);
var db = mongodb.GetDatabase("EveHypernet");
var tokensCollection = db.GetCollection<OAuthTokens>("Authentication");
var hypernetCollection = db.GetCollection<HyperNetAuction>("Hypernet");

var discordClient = new DiscordWebhookClient(app.Configuration["DISCORD_WEBHOOK"]);

app.MapGet("/callback", async (httpContext) => {
    var code = httpContext.Request.Query["code"];
    if (state != httpContext.Request.Query["state"])
    {
        await Results.Text("Invalid state").ExecuteAsync(httpContext);
        return;
    }

    var sso = await esiClient.SSO.GetToken(GrantType.AuthorizationCode, code);

    var auth = await esiClient.SSO.Verify(sso);

    var token = new OAuthTokens
    {
        CharacterId = auth.CharacterID,
        AccessToken = sso.AccessToken,
        RefreshToken = sso.RefreshToken,
        ExpiresIn = sso.ExpiresIn,
        TokenType = sso.TokenType,
        CharacterName = auth.CharacterName,
        ExpiresOn = DateTime.UtcNow.AddSeconds(sso.ExpiresIn)
    };

    if (tokensCollection.Find(x => x.CharacterId == token.CharacterId).Any())
        tokensCollection.ReplaceOne(x => x.CharacterId == token.CharacterId, token);
    else
        tokensCollection.InsertOne(token);


    await Results.Text("You can now close this page.").ExecuteAsync(httpContext);
});

app.Logger.Log(LogLevel.Information, "Starting EveHypernetNotification");
app.Logger.Log(LogLevel.Information, "{}", esiClient.SSO.CreateAuthenticationUrl(new List<string>
{
    "esi-characters.read_notifications.v1"
}, state));


Task.Run(async () => {
    while (true)
    {
        try
        {
            var tokens = await tokensCollection.FindAsync(FilterDefinition<OAuthTokens>.Empty);
            await tokens.ForEachAsync(async authTokens => {
                if (authTokens.ExpiresOn < DateTime.UtcNow.AddSeconds(10))
                {
                    app.Logger.LogInformation("Refreshing Tokens");
                    var newTokens = await esiClient.SSO.GetToken(GrantType.RefreshToken, authTokens.RefreshToken);
                    authTokens.AccessToken = newTokens.AccessToken;
                    authTokens.RefreshToken = newTokens.RefreshToken;
                    authTokens.ExpiresIn = newTokens.ExpiresIn;
                    authTokens.TokenType = newTokens.TokenType;
                    authTokens.ExpiresOn = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn);
                    tokensCollection.ReplaceOne(x => x.CharacterId == authTokens.CharacterId, authTokens);
                }

                var authData = await esiClient.SSO.Verify(new SsoToken
                {
                    AccessToken = authTokens.AccessToken,
                    RefreshToken = authTokens.RefreshToken,
                    TokenType = authTokens.TokenType,
                    ExpiresIn = authTokens.ExpiresIn
                });


                //RaffleCreated
                //RaffleFinished
                //RaffleExpired
                var client = new EsiClient(config);

                var coreOrders = await client.Market.RegionOrders(10000002, MarketOrderType.All, 1, 52568);
                var openCoreOrders = coreOrders.Data
                    .Where(order => order.LocationId == 60003760).ToArray();

                var coreSellOrderPrice =
                    (float)openCoreOrders.Where(order => !order.IsBuyOrder).Min(order => order.Price);
                var coreBuyOrderPrice =
                    (float)openCoreOrders.Where(order => order.IsBuyOrder).Max(order => order.Price);

                //var q = await client.Universe.Type(11379);

                client.SetCharacterData(authData);

                var notifications = await client.Character.Notifications();

                var rafflesCreated = notifications.Data
                    .Where(notification => notification.Type == "RaffleCreated")
                    .Select(notification => notification.Text.Trim().Split("\n"))
                    .Select(strings =>
                        strings.Select(s => s.Split(": ", 2))
                            .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
                    ).Select(dictionary => HyperNetAuction.FromDictionary(dictionary, HyperNetAuctionStatus.Created,
                        coreBuyOrderPrice, coreSellOrderPrice))
                    .ToList();

                var rafflesFinished = notifications.Data
                    .Where(notification => notification.Type == "RaffleFinished")
                    .Select(notification => notification.Text.Trim().Split("\n"))
                    .Select(strings =>
                        strings.Select(s => s.Split(": ", 2))
                            .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
                    ).Select(dictionary => HyperNetAuction.FromDictionary(dictionary, HyperNetAuctionStatus.Finished,
                        coreBuyOrderPrice, coreSellOrderPrice))
                    .ToList();

                var rafflesExpired = notifications.Data
                    .Where(notification => notification.Type == "RaffleExpired")
                    .Select(notification => notification.Text.Trim().Split("\n"))
                    .Select(strings =>
                        strings.Select(s => s.Split(": ", 2))
                            .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
                    ).Select(dictionary => HyperNetAuction.FromDictionary(dictionary, HyperNetAuctionStatus.Expired,
                        coreBuyOrderPrice, coreSellOrderPrice))
                    .ToList();


                var runningAuctions = rafflesCreated
                    .ExceptByProperty(rafflesFinished, auction => auction.RaffleId)
                    .ExceptByProperty(rafflesExpired, auction => auction.RaffleId);

                var databaseDocuments = (await hypernetCollection.FindAsync(auction =>
                    runningAuctions.Select(netAuction => netAuction.RaffleId).Contains(auction.RaffleId)
                )).ToList();

                var newAuctions = runningAuctions
                    .ExceptByProperty(databaseDocuments, auction => auction.RaffleId)
                    .ToList();

                foreach (var hyperNetAuction in newAuctions)
                {
                    var itemOrders = await client.Market.RegionOrders(
                        10000002,
                        MarketOrderType.All,
                        1,
                        hyperNetAuction.TypeId
                    );
                    var openItemOrders = itemOrders.Data.Where(order => order.LocationId == 60003760).ToArray();
                    var itemSellOrderPrice =
                        (float)openItemOrders.Where(order => !order.IsBuyOrder).Min(order => order.Price);
                    var itemBuyOrderPrice =
                        (float)openItemOrders.Where(order => order.IsBuyOrder).Max(order => order.Price);

                    hyperNetAuction.ItemBuyorderPrice = itemBuyOrderPrice;
                    hyperNetAuction.ItemSellorderPrice = itemSellOrderPrice;
                }

                if (newAuctions.Count > 0)
                {
                    if (app.Configuration["SEND_CREATE"] == "true")
                    {
                        foreach (var hyperNetAuction in newAuctions)
                        {
                            await Utils.SendMessage(typeCache, hyperNetAuction, discordClient, client);
                        }
                    }

                    app.Logger.LogInformation("Inserted {} new auctions", newAuctions.Count);
                    await hypernetCollection.InsertManyAsync(newAuctions);
                }


                var openAuctions =
                    (await hypernetCollection.FindAsync(auction => auction.Status == HyperNetAuctionStatus.Created))
                    .ToList();

                var changedAuctions = new List<HyperNetAuction>();
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
                    await hypernetCollection.ReplaceOneAsync(
                        auction => auction.RaffleId == hyperNetAuction.RaffleId, hyperNetAuction
                    );
                }

                app.Logger.LogInformation("Updated status of {} auctions", changedAuctions.Count);


                foreach (var auction in changedAuctions)
                {
                    await Utils.SendMessage(typeCache, auction, discordClient, client);
                }
            });
        }
        catch (Exception e)
        {
            app.Logger.LogError(e, "Error while processing notifications");
            try
            {
                await discordClient.SendMessageAsync($"Error accured while processing notifications, {e}");
            }
            catch (Exception exception)
            {
                app.Logger.LogError(exception, "Error while sending error message");
            }
        }

        await Task.Delay(300 * 1000);
    }
});

app.Run();