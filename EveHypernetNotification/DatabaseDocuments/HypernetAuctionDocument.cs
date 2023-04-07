using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments;

public enum HyperNetAuctionStatus
{
    Created,
    Expired,
    Finished
}

public enum AuctionResult
{
    Won,
    Loss
}

[BsonIgnoreExtraElements]
public class HypernetAuctionDocument
{
    public long LocationId { get; set; }
    public long OwnerId { get; set; }
    public string RaffleId { get; set; }
    public int TicketCount { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TicketPrice { get; set; }

    public int TypeId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public HyperNetAuctionStatus Status { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal HypercoreBuyorderPrice { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal HypercoreSellorderPrice { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]

    public decimal ItemBuyorderPrice { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal ItemSellorderPrice { get; set; }

    public decimal TotalPrice => TicketCount * TicketPrice;

    [BsonRepresentation(BsonType.String)]
    public AuctionResult? Result { get; set; }

    public DateTime FirstAppeared { get; set; } = DateTime.UtcNow;

    public long CharacterId { get; set; }

    public static HypernetAuctionDocument FromDictionary(Dictionary<string, string> dictionary,
        HyperNetAuctionStatus status,
        decimal coreBuyOrder,
        decimal coreSellOrder,
        long characterId
    )
    {
        return new HypernetAuctionDocument
        {
            LocationId = long.Parse(dictionary["location_id"]),
            OwnerId = long.Parse(dictionary["owner_id"]),
            RaffleId = dictionary["raffle_id"],
            TicketCount = int.Parse(dictionary["ticket_count"]),
            TicketPrice = decimal.Parse(dictionary["ticket_price"], CultureInfo.InvariantCulture),
            TypeId = int.Parse(dictionary["type_id"]),
            Status = status,
            HypercoreBuyorderPrice = coreBuyOrder,
            HypercoreSellorderPrice = coreSellOrder,
            CharacterId = characterId
        };
    }
}