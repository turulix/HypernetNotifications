using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using ESI.NET;
using ESI.NET.Models.Market;
using EveHypernetNotification.DatabaseDocuments.Market;
using EveHypernetNotification.Services.Base;
using EveHypernetNotification.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EveHypernetNotification.Services.DataCollector;

public class RegionOrderCollectionService : TimedService
{
    private readonly EsiService _esiService;
    private readonly MongoDbService _dbService;

    // We only want to collect region order data once every hour, since it's a lot of data.
    public RegionOrderCollectionService(WebApplication app, EsiService esiService, MongoDbService dbService) : base(app, 600 * 1000)
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

        var allOrders = Array.Empty<RegionOrderDocument>();

        if (regions.StatusCode == HttpStatusCode.OK)
        {
            var regionTasks = new Task<(int, Order[])>[regions.Data.Length];
            for (var i = 0; i < regions.Data.Length; i++)
            {
                regionTasks[i] = DownloadRegion(regions.Data[i], esi);
            }

            await Task.WhenAll(regionTasks);

            allOrders = regionTasks
                .Select(task => (task.Result.Item1, task.Result.Item2))
                .Select(region => region.Item2.Select(order => new RegionOrderDocument(order, region.Item1, DateTime.UtcNow)).ToArray())
                .ToArray()
                .SelectMany(documents => documents)
                .ToArray();
        }

        App.Logger.LogInformation("Finished collecting region order data");
        
        var updates = new ConcurrentStack<UpdateOneModel<RegionOrderDocument>>();
        var renderTasks = new Task[allOrders.Length];
        for (var i = 0; i < allOrders.Length; i++)
        {
            var i1 = i;
            renderTasks[i] = Task.Run(() => { updates.Push(ConvertToWriteModel(allOrders[i1])); });
        }

        await Task.WhenAll(renderTasks);
        
        await _dbService.RegionOrderCollection.BulkWriteAsync(updates, new BulkWriteOptions
        {
            IsOrdered = false
        });

        App.Logger.LogInformation("Finished downloading region order data, took {Time}s | Total Orders: {}",
            (DateTime.UtcNow - fetchTime).TotalSeconds,
            allOrders.Length
        );
    }

    private UpdateOneModel<RegionOrderDocument> ConvertToWriteModel(RegionOrderDocument order)
    {
        var filter = new BsonDocument
        {
            { "OrderId", order.OrderId },
            { "RegionId", order.RegionId },
            { "IsActive", true }
        };

        var update = new BsonDocument
        {
            {
                "$set", new BsonDocument
                {
                    { "Issued", order.Issued }
                }
            },
            {
                "$push", new BsonDocument
                {
                    {
                        "OrderDetails", new BsonDocument
                        {
                            { "Price", order.OrderDetails.Last().Price },
                            { "Volume", order.OrderDetails.Last().VolumeRemain },
                            { "UpdateDate", order.OrderDetails.Last().UpdateDate }
                        }
                    }
                }
            },
            {
                "$setOnInsert", new BsonDocument
                {
                    { "RegionId", order.RegionId },
                    { "Duration", order.Duration },
                    { "IsBuyOrder", order.IsBuyOrder },
                    { "LocationId", order.LocationId },
                    { "MinVolume", order.MinVolume },
                    { "OrderId", order.OrderId },
                    { "Range", order.Range },
                    { "SystemId", order.SystemId },
                    { "TypeId", order.TypeId },
                    { "VolumeTotal", order.VolumeTotal },
                    { "IsActive", true }
                }
            }
        };

        // var update = Builders<RegionOrderDocument>.Update
        //     .Set(document => document.Issued, order.Issued)
        //     .Push(document => document.OrderDetails,
        //         order.OrderDetails.Last()
        //     );
        //
        // order.GetType().GetProperties().Where(property => property.Name != "OrderDetails" && property.Name != "Issued").ToList().ForEach(property => {
        //     update = update.SetOnInsert(property.Name, property.GetValue(order));
        // });

        return new UpdateOneModel<RegionOrderDocument>(filter, update) { IsUpsert = true };
    }

    private async Task<(int, Order[])> DownloadRegion(int region, IEsiClient esi)
    {
        var pageCount = (await esi.Market.RegionOrders(region)).Pages ?? 1;
        var pageTasks = new Task<Order[]>[pageCount];
        for (var i = 0; i < pageCount; i++)
        {
            pageTasks[i] = DownloadPage(region, i + 1, esi);
        }

        await Task.WhenAll(pageTasks);

        var orders = new Order[pageCount * 1000];
        var index = 0;
        foreach (var page in pageTasks)
        {
            var pageOrders = await page;
            pageOrders.CopyTo(orders, index);
            index += pageOrders.Length;
        }

        Array.Resize(ref orders, index);

        return (region, orders);
    }

    private async Task<Order[]> DownloadPage(int region, int page, IEsiClient esi)
    {
        var data = await esi.Market.RegionOrders(region, page: page);
        if (data.StatusCode == HttpStatusCode.OK)
        {
            return data.Data.Count <= 0 ? Array.Empty<Order>() : data.Data.ToArray();
        }

        App.Logger.LogError("Error while collecting region market data for region {RegionId} on {} | {}", region, page, data.StatusCode);
        return Array.Empty<Order>();
    }
}