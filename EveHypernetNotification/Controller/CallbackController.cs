using ESI.NET.Enumerations;
using EveHypernetNotification.DatabaseDocuments;
using EveHypernetNotification.Services;
using Microsoft.AspNetCore.Mvc;

namespace EveHypernetNotification.Controller;

[ApiController]
public class CallbackController : Microsoft.AspNetCore.Mvc.Controller
{
    private readonly EsiService _esiClient;
    private readonly MongoDbService _databaseSession;

    public CallbackController()
    {
        _esiClient = Program.Services.GetService<EsiService>()!;
        _databaseSession = Program.Services.GetService<MongoDbService>()!;
    }

    [Route("/callback")]
    public async Task<IActionResult> Callback()
    {
        var code = Request.Query["code"];
        var sso = await _esiClient.GetToken(GrantType.AuthorizationCode, code);

        var auth = await _esiClient.Verify(sso);

        var token = new OAuthTokens
        {
            CharacterId = auth.CharacterID,
            AccessToken = sso.AccessToken,
            RefreshToken = sso.RefreshToken,
            ExpiresIn = sso.ExpiresIn,
            TokenType = sso.TokenType,
            CharacterName = auth.CharacterName,
            ExpiresOn = DateTime.UtcNow.AddSeconds(sso.ExpiresIn)
        };
        
        await _databaseSession.AddOrUpdateTokenAsync(token);

        return Ok("You are now authenticated! And can safely close this window.");
    }
}