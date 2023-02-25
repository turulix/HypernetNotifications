using MongoDB.Bson.Serialization.Attributes;

namespace EveHypernetNotification.DatabaseDocuments;

[BsonIgnoreExtraElements]
public class OAuthTokens
{
    public long CharacterId { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public string TokenType { get; set; }
    public string CharacterName { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

}