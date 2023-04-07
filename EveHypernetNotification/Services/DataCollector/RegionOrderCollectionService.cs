using System.Net;
using ESI.NET;
using EveHypernetNotification.DatabaseDocuments.Market;
using EveHypernetNotification.Services.Base;
using MongoDB.Driver;

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
        App.Logger.LogInformation("Collecting RegionOrder Data");
        var esi = _esiService.GetClient();
        var regions = await esi.Universe.Regions();
        var fetchTime = DateTime.UtcNow;
        var insertTasks = new List<Task>();
        var activeOrderIds = new List<long>();
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
                            activeOrderIds.AddRange(orders.Data.Select(order => order.OrderId));
                            var documents = orders.Data.Select(order => new RegionOrderDocument(order, regionId, fetchTime));
                            var writes = documents.Select(document => {
                                var writeModel = new UpdateManyModel<RegionOrderDocument>(
                                    Builders<RegionOrderDocument>.Filter.Where(orderDocument => orderDocument.OrderId == document.OrderId),
                                    Builders<RegionOrderDocument>.Update
                                        .SetOnInsert(orderDocument => orderDocument.OrderId, document.OrderId)
                                        .SetOnInsert(orderDocument => orderDocument.IsBuyOrder, document.IsBuyOrder)
                                        .SetOnInsert(orderDocument => orderDocument.LocationId, document.LocationId)
                                        .SetOnInsert(orderDocument => orderDocument.MinVolume, document.MinVolume)
                                        .SetOnInsert(orderDocument => orderDocument.Duration, document.Duration)
                                        .SetOnInsert(orderDocument => orderDocument.RegionId, document.RegionId)
                                        .SetOnInsert(orderDocument => orderDocument.SystemId, document.SystemId)
                                        .SetOnInsert(orderDocument => orderDocument.TypeId, document.TypeId)
                                        .SetOnInsert(orderDocument => orderDocument.VolumeTotal, document.VolumeTotal)
                                        .SetOnInsert(orderDocument => orderDocument.Range, document.Range)
                                        .SetOnInsert(orderDocument => orderDocument.IsActive, true)
                                        .Push(orderDocument => orderDocument.OrderDetails, document.OrderDetails.Last())
                                        .Set(orderDocument => orderDocument.Issued, document.Issued)
                                )
                                {
                                    IsUpsert = true
                                };
                                return writeModel;
                            });
                            insertTasks.Add(_dbService.RegionOrderCollection.BulkWriteAsync(writes));
                        }
                    }
                    else
                    {
                        App.Logger.LogError("Error while collecting region market data for region {regionId}", regionId);
                    }
                }
            }
        }


        await Task.WhenAll(insertTasks);

        // Deactivate orders that are no longer active
        var filter = Builders<RegionOrderDocument>.Filter.Eq(document => document.IsActive, true);
        filter &= Builders<RegionOrderDocument>.Filter.Nin(document => document.OrderId, activeOrderIds);

        var update = Builders<RegionOrderDocument>.Update.Set(document => document.IsActive, false);
        await _dbService.RegionOrderCollection.UpdateManyAsync(filter, update);

        insertTasks.Clear();
        activeOrderIds.Clear();
        App.Logger.LogInformation("Finished collecting region order data, took {time}s", (DateTime.UtcNow - fetchTime).TotalSeconds);
    }
}