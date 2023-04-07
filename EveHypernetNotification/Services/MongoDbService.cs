using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.DatabaseDocuments.Market;
using MongoDB.Driver;

namespace EveHypernetNotification.Services;

public class MongoDbService
{
    public readonly MongoClient Mongodb;

    public readonly IMongoCollection<HypernetAuctionDocument> HypernetAuctionCollection;
    public readonly IMongoCollection<OAuthTokensDocument> TokensCollection;
    public readonly IMongoCollection<TransactionDocument> TransactionsCollection;
    public readonly IMongoCollection<JournalEntryDocument> JournalEntryCollection;
    public readonly IMongoCollection<RegionOrderDocument> RegionOrderCollection;
    public readonly IMongoCollection<MarketPriceDocument> MarketPriceCollection;
    public readonly IMongoCollection<PersonalOrderDocument> PersonalOrderCollection;
    public readonly IMongoCollection<PersonalOrderHistoryDocument> PersonalOrderHistoryCollection;
    public readonly IMongoDatabase Database;


    public MongoDbService(WebApplication app)
    {
        Mongodb = new MongoClient(app.Configuration["MONGO_URL"]);
        Database = Mongodb.GetDatabase("EveHypernet");
        TokensCollection = Database.GetCollection<OAuthTokensDocument>("Authentication");
        HypernetAuctionCollection = Database.GetCollection<HypernetAuctionDocument>("Hypernet");
        TransactionsCollection = Database.GetCollection<TransactionDocument>("Transactions");
        JournalEntryCollection = Database.GetCollection<JournalEntryDocument>("JournalEntries");
        RegionOrderCollection = Database.GetCollection<RegionOrderDocument>("RegionOrders");
        MarketPriceCollection = Database.GetCollection<MarketPriceDocument>("MarketPrices");
        PersonalOrderCollection = Database.GetCollection<PersonalOrderDocument>("PersonalOrders");
        PersonalOrderHistoryCollection = Database.GetCollection<PersonalOrderHistoryDocument>("PersonalOrderHistory");
    }


    public async Task AddOrUpdateTokenAsync(OAuthTokensDocument tokenDocument)
    {
        if (await TokensCollection.Find(x => x.CharacterId == tokenDocument.CharacterId).AnyAsync())
            await TokensCollection.ReplaceOneAsync(x => x.CharacterId == tokenDocument.CharacterId, tokenDocument);
        else
            await TokensCollection.InsertOneAsync(tokenDocument);
    }

    public Task<IAsyncCursor<OAuthTokensDocument>> GetAllUserTokensAsync()
    {
        return TokensCollection.FindAsync(FilterDefinition<OAuthTokensDocument>.Empty);
    }
}