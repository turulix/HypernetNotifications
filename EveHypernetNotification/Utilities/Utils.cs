using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Webhook;
using ESI.NET;
using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.Extensions;
using Type = ESI.NET.Models.Universe.Type;

namespace EveHypernetNotification;

public static class Utils
{
    public static string FormatBigNumber(decimal num)
    {
        return num == 0 ? "O" : num.ToString("##,##.##");
    }

    public static decimal EstimateCoresNeeded(decimal minPrice, decimal maxPrice, decimal totalPrice)
    {
        var coresNeeded = totalPrice * 0.05m / ((minPrice + maxPrice) / 2);
        if (coresNeeded < 1)
            coresNeeded = 1;
        return Math.Floor(coresNeeded);
    }

    public static async Task<Embed> GetHypernetMessageEmbedAsync(HypernetAuctionDocument auctionDocument, EsiClient client)
    {
        var itemType = await client.GetCachedType(auctionDocument.TypeId);

        return new EmbedBuilder()
            .WithTitle($"Hypernet Auction {auctionDocument.Status}")
            .WithDescription(
                $"Hypernet Auction changed status to {auctionDocument.Status}"
            )
            .WithThumbnailUrl($"https://images.evetech.net/types/{itemType.TypeId}/icon")
            .WithColor(auctionDocument.Status switch
            {
                HyperNetAuctionStatus.Created => Color.Blue,
                HyperNetAuctionStatus.Finished => Color.Green,
                HyperNetAuctionStatus.Expired => Color.Red,
                _ => Color.Default
            })
            .AddField("Item", itemType.Name, true)
            .AddField("Marked Value (Sell)", FormatBigNumber(auctionDocument.ItemSellorderPrice), true)
            .AddField("Marked Value (Buy)", FormatBigNumber(auctionDocument.ItemBuyorderPrice), true)
            .AddField("Ticket Count", auctionDocument.TicketCount, true)
            .AddField("Ticket Price", FormatBigNumber(auctionDocument.TicketPrice), true)
            .AddField("Payout",
                FormatBigNumber(auctionDocument.TotalPrice * 0.95m), true)
            .AddField("Estimated Profit (Win)",
                FormatBigNumber(auctionDocument.TotalPrice / 2m -
                                auctionDocument.TotalPrice * 0.05m -
                                EstimateCoresNeeded(
                                    auctionDocument.HypercoreBuyorderPrice,
                                    auctionDocument.HypercoreSellorderPrice,
                                    auctionDocument.TotalPrice
                                ) *
                                auctionDocument.HypercoreSellorderPrice),
                true
            )
            .AddField("Estimated Profit (Loss)",
                FormatBigNumber(-auctionDocument.ItemSellorderPrice +
                                auctionDocument.TotalPrice / 2m -
                                auctionDocument.TotalPrice * 0.05m -
                                EstimateCoresNeeded(
                                    auctionDocument.HypercoreBuyorderPrice,
                                    auctionDocument.HypercoreSellorderPrice,
                                    auctionDocument.TotalPrice
                                ) *
                                auctionDocument.HypercoreSellorderPrice),
                true
            )
            .WithFooter($"RaffleID: {auctionDocument.RaffleId}")
            .Build();
    }

    public static MessageComponent GetComponents(HypernetAuctionDocument hypernetAuctionDocument)
    {
        if (hypernetAuctionDocument.Status is HyperNetAuctionStatus.Created or HyperNetAuctionStatus.Expired)
            return new ComponentBuilder().Build();
        return new ComponentBuilder()
            .WithButton("Won Auction", $"won:{hypernetAuctionDocument.RaffleId}", style: ButtonStyle.Success)
            .WithButton("Lost Auction", $"loss:{hypernetAuctionDocument.RaffleId}", style: ButtonStyle.Danger)
            .Build();
    }

    public static ComponentBuilder DisableAllButtons(this ComponentBuilder cb)
    {
        var newComponentBuilder = new ComponentBuilder();

        foreach (var actionRowBuilder in cb.ActionRows)
        {
            var newRow = new ActionRowBuilder();
            foreach (var messageComponent in actionRowBuilder.Components)
            {
                if (messageComponent.Type == ComponentType.Button)
                {
                    var button = (ButtonComponent)messageComponent;
                    button = button.ToBuilder().WithDisabled(true).Build();
                    newRow.AddComponent(button);
                    continue;
                }

                newRow.AddComponent(messageComponent);
            }

            newComponentBuilder.AddRow(newRow);
        }

        return newComponentBuilder;
    }

}

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> ExceptByProperty<TSource, TProperty>(this IEnumerable<TSource> first,
        IEnumerable<TSource> second, Func<TSource, TProperty> keySelector)
    {
        return first.ExceptBy(second, x => x, GenericComparer<TSource, TProperty>.Comparer(keySelector));
    }

}

public sealed class GenericComparer<T, TProperty> : IEqualityComparer<T>
{
    public static IEqualityComparer<T> Comparer(Func<T, TProperty> selector)
    {
        return new GenericComparer<T, TProperty>(selector);
    }

    private readonly Func<T, TProperty> selector;

    public GenericComparer(Func<T, TProperty> selector)
    {
        this.selector = selector;
    }

    public bool Equals(T? x, T? y)
    {
        if (x == null || y == null) return false;

        return Equals(selector(x), selector(y));
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        object? value = selector(obj);

        if (value == null) return obj.GetHashCode();

        return value.GetHashCode();
    }
}