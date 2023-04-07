using System.Net;
using ESI.NET;
using EveHypernetNotification.DatabaseDocuments.Market;
using EveHypernetNotification.Services.Base;

namespace EveHypernetNotification.Services.DataCollector;

public class PriceCollectionService : TimedService
{
    private readonly EsiService _esiService;
    private readonly MongoDbService _dbService;

    public PriceCollectionService(WebApplication app, EsiService esiService, MongoDbService dbService) : base(app, 3600 * 1000)
    {
        _esiService = esiService;
        _dbService = dbService;
    }

    protected override async Task OnTimerElapsed()
    {
        App.Logger.LogInformation("Collecting Price Data");
        var esi = _esiService.GetClient();

        var orders = await esi.Market.Prices();
        if (orders.StatusCode == HttpStatusCode.OK)
        {
            if (orders.Data.Count > 0)
            {
                await _dbService.MarketPriceCollection.InsertManyAsync(orders.Data.Select(price => new MarketPriceDocument(price)));
            }
        }
        else
        {
            App.Logger.LogError("Error while collecting Price data");
        }
    }
}