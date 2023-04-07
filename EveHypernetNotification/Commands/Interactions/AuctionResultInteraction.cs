using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.Services;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace EveHypernetNotification.Commands.Interactions;
[UsedImplicitly]
public class MessageClicked : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MongoDbService _db;

    public MessageClicked(MongoDbService db)
    {
        _db = db;
    }
    [UsedImplicitly]
    [ComponentInteraction("loss:*")]
    public async Task LossButton(string raffleId)
    {
        var auctions = await _db.HypernetAuctionCollection
            .FindAsync(Builders<HypernetAuctionDocument>.Filter.Eq(auction => auction.RaffleId, raffleId));

        var auction = auctions.First();

        if (auction is null)
            return;

        auction.Result = AuctionResult.Loss;
        await _db.HypernetAuctionCollection.ReplaceOneAsync(
            Builders<HypernetAuctionDocument>.Filter.Eq(x => x.RaffleId, raffleId),
            auction
        );

        var interactionCast = ((SocketMessageComponent)Context.Interaction);

        var cb = ComponentBuilder.FromComponents(interactionCast.Message.Components).DisableAllButtons();
        var eb = interactionCast.Message.Embeds.First().ToEmbedBuilder();
        eb.Title = $"{eb.Title} - Loss";
        eb.Color = Color.Orange;

        await interactionCast.UpdateAsync(properties => {
            properties.Components = cb.Build();
            properties.Embeds = new[] { eb.Build() };
        });
    }
    [UsedImplicitly]
    [ComponentInteraction("won:*")]
    public async Task WonButton(string raffleId)
    {
        var auctions = await _db.HypernetAuctionCollection
            .FindAsync(Builders<HypernetAuctionDocument>.Filter.Eq(auction => auction.RaffleId, raffleId));

        var auction = auctions.First();

        if (auction is null)
            return;

        auction.Result = AuctionResult.Won;
        await _db.HypernetAuctionCollection.ReplaceOneAsync(
            Builders<HypernetAuctionDocument>.Filter.Eq(x => x.RaffleId, raffleId),
            auction
        );

        var interactionCast = ((SocketMessageComponent)Context.Interaction);

        var cb = ComponentBuilder.FromComponents(interactionCast.Message.Components).DisableAllButtons();
        var eb = interactionCast.Message.Embeds.First().ToEmbedBuilder();
        eb.Title = $"{eb.Title} - Won";
        eb.Color = Color.Gold;

        await interactionCast.UpdateAsync(properties => {
            properties.Components = cb.Build();
            properties.Embeds = new[] { eb.Build() };
        });
    }
}