using EveHypernetNotification.DatabaseDocuments;
using MongoDB.Driver;

namespace EveHypernetNotification.Services;

public class MongoDbService
{
    public readonly MongoClient Mongodb;

    public readonly IMongoCollection<HyperNetAuction> AuctionCollection;
    public readonly IMongoCollection<OAuthTokens> TokensCollection;
    public readonly IMongoDatabase Database;

    public MongoDbService(WebApplication app)
    {
        Mongodb = new MongoClient(app.Configuration["MONGO_URL"]);
        Database = Mongodb.GetDatabase("EveHypernet");
        AuctionCollection = Database.GetCollection<HyperNetAuction>("Hypernet");
        TokensCollection = Database.GetCollection<OAuthTokens>("Authentication");
    }

    public async Task AddOrUpdateTokenAsync(OAuthTokens token)
    {
        if (await TokensCollection.Find(x => x.CharacterId == token.CharacterId).AnyAsync())
            await TokensCollection.ReplaceOneAsync(x => x.CharacterId == token.CharacterId, token);
        else
            await TokensCollection.InsertOneAsync(token);
    }
}