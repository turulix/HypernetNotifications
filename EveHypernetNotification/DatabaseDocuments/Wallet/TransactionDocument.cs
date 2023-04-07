using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments;

[BsonIgnoreExtraElements]
public class TransactionDocument
{
    public long TransactionId { get; set; }
    public DateTime Date { get; set; }
    public int Quantity { get; set; }
    public int ClientId { get; set; }
    public bool IsBuy { get; set; }
    public bool IsPersonal { get; set; }
    public long LocationId { get; set; }
    public int TypeId { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    public long JournalRefId { get; set; }
    public long CharacterId { get; set; }

    public TransactionDocument(ESI.NET.Models.Wallet.Transaction transaction, long characterId)
    {
        TransactionId = transaction.TransactionId;
        Date = transaction.Date;
        Quantity = transaction.Quantity;
        ClientId = transaction.ClientId;
        IsBuy = transaction.IsBuy;
        IsPersonal = transaction.IsPersonal;
        LocationId = transaction.LocationId;
        TypeId = transaction.TypeId;
        UnitPrice = transaction.UnitPrice;
        JournalRefId = transaction.JournalRefId;
        CharacterId = characterId;
    }

}