using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using EveHypernetNotification.DatabaseDocuments;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Type = ESI.NET.Models.Universe.Type;

namespace EveHypernetNotification.Services;

public class EsiService
{
    private readonly WebApplication _app;
    private readonly MongoDbService _dbService;
    private readonly IOptions<EsiConfig> _config;
    private readonly EsiClient _esiClient;

    public EsiService(WebApplication app, MongoDbService dbService)
    {
        _app = app;
        _dbService = dbService;
        _config = Options.Create(new EsiConfig
        {
            EsiUrl = "https://esi.evetech.net/",
            DataSource = DataSource.Tranquility,
            ClientId = app.Configuration["CLIENT_ID"],
            SecretKey = app.Configuration["CLIENT_SECRET"],
            CallbackUrl = app.Configuration["CALLBACK"],
            UserAgent = "Leira Saint",
        });
        _esiClient = new EsiClient(_config);
    }

    public string GetAuthUrl()
    {
        return _esiClient.SSO.CreateAuthenticationUrl(new List<string>
        {
            "esi-characters.read_notifications.v1"
        }, "abc");
    }

    public async Task<SsoToken> GetToken(GrantType authorizationCode, StringValues code)
    {
        return await _esiClient.SSO.GetToken(authorizationCode, code);
    }

    public async Task<OAuthTokens> RefreshToken(OAuthTokens token)
    {
        _app.Logger.LogInformation("Refreshing Tokens");
        var newTokens =
            await _esiClient.SSO.GetToken(GrantType.RefreshToken, token.RefreshToken);
        token.AccessToken = newTokens.AccessToken;
        token.RefreshToken = newTokens.RefreshToken;
        token.ExpiresIn = newTokens.ExpiresIn;
        token.TokenType = newTokens.TokenType;
        token.ExpiresOn = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn);
        await _dbService.AddOrUpdateTokenAsync(token);
        return token;
    }

    public async Task<OAuthTokens> EnsureTokenValid(OAuthTokens token)
    {
        if (token.ExpiresOn < DateTime.UtcNow.AddSeconds(10))
        {
            token = await RefreshToken(token);
        }

        return token;
    }

    public async Task<AuthorizedCharacterData> Verify(SsoToken sso)
    {
        return await _esiClient.SSO.Verify(sso);
    }

    public async Task<AuthorizedCharacterData> Verify(OAuthTokens sso)
    {
        return await _esiClient.SSO.Verify(new SsoToken
        {
            RefreshToken = sso.RefreshToken,
            AccessToken = sso.AccessToken,
            ExpiresIn = sso.ExpiresIn,
            TokenType = sso.TokenType
        });
    }

    public EsiClient GetClient()
    {
        return new EsiClient(_config);
    }

    public EsiClient GetClient(AuthorizedCharacterData authData)
    {
        var client = new EsiClient(_config);
        client.SetCharacterData(authData);
        return client;
    }
}