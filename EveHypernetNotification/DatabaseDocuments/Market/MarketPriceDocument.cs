using ESI.NET.Models.Market;

namespace EveHypernetNotification.DatabaseDocuments.Market;

public class MarketPriceDocument
{
    public int TypeId { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public DateTime FetchDate { get; set; }


    public MarketPriceDocument(Price price)
    {
        TypeId = price.TypeId;
        AveragePrice = price.AveragePrice;
        AdjustedPrice = price.AdjustedPrice;
        FetchDate = DateTime.UtcNow;
    }
}