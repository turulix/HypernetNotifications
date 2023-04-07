using System.Net;
using ESI.NET;
using EveHypernetNotification.DatabaseDocuments.Market;
using EveHypernetNotification.Services.Base;
using MongoDB.Driver;

namespace EveHypernetNotification.Services.DataCollector;

public class PersonalOrderHistoryCollectionService : TimedService
{
    private readonly EsiService _esiService;
    private readonly MongoDbService _dbService;


    public PersonalOrderHistoryCollectionService(WebApplication app, EsiService esiService, MongoDbService dbService) : base(app, 3600 * 1000)
    {
        _esiService = esiService;
        _dbService = dbService;
    }

    protected override async Task OnTimerElapsed()
    {
        App.Logger.LogInformation("Collecting PersonalOrderHistory Data");
        var tokens = await _dbService.GetAllUserTokensAsync();
        await tokens.ForEachAsync(async token => {
            var esi = await _esiService.GetClientAsync(token);
            var orders = await esi.Market.CharacterOrderHistory();
            if (orders.StatusCode == HttpStatusCode.OK)
            {
                if (orders.Data.Count > 0)
                {
                    var orderHistoryDocuments = orders.Data.Select(order => new PersonalOrderHistoryDocument(order, token.CharacterId));
                    try
                    {
                        await _dbService.PersonalOrderHistoryCollection.InsertManyAsync(orderHistoryDocuments, new InsertManyOptions
                        {
                            IsOrdered = false
                        });
                    }
                    catch (MongoBulkWriteException e)
                    {
                        if (e.WriteErrors.Any(error => error.Code != 11000))
                            App.Logger.LogError(e, "Error while collecting transaction data for {authToken.CharacterName}", token.CharacterName);
                    }
                }
            }
        });
    }
}