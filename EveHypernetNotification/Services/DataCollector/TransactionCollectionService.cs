using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.Services.Base;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace EveHypernetNotification.Services.DataCollector;

[UsedImplicitly]
public class TransactionCollectionService : TimedService
{
    private readonly EsiService _esiService;
    private readonly MongoDbService _dbService;

    public TransactionCollectionService(WebApplication app, EsiService esiService, MongoDbService dbService) : base(app, 3600 * 1000)
    {
        _esiService = esiService;
        _dbService = dbService;
    }

    protected override async Task OnTimerElapsed()
    {
        App.Logger.LogInformation("Collecting wallet data");
        var tokens = await _dbService.GetAllUserTokensAsync();
        await tokens.ForEachAsync(async authToken => {
            var esiClient = await _esiService.GetClientAsync(authToken);
            var transactions = await esiClient.Wallet.CharacterTransactions();
            if (transactions.Data != null)
            {
                var documents = transactions.Data.Select(transaction => new TransactionDocument(transaction, authToken.CharacterId));
                try
                {
                    await _dbService.TransactionsCollection.InsertManyAsync(documents, new InsertManyOptions
                    {
                        IsOrdered = false
                    });
                }
                catch (MongoBulkWriteException e)
                {
                    if (e.WriteErrors.Any(error => error.Code != 11000))
                        App.Logger.LogError(e, "Error while collecting transaction data for {authToken.CharacterName}", authToken.CharacterName);
                }
            }

            var journal = await esiClient.Wallet.CharacterJournal();
            if (journal.Data != null)
            {
                var documents = journal.Data.Select(journalEntry => new JournalEntryDocument(journalEntry, authToken.CharacterId));
                try
                {
                    await _dbService.JournalEntryCollection.InsertManyAsync(documents, new InsertManyOptions
                    {
                        IsOrdered = false
                    });
                }
                catch (MongoBulkWriteException e)
                {
                    if (e.WriteErrors.Any(error => error.Code != 11000))
                        App.Logger.LogError(e, "Error while collecting Journal data for {authToken.CharacterName}", authToken.CharacterName);
                }
            }
        });
    }
}