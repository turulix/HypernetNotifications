using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using EveHypernetNotification;
using EveHypernetNotification.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Type = ESI.NET.Models.Universe.Type;


namespace EveHypernetNotification
{
    public class Program
    {
        public static ServiceProvider Services { get; set; }

        public static void Main(string[] args)
        {
            new Program().MainAsync(args).GetAwaiter().GetResult();

            //
            //
            // Task.Run(async () => {
            //     while (true)
            //     {
            //         try
            //         {
            //             var tokens = await tokensCollection.FindAsync(FilterDefinition<OAuthTokens>.Empty);
            //             await tokens.ForEachAsync(async authTokens => {
            //                 if (authTokens.ExpiresOn < DateTime.UtcNow.AddSeconds(10))
            //                 {
            //                     app.Logger.LogInformation("Refreshing Tokens");
            //                     var newTokens =
            //                         await esiClient.SSO.GetToken(GrantType.RefreshToken, authTokens.RefreshToken);
            //                     authTokens.AccessToken = newTokens.AccessToken;
            //                     authTokens.RefreshToken = newTokens.RefreshToken;
            //                     authTokens.ExpiresIn = newTokens.ExpiresIn;
            //                     authTokens.TokenType = newTokens.TokenType;
            //                     authTokens.ExpiresOn = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn);
            //                     tokensCollection.ReplaceOne(x => x.CharacterId == authTokens.CharacterId, authTokens);
            //                 }
            //
            //                 var authData = await esiClient.SSO.Verify(new SsoToken
            //                 {
            //                     AccessToken = authTokens.AccessToken,
            //                     RefreshToken = authTokens.RefreshToken,
            //                     TokenType = authTokens.TokenType,
            //                     ExpiresIn = authTokens.ExpiresIn
            //                 });
            //
            //
            //                 //RaffleCreated
            //                 //RaffleFinished
            //                 //RaffleExpired
            //                 var client = new EsiClient(config);
            //
            //                 var coreOrders = await client.Market.RegionOrders(10000002, MarketOrderType.All, 1, 52568);
            //                 var openCoreOrders = coreOrders.Data
            //                     .Where(order => order.LocationId == 60003760).ToArray();
            //
            //                 var coreSellOrderPrice =
            //                     (float)openCoreOrders.Where(order => !order.IsBuyOrder).Min(order => order.Price);
            //                 var coreBuyOrderPrice =
            //                     (float)openCoreOrders.Where(order => order.IsBuyOrder).Max(order => order.Price);
            //
            //                 //var q = await client.Universe.Type(11379);
            //
            //                 client.SetCharacterData(authData);
            //
            //                 var notifications = await client.Character.Notifications();
            //
            //                 var rafflesCreated = notifications.Data
            //                     .Where(notification => notification.Type == "RaffleCreated")
            //                     .Select(notification => notification.Text.Trim().Split("\n"))
            //                     .Select(strings =>
            //                         strings.Select(s => s.Split(": ", 2))
            //                             .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
            //                     ).Select(dictionary => HyperNetAuction.FromDictionary(dictionary,
            //                         HyperNetAuctionStatus.Created,
            //                         coreBuyOrderPrice, coreSellOrderPrice))
            //                     .ToList();
            //
            //                 var rafflesFinished = notifications.Data
            //                     .Where(notification => notification.Type == "RaffleFinished")
            //                     .Select(notification => notification.Text.Trim().Split("\n"))
            //                     .Select(strings =>
            //                         strings.Select(s => s.Split(": ", 2))
            //                             .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
            //                     ).Select(dictionary => HyperNetAuction.FromDictionary(dictionary,
            //                         HyperNetAuctionStatus.Finished,
            //                         coreBuyOrderPrice, coreSellOrderPrice))
            //                     .ToList();
            //
            //                 var rafflesExpired = notifications.Data
            //                     .Where(notification => notification.Type == "RaffleExpired")
            //                     .Select(notification => notification.Text.Trim().Split("\n"))
            //                     .Select(strings =>
            //                         strings.Select(s => s.Split(": ", 2))
            //                             .ToDictionary(strings1 => strings1[0], strings1 => strings1[1])
            //                     ).Select(dictionary => HyperNetAuction.FromDictionary(dictionary,
            //                         HyperNetAuctionStatus.Expired,
            //                         coreBuyOrderPrice, coreSellOrderPrice))
            //                     .ToList();
            //
            //
            //                 var runningAuctions = rafflesCreated
            //                     .ExceptByProperty(rafflesFinished, auction => auction.RaffleId)
            //                     .ExceptByProperty(rafflesExpired, auction => auction.RaffleId);
            //
            //                 var databaseDocuments = (await hypernetCollection.FindAsync(auction =>
            //                     runningAuctions.Select(netAuction => netAuction.RaffleId).Contains(auction.RaffleId)
            //                 )).ToList();
            //
            //                 var newAuctions = runningAuctions
            //                     .ExceptByProperty(databaseDocuments, auction => auction.RaffleId)
            //                     .ToList();
            //
            //                 foreach (var hyperNetAuction in newAuctions)
            //                 {
            //                     var itemOrders = await client.Market.RegionOrders(
            //                         10000002,
            //                         MarketOrderType.All,
            //                         1,
            //                         hyperNetAuction.TypeId
            //                     );
            //                     var openItemOrders = itemOrders.Data.Where(order => order.LocationId == 60003760)
            //                         .ToArray();
            //                     var itemSellOrderPrice =
            //                         (float)openItemOrders.Where(order => !order.IsBuyOrder).Min(order => order.Price);
            //                     var itemBuyOrderPrice =
            //                         (float)openItemOrders.Where(order => order.IsBuyOrder).Max(order => order.Price);
            //
            //                     hyperNetAuction.ItemBuyorderPrice = itemBuyOrderPrice;
            //                     hyperNetAuction.ItemSellorderPrice = itemSellOrderPrice;
            //                 }
            //
            //                 if (newAuctions.Count > 0)
            //                 {
            //                     if (app.Configuration["SEND_CREATE"] == "true")
            //                     {
            //                         foreach (var hyperNetAuction in newAuctions)
            //                         {
            //                             await Utils.SendMessage(typeCache, hyperNetAuction, discordClient, client);
            //                         }
            //                     }
            //
            //                     app.Logger.LogInformation("Inserted {} new auctions", newAuctions.Count);
            //                     await hypernetCollection.InsertManyAsync(newAuctions);
            //                 }
            //
            //
            //                 var openAuctions =
            //                     (await hypernetCollection.FindAsync(auction =>
            //                         auction.Status == HyperNetAuctionStatus.Created))
            //                     .ToList();
            //
            //                 var changedAuctions = new List<HyperNetAuction>();
            //                 foreach (var hyperNetAuction in openAuctions)
            //                 {
            //                     var changed = false;
            //                     if (rafflesFinished.Any(auction => auction.RaffleId == hyperNetAuction.RaffleId))
            //                     {
            //                         hyperNetAuction.Status = HyperNetAuctionStatus.Finished;
            //                         changed = true;
            //                     }
            //                     else if (rafflesExpired.Any(auction => auction.RaffleId == hyperNetAuction.RaffleId))
            //                     {
            //                         hyperNetAuction.Status = HyperNetAuctionStatus.Expired;
            //                         changed = true;
            //                     }
            //
            //                     if (!changed)
            //                         continue;
            //
            //                     changedAuctions.Add(hyperNetAuction);
            //                     await hypernetCollection.ReplaceOneAsync(
            //                         auction => auction.RaffleId == hyperNetAuction.RaffleId, hyperNetAuction
            //                     );
            //                 }
            //
            //                 app.Logger.LogInformation("Updated status of {} auctions", changedAuctions.Count);
            //
            //
            //                 foreach (var auction in changedAuctions)
            //                 {
            //                     await Utils.SendMessage(typeCache, auction, discordClient, client);
            //                 }
            //             });
            //         }
            //         catch (Exception e)
            //         {
            //             app.Logger.LogError(e, "Error while processing notifications");
            //             try
            //             {
            //                 await discordClient.SendMessageAsync($"Error accured while processing notifications, {e}");
            //             }
            //             catch (Exception exception)
            //             {
            //                 app.Logger.LogError(exception, "Error while sending error message");
            //             }
            //         }
            //
            //         await Task.Delay(300 * 1000);
            //     }
            // });
            //
            // app.Run();
        }

        public async Task MainAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            var app = builder.Build();

            var discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
            });
            await discordClient.LoginAsync(TokenType.Bot, app.Configuration["DISCORD_TOKEN"]);

            var interactionService = new InteractionService(discordClient.Rest, new InteractionServiceConfig
            {
                InteractionCustomIdDelimiters = new[] { '.' },
                AutoServiceScopes = false,
                DefaultRunMode = RunMode.Async
            });

            Services = new ServiceCollection()
                .AddSingleton(app)
                .AddSingleton(discordClient)
                .AddSingleton<MongoDbService>()
                .AddSingleton<EsiService>()
                .AddSingleton<TimedUpdateService>()
                .BuildServiceProvider();

            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

            discordClient.InteractionCreated += async interaction => {
                var ctx = new SocketInteractionContext(discordClient, interaction);
                await interactionService.ExecuteCommandAsync(ctx, Services);
            };


            discordClient.Ready += () => Task.Run(async () => {
                app.Logger.LogInformation("Discord client ready");

#if DEBUG
                await interactionService.RegisterCommandsToGuildAsync(1002206872096473138);
#else
            await interactionService.RegisterCommandsGloballyAsync();
#endif

                await discordClient.SetGameAsync("HyperNet Auctions");
                Services.GetService<TimedUpdateService>()!.Start();
            });

            app.Logger.LogInformation("{}", Services.GetService<EsiService>()!.GetAuthUrl());

            app.MapControllers();

            await discordClient.StartAsync();
            await app.RunAsync();
        }
    }
}