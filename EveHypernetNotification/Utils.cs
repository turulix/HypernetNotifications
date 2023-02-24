using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Webhook;
using ESI.NET;
using Type = ESI.NET.Models.Universe.Type;

namespace EveHypernetNotification;

public static class Utils
{
    public static string FormatBigNumber(float num)
    {
        return num == 0 ? "O" : num.ToString("##,##.##");
    }

    public static float EstimateCoresNeeded(float minPrice, float maxPrice, float totalPrice)
    {
        var coresNeeded = totalPrice * 0.05f / ((minPrice + maxPrice) / 2);
        if (coresNeeded < 1)
            coresNeeded = 1;
        return (float)Math.Floor(coresNeeded);
    }

    public static async Task SendMessage(Dictionary<int, Type> typeCache, HyperNetAuction auction,
        DiscordWebhookClient discordClient, EsiClient client)
    {
        if (!typeCache.TryGetValue(auction.TypeId, out var itemType))
        {
            var item = await client.Universe.Type(auction.TypeId);
            typeCache.Add(auction.TypeId, item.Data);
            itemType = item.Data;
        }

        await discordClient.SendMessageAsync(
            embeds: new[]
            {
                new EmbedBuilder()
                    .WithTitle($"Hypernet Auction {auction.Status}")
                    .WithDescription(
                        $"Hypernet Auction changed status to {auction.Status}"
                    )
                    .WithThumbnailUrl($"https://images.evetech.net/types/{itemType.TypeId}/icon")
                    .WithColor(auction.Status switch
                    {
                        HyperNetAuctionStatus.Created => Color.Blue,
                        HyperNetAuctionStatus.Finished => Color.Green,
                        HyperNetAuctionStatus.Expired => Color.Red,
                        _ => Color.Default
                    })
                    .AddField("Item", itemType.Name, true)
                    .AddField("Marked Value (Sell)", FormatBigNumber(auction.ItemSellorderPrice), true)
                    .AddField("Marked Value (Buy)", FormatBigNumber(auction.ItemBuyorderPrice), true)
                    .AddField("Ticket Count", auction.TicketCount, true)
                    .AddField("Ticket Price", FormatBigNumber(auction.TicketPrice), true)
                    .AddField("Payout",
                        FormatBigNumber(auction.TotalPrice * 0.95f), true)
                    .AddField("Estimated Profit (Win)",
                        FormatBigNumber(auction.TotalPrice / 2f -
                                        auction.TotalPrice * 0.05f -
                                        EstimateCoresNeeded(
                                            auction.HypercoreBuyorderPrice,
                                            auction.HypercoreSellorderPrice,
                                            auction.TotalPrice
                                        ) *
                                        auction.HypercoreSellorderPrice),
                        true
                    )
                    .AddField("Estimated Profit (Loss)",
                        FormatBigNumber(-auction.ItemSellorderPrice +
                                        auction.TotalPrice / 2f -
                                        auction.TotalPrice * 0.05f -
                                        EstimateCoresNeeded(
                                            auction.HypercoreBuyorderPrice,
                                            auction.HypercoreSellorderPrice,
                                            auction.TotalPrice
                                        ) *
                                        auction.HypercoreSellorderPrice),
                        true
                    )
                    .Build()
            }
        );
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