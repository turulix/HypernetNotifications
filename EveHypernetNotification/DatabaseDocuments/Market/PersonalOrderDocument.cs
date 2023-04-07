using ESI.NET.Models.Market;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments.Market;

public class PersonalOrderDetails
{

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Escrow { get; set; }

    public int VolumeRemain { get; set; }

    public DateTime UpdateDate { get; set; }
}

public class PersonalOrderDocument
{
    public int Duration { get; set; }
    public bool IsBuyOrder { get; set; }
    public bool IsCorporation { get; set; }
    public DateTime Issued { get; set; }
    public long LocationId { get; set; }
    public int MinVolume { get; set; }
    public long OrderId { get; set; }
    public string Range { get; set; }
    public int RegionId { get; set; }
    public int TypeId { get; set; }
    public int VolumeTotal { get; set; }
    public List<PersonalOrderDetails> OrderDetails { get; set; }

    public long CharacterId { get; set; }

    public bool IsActive { get; set; }

    public PersonalOrderDocument(Order order, long characterId)
    {
        CharacterId = characterId;
        Duration = order.Duration;
        IsBuyOrder = order.IsBuyOrder;
        IsCorporation = order.IsCorporation;
        Issued = order.Issued;
        LocationId = order.LocationId;
        MinVolume = order.MinVolume;
        OrderId = order.OrderId;
        Range = order.Range;
        RegionId = order.RegionId;
        TypeId = order.TypeId;
        VolumeTotal = order.VolumeTotal;
        IsActive = true;
        OrderDetails = new List<PersonalOrderDetails>
        {
            new()
            {
                Price = order.Price,
                Escrow = order.Escrow,
                VolumeRemain = order.VolumeRemain,
                UpdateDate = DateTime.UtcNow
            }
        };
    }
}