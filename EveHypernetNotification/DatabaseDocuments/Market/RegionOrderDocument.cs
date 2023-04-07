using ESI.NET.Models.Market;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments.Market;

public class RegionalOrderDetails
{
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    public int VolumeRemain { get; set; }

    public DateTime UpdateDate { get; set; }
}

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

    public string Range { get; set; }

    public long SystemId { get; set; }

    public int TypeId { get; set; }

    public int VolumeTotal { get; set; }

    public List<RegionalOrderDetails> OrderDetails { get; set; }

    public bool IsActive { get; set; }


    public RegionOrderDocument(Order order, int regionId, DateTime fetchDate)
    {
        RegionId = regionId;
        Duration = order.Duration;
        IsBuyOrder = order.IsBuyOrder;
        Issued = order.Issued;
        LocationId = order.LocationId;
        MinVolume = order.MinVolume;
        OrderId = order.OrderId;
        Range = order.Range;
        SystemId = order.SystemId;
        TypeId = order.TypeId;
        VolumeTotal = order.VolumeTotal;
        IsActive = true;
        OrderDetails = new List<RegionalOrderDetails>
        {
            new()
            {
                Price = order.Price,
                VolumeRemain = order.VolumeRemain,
                UpdateDate = fetchDate
            }
        };
    }
}