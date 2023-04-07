using ESI.NET.Models.Market;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments.Market;

[BsonIgnoreExtraElements]
public class RegionOrderDocument
{

    public int RegionId { get; set; }

    public int Duration { get; set; }

    public bool IsBuyOrder { get; set; }

    public DateTime Issued { get; set; }

    public long LocationId { get; set; }

    public int MinVolume { get; set; }

    public long OrderId { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    public string Range { get; set; }

    public long SystemId { get; set; }

    public int TypeId { get; set; }

    public int VolumeRemain { get; set; }

    public int VolumeTotal { get; set; }

    public DateTime FetchDate { get; set; }

    public RegionOrderDocument(Order order, int regionId, DateTime fetchDate)
    {
        RegionId = regionId;
        Duration = order.Duration;
        IsBuyOrder = order.IsBuyOrder;
        Issued = order.Issued;
        LocationId = order.LocationId;
        MinVolume = order.MinVolume;
        OrderId = order.OrderId;
        Price = order.Price;
        Range = order.Range;
        SystemId = order.SystemId;
        TypeId = order.TypeId;
        VolumeRemain = order.VolumeRemain;
        VolumeTotal = order.VolumeTotal;
        FetchDate = fetchDate;
    }
}