using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace EveHypernetNotification.Utilities;

public static class MongoExtensions
{
    public static BsonDocument RenderToBsonDocument<T>(this FilterDefinition<T> filter)
    {
        var serializerRegistry = BsonSerializer.SerializerRegistry;
        var documentSerializer = serializerRegistry.GetSerializer<T>();
        return filter.Render(documentSerializer, serializerRegistry);
    }

    public static BsonDocument RenderToBsonDocument<T>(this UpdateDefinition<T> update)
    {
        var serializerRegistry = BsonSerializer.SerializerRegistry;
        var documentSerializer = serializerRegistry.GetSerializer<T>();
        return (BsonDocument)update.Render(documentSerializer, serializerRegistry);
    }
}