using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments;

[BsonIgnoreExtraElements]
public class JournalEntryDocument
{
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Balance { get; set; }

    public long JournalEntryId { get; set; }

    public DateTime Date { get; set; }
    public string Reason { get; set; }
    public string Description { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Tax { get; set; }

    public long ContextId { get; set; }
    public string RefType { get; set; }
    public string ContextIdType { get; set; }
    public int FirstPartyId { get; set; }
    public int SecondPartyId { get; set; }
    public int TaxReceiverId { get; set; }
    public long CharacterId { get; set; }

    public JournalEntryDocument(ESI.NET.Models.Wallet.JournalEntry journalEntry, long characterId)
    {
        Amount = journalEntry.Amount;
        Balance = journalEntry.Balance;
        JournalEntryId = journalEntry.Id;
        Date = journalEntry.Date;
        Reason = journalEntry.Reason;
        Description = journalEntry.Description;
        Tax = journalEntry.Tax;
        ContextId = journalEntry.ContextId;
        RefType = journalEntry.RefType;
        ContextIdType = journalEntry.ContextIdType;
        FirstPartyId = journalEntry.FirstPartyId;
        SecondPartyId = journalEntry.SecondPartyId;
        TaxReceiverId = journalEntry.TaxReceiverId;
        CharacterId = characterId;
    }

}