using ESI.NET;
using ESI.NET.Enumerations;
using Type = ESI.NET.Models.Universe.Type;

namespace EveHypernetNotification.Extensions;

public static class EsiClientExtension
{
    private static readonly Dictionary<int, Type> _typeCache = new();

    public static async Task<decimal> GetMaxBuyOrderPriceAsync(
        this EsiClient client,
        int typeId,
        long locationId = 60003760,
        int regionId = 10000002
    )
    {
        var orders = await client.Market.RegionOrders(regionId, MarketOrderType.Buy, 1, typeId);
        return orders.Data.Where(order => order.LocationId == locationId).Max(order => order.Price);
    }

    public static async Task<decimal> GetMinSellOrderPriceAsync(
        this EsiClient client,
        int typeId,
        long locationId = 60003760,
        int regionId = 10000002
    )
    {
        var orders = await client.Market.RegionOrders(regionId, MarketOrderType.Sell, 1, typeId);
        return orders.Data.Where(order => order.LocationId == locationId).Min(order => order.Price);
    }

    public static async Task<Type> GetCachedType(this EsiClient client, int typeId)
    {
        if (_typeCache.TryGetValue(typeId, out var value))
            return value;
        var type = await client.Universe.Type(typeId);
        _typeCache.Add(typeId, type.Data);
        return type.Data;
    }
}