using System.Net;
using ESI.NET;
using EveHypernetNotification.DatabaseDocuments.Market;
using EveHypernetNotification.Services.Base;
using MongoDB.Driver;

namespace EveHypernetNotification.Services.DataCollector;

public class PersonalOrderCollectionService : TimedService
{
    private readonly EsiService _esiService;
    private readonly MongoDbService _dbService;

    // We only want to collect region order data once every hour, since it's a lot of data.
    public PersonalOrderCollectionService(WebApplication app, EsiService esiService, MongoDbService dbService) : base(app, 1200 * 1000)
    {
        _esiService = esiService;
        _dbService = dbService;
    }

    protected override async Task OnTimerElapsed()
    {
        App.Logger.LogInformation("Collecting PersonalOrder Data");
        var tokens = await _dbService.GetAllUserTokensAsync();
        await tokens.ForEachAsync(async token => {
            var esi = await _esiService.GetClientAsync(token);
            var orders = await esi.Market.CharacterOrders();
            if (orders.StatusCode == HttpStatusCode.OK)
            {
                if (orders.Data.Count > 0)
                {
                    var documents = orders.Data.Select(order => new PersonalOrderDocument(order, token.CharacterId));
                    var writes = documents.Select(document => {
                        var writeModel = new UpdateManyModel<PersonalOrderDocument>(
                            Builders<PersonalOrderDocument>.Filter.Where(orderDocument => orderDocument.OrderId == document.OrderId),
                            Builders<PersonalOrderDocument>.Update
                                .SetOnInsert(orderDocument => orderDocument.OrderId, document.OrderId)
                                .SetOnInsert(orderDocument => orderDocument.IsBuyOrder, document.IsBuyOrder)
                                .SetOnInsert(orderDocument => orderDocument.LocationId, document.LocationId)
                                .SetOnInsert(orderDocument => orderDocument.MinVolume, document.MinVolume)
                                .SetOnInsert(orderDocument => orderDocument.Duration, document.Duration)
                                .SetOnInsert(orderDocument => orderDocument.RegionId, document.RegionId)
                                .SetOnInsert(orderDocument => orderDocument.TypeId, document.TypeId)
                                .SetOnInsert(orderDocument => orderDocument.VolumeTotal, document.VolumeTotal)
                                .SetOnInsert(orderDocument => orderDocument.Issued, document.Issued)
                                .SetOnInsert(orderDocument => orderDocument.Range, document.Range)
                                .SetOnInsert(orderDocument => orderDocument.IsCorporation, document.IsCorporation)
                                .Push(orderDocument => orderDocument.OrderDetails, document.OrderDetails.Last())
                        )
                        {
                            IsUpsert = true
                        };
                        return writeModel;
                    });
                    await _dbService.PersonalOrderCollection.BulkWriteAsync(writes);
                }
            }
        });
    }
}