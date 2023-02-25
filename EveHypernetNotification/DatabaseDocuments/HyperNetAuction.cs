using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification;

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
public class HyperNetAuction
{
    public long LocationId { get; set; }
    public long OwnerId { get; set; }
    public string RaffleId { get; set; }
    public int TicketCount { get; set; }
    public float TicketPrice { get; set; }
    public int TypeId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public HyperNetAuctionStatus Status { get; set; }

    public float HypercoreBuyorderPrice { get; set; }
    public float HypercoreSellorderPrice { get; set; }

    public float ItemBuyorderPrice { get; set; }
    public float ItemSellorderPrice { get; set; }
    public float TotalPrice => TicketCount * TicketPrice;

    [BsonRepresentation(BsonType.String)]
    public AuctionResult? Result { get; set; }
    
    public DateTime FirstAppeared { get; set; } = DateTime.UtcNow;

    public static HyperNetAuction FromDictionary(Dictionary<string, string> dictionary,
        HyperNetAuctionStatus status,
        float coreBuyOrder,
        float coreSellOrder
    )
    {
        return new HyperNetAuction
        {
            LocationId = long.Parse(dictionary["location_id"]),
            OwnerId = long.Parse(dictionary["owner_id"]),
            RaffleId = dictionary["raffle_id"],
            TicketCount = int.Parse(dictionary["ticket_count"]),
            TicketPrice = float.Parse(dictionary["ticket_price"], CultureInfo.InvariantCulture),
            TypeId = int.Parse(dictionary["type_id"]),
            Status = status,
            HypercoreBuyorderPrice = coreBuyOrder,
            HypercoreSellorderPrice = coreSellOrder
        };
    }
}