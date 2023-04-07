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
using EveHypernetNotification.Services.DataCollector;
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
        }

        public async Task MainAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            var app = builder.Build();

            app.Logger.LogInformation("Starting up...");

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
                .AddSingleton<HypernetCollectionService>()
                .AddSingleton<TransactionCollectionService>()
                .AddSingleton<RegionOrderCollectionService>()
                .AddSingleton<PersonalOrderCollectionService>()
                .AddSingleton<PriceCollectionService>()
                .AddSingleton<PersonalOrderHistoryCollectionService>()
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
                await new InteractionService(discordClient).RegisterCommandsToGuildAsync(1002206872096473138);
#endif

                await discordClient.SetGameAsync("HyperNet Auctions");
                Services.GetService<HypernetCollectionService>()!.Start();
                Services.GetService<TransactionCollectionService>()!.Start();
                // Disabled for now.
                //Services.GetService<RegionOrderCollectionService>()!.Start();
                Services.GetService<PersonalOrderCollectionService>()!.Start();
                Services.GetService<PriceCollectionService>()!.Start();
                Services.GetService<PersonalOrderHistoryCollectionService>()!.Start();
            });

            app.Logger.LogInformation("{}", Services.GetService<EsiService>()!.GetAuthUrl());

            app.MapControllers();

            await discordClient.StartAsync();
            await app.RunAsync();
        }
    }
}