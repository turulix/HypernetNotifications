using System.Net;
using ESI.NET;
using EveHypernetNotification.DatabaseDocuments.Market;
using EveHypernetNotification.Services.Base;

namespace EveHypernetNotification.Services.DataCollector;

public class RegionOrderCollectionService : TimedService
{
    private readonly EsiService _esiService;
    private readonly MongoDbService _dbService;

    // We only want to collect region order data once every hour, since it's a lot of data.
    public RegionOrderCollectionService(WebApplication app, EsiService esiService, MongoDbService dbService) : base(app, 3600 * 1000)
    {
        _esiService = esiService;
        _dbService = dbService;
    }

    protected override async Task OnTimerElapsed()
    {
        App.Logger.LogInformation("Collecting region order data");
        var esi = _esiService.GetClient();
        var regions = await esi.Universe.Regions();
        var fetchTime = DateTime.UtcNow;
        if (regions.StatusCode == HttpStatusCode.OK)
        {
            foreach (var regionId in regions.Data)
            {
                var pageCount = 1;
                for (var i = 1; i < pageCount + 1; i++)
                {
                    var orders = await esi.Market.RegionOrders(regionId, page: i);
                    pageCount = orders.Pages ?? 1;
                    if (orders.StatusCode == HttpStatusCode.OK)
                    {
                        if (orders.Data.Count > 0)
                        {
                            await _dbService.RegionOrderCollection.InsertManyAsync(orders.Data.Select(order =>
                                new RegionOrderDocument(order, regionId, fetchTime)));
                        }
                    }
                    else
                    {
                        App.Logger.LogError("Error while collecting region market data for region {regionId}", regionId);
                    }
                }
            }
        }
    }
}