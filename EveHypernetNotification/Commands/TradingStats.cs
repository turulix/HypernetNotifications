using Discord;
using Discord.Interactions;
using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.DatabaseDocuments.Market;
using EveHypernetNotification.Services;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace EveHypernetNotification.Commands;

[UsedImplicitly]
[Group("trade", "Get trade stats")]
public class TradingStats : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MongoDbService _dbService;
    private readonly EsiService _esiService;

    public TradingStats(MongoDbService dbService, EsiService esiService)
    {
        _dbService = dbService;
        _esiService = esiService;
    }

    [UsedImplicitly]
    [SlashCommand("stats", "Get trade stats for a specific item")]
    public async Task RecheckNow(
        [Summary("item")] int typeId
    )
    {
        const decimal tradeFee = 0.036m;
        const decimal brokerFee = 0.0148m;

        var transactionFilter = Builders<TransactionDocument>.Filter.Eq(document => document.CharacterId, 2114942916);
        transactionFilter &= Builders<TransactionDocument>.Filter.Eq(document => document.TypeId, typeId);
        transactionFilter &= Builders<TransactionDocument>.Filter.Gt(document => document.Date, DateTime.UtcNow - TimeSpan.FromDays(30));

        var activeOrdersFilter = Builders<PersonalOrderDocument>.Filter.Eq(document => document.CharacterId, 2114942916);
        activeOrdersFilter &= Builders<PersonalOrderDocument>.Filter.Eq(document => document.TypeId, typeId);
        activeOrdersFilter &= Builders<PersonalOrderDocument>.Filter.Eq(document => document.IsActive, true);
        activeOrdersFilter &= Builders<PersonalOrderDocument>.Filter.Eq(document => document.IsBuyOrder, false);

        var typeDetails = await _esiService.GetTypeDetailsAsync(typeId);
        if (typeDetails == null)
        {
            await Context.Interaction.RespondAsync("Could not find type details", ephemeral: true);
            return;
        }

        var transactionData = await _dbService.TransactionsCollection.FindAsync<TransactionDocument>(transactionFilter);
        var activeOrders = await _dbService.PersonalOrderCollection.FindAsync<PersonalOrderDocument>(activeOrdersFilter);
        var transactions = transactionData.ToList();
        var activeSellOrders = activeOrders.ToList();

        var quantityInSellOrders = activeSellOrders.Sum(order => {
            var lastStatus = order.OrderDetails.Last();
            return lastStatus.VolumeRemain;
        });

        var amountInSellOrders = activeSellOrders.Sum(order => {
            var lastStatus = order.OrderDetails.Last();
            return lastStatus.VolumeRemain * lastStatus.Price;
        });

        var totalBrokerFee = activeSellOrders.Sum(transaction => {
            var initialFee = transaction.VolumeTotal * transaction.OrderDetails.First().Price * brokerFee;
            if (transaction.OrderDetails.Count == 1)
                return initialFee;
            for (var i = 1; i < transaction.OrderDetails.Count; i++)
            {
                var currentOrder = transaction.OrderDetails[i];
                var previousOrder = transaction.OrderDetails[i - 1];

                var currentOrderValue = currentOrder.Price * currentOrder.VolumeRemain;
                var previousOrderValue = previousOrder.Price * previousOrder.VolumeRemain;
                if (currentOrder.Price != previousOrder.Price)
                {
                    initialFee += (0.5m - 0.06m * 5m) * brokerFee * currentOrderValue + brokerFee * decimal.Max(previousOrderValue - currentOrderValue, 0);
                }
            }

            return initialFee;
        });

        var buyOrders = transactions.Where(transaction => transaction.IsBuy).ToList();
        var sellOrders = transactions.Where(transaction => !transaction.IsBuy).ToList();

        var totalAmountBought = buyOrders.Sum(transaction => transaction.UnitPrice * transaction.Quantity);
        var totalAmountSold = sellOrders.Sum(transaction => transaction.UnitPrice * transaction.Quantity);

        var totalQuantityBought = buyOrders.Sum(transaction => transaction.Quantity);
        var totalQuantitySold = sellOrders.Sum(transaction => transaction.Quantity);

        var averageBuyPrice = totalAmountBought / totalQuantityBought;
        var averageSellPrice = totalAmountSold / totalQuantitySold;
        var averageProfit = averageSellPrice - averageBuyPrice;

        var estimatedProfit = -totalAmountBought + (totalAmountSold + amountInSellOrders) * (1 - tradeFee);

        var embed = new EmbedBuilder()
            .WithTitle($"Transaction stats for {typeDetails.Name} (Last 30 Days)")
            .WithColor(Color.Green)
            .WithThumbnailUrl($"https://images.evetech.net/types/{typeDetails.TypeId}/icon")
            .AddField("Item", $"{typeDetails.Name}", false)
            .AddField("Quantity Bought", $"{totalQuantityBought:N0}", true)
            .AddField("Quantity Sold", $"{totalQuantitySold:N0}", true)
            .AddField("In Orders", $"{quantityInSellOrders:N0}", true)
            .AddField("Total Amount Bought", $"{totalAmountBought:N2} ISK", true)
            .AddField("Total Amount Sold", $"{totalAmountSold:N2} ISK", true)
            .AddField("Total In Orders", $"{amountInSellOrders:N2} ISK", true)
            .AddField("Average Buy Price", $"{averageBuyPrice:N2} ISK", true)
            .AddField("Average Sell Price", $"{averageSellPrice:N2} ISK", true)
            .AddField("Average Profit", $"{averageProfit:N2} ISK", true)
            .AddField("Total Profit", $"{totalAmountSold * (1 - tradeFee) - totalAmountBought:N2} ISK", true)
            .AddField("Est. Total Profit", $"{estimatedProfit:N2} ISK", true)
            .AddField("Est. Broker Fees", $"{totalBrokerFee:N2}", true)
            .WithFooter("Data might be up to 20 minutes old")
            .Build();

        await Context.Interaction.RespondAsync(embeds: new[] { embed }, ephemeral: true);
    }
}