using ESI.NET.Models.Market;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments.Market;

[BsonIgnoreExtraElements]
public class PersonalOrderHistoryDocument
{
    public int Duration { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Escrow { get; set; }

    public bool IsBuyOrder { get; set; }
    public bool IsCorporation { get; set; }
    public DateTime Issued { get; set; }
    public long LocationId { get; set; }
    public int MinVolume { get; set; }
    public long OrderId { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    public string Range { get; set; }
    public long RegionId { get; set; }
    public string State { get; set; }
    public int TypeId { get; set; }
    public int VolumeRemain { get; set; }
    public int VolumeTotal { get; set; }
    public long CharacterId { get; set; }

    public PersonalOrderHistoryDocument(Order order, long characterId)
    {
        CharacterId = characterId;
        Duration = order.Duration;
        Escrow = order.Escrow;
        IsBuyOrder = order.IsBuyOrder;
        IsCorporation = order.IsCorporation;
        Issued = order.Issued;
        LocationId = order.LocationId;
        MinVolume = order.MinVolume;
        OrderId = order.OrderId;
        Price = order.Price;
        Range = order.Range;
        RegionId = order.RegionId;
        State = order.State;
        TypeId = order.TypeId;
        VolumeRemain = order.VolumeRemain;
        VolumeTotal = order.VolumeTotal;
    }

}