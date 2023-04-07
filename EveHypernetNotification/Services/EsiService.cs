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
            "publicData",
            "esi-calendar.respond_calendar_events.v1",
            "esi-calendar.read_calendar_events.v1",
            "esi-location.read_location.v1",
            "esi-location.read_ship_type.v1",
            "esi-mail.organize_mail.v1",
            "esi-mail.read_mail.v1",
            "esi-mail.send_mail.v1",
            "esi-skills.read_skills.v1",
            "esi-skills.read_skillqueue.v1",
            "esi-wallet.read_character_wallet.v1",
            "esi-search.search_structures.v1",
            "esi-clones.read_clones.v1",
            "esi-characters.read_contacts.v1",
            "esi-universe.read_structures.v1",
            "esi-bookmarks.read_character_bookmarks.v1",
            "esi-killmails.read_killmails.v1",
            "esi-corporations.read_corporation_membership.v1",
            "esi-assets.read_assets.v1",
            "esi-planets.manage_planets.v1",
            "esi-fleets.read_fleet.v1",
            "esi-fleets.write_fleet.v1",
            "esi-ui.open_window.v1",
            "esi-ui.write_waypoint.v1",
            "esi-characters.write_contacts.v1",
            "esi-fittings.read_fittings.v1",
            "esi-fittings.write_fittings.v1",
            "esi-markets.structure_markets.v1",
            "esi-corporations.read_structures.v1",
            "esi-characters.read_loyalty.v1",
            "esi-characters.read_opportunities.v1",
            "esi-characters.read_medals.v1",
            "esi-characters.read_standings.v1",
            "esi-characters.read_agents_research.v1",
            "esi-industry.read_character_jobs.v1",
            "esi-markets.read_character_orders.v1",
            "esi-characters.read_blueprints.v1",
            "esi-characters.read_corporation_roles.v1",
            "esi-location.read_online.v1",
            "esi-contracts.read_character_contracts.v1",
            "esi-clones.read_implants.v1",
            "esi-characters.read_fatigue.v1",
            "esi-killmails.read_corporation_killmails.v1",
            "esi-corporations.track_members.v1",
            "esi-wallet.read_corporation_wallets.v1",
            "esi-characters.read_notifications.v1",
            "esi-corporations.read_divisions.v1",
            "esi-corporations.read_contacts.v1",
            "esi-assets.read_corporation_assets.v1",
            "esi-corporations.read_titles.v1",
            "esi-corporations.read_blueprints.v1",
            "esi-bookmarks.read_corporation_bookmarks.v1",
            "esi-contracts.read_corporation_contracts.v1",
            "esi-corporations.read_standings.v1",
            "esi-corporations.read_starbases.v1",
            "esi-industry.read_corporation_jobs.v1",
            "esi-markets.read_corporation_orders.v1",
            "esi-corporations.read_container_logs.v1",
            "esi-industry.read_character_mining.v1",
            "esi-industry.read_corporation_mining.v1",
            "esi-planets.read_customs_offices.v1",
            "esi-corporations.read_facilities.v1",
            "esi-corporations.read_medals.v1",
            "esi-characters.read_titles.v1",
            "esi-alliances.read_contacts.v1",
            "esi-characters.read_fw_stats.v1",
            "esi-corporations.read_fw_stats.v1"
        }, "abc");
    }

    public async Task<SsoToken> GetToken(GrantType authorizationCode, StringValues code)
    {
        return await _esiClient.SSO.GetToken(authorizationCode, code);
    }

    public async Task<OAuthTokensDocument> RefreshToken(OAuthTokensDocument tokenDocument)
    {
        _app.Logger.LogInformation("Refreshing Tokens");
        var newTokens =
            await _esiClient.SSO.GetToken(GrantType.RefreshToken, tokenDocument.RefreshToken);
        tokenDocument.AccessToken = newTokens.AccessToken;
        tokenDocument.RefreshToken = newTokens.RefreshToken;
        tokenDocument.ExpiresIn = newTokens.ExpiresIn;
        tokenDocument.TokenType = newTokens.TokenType;
        tokenDocument.ExpiresOn = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn);
        await _dbService.AddOrUpdateTokenAsync(tokenDocument);
        return tokenDocument;
    }

    public async Task<OAuthTokensDocument> EnsureTokenValid(OAuthTokensDocument tokenDocument)
    {
        if (tokenDocument.ExpiresOn < DateTime.UtcNow.AddSeconds(10))
        {
            tokenDocument = await RefreshToken(tokenDocument);
        }

        return tokenDocument;
    }

    public async Task<AuthorizedCharacterData> Verify(SsoToken sso)
    {
        return await _esiClient.SSO.Verify(sso);
    }

    public async Task<AuthorizedCharacterData> Verify(OAuthTokensDocument sso)
    {
        return await _esiClient.SSO.Verify(new SsoToken
        {
            RefreshToken = sso.RefreshToken,
            AccessToken = sso.AccessToken,
            ExpiresIn = sso.ExpiresIn,
            TokenType = sso.TokenType,
        });
    }

    public EsiClient GetClient()
    {
        return new EsiClient(_config);
    }

    public async Task<EsiClient> GetClientAsync(OAuthTokensDocument tokenDocument)
    {
        var ensuredToken = await EnsureTokenValid(tokenDocument);
        var client = new EsiClient(_config);
        var verifiedTokens = await Verify(ensuredToken);
        client.SetCharacterData(verifiedTokens);
        return client;
    }

    public EsiClient GetClient(AuthorizedCharacterData authData)
    {
        var client = new EsiClient(_config);
        client.SetCharacterData(authData);
        return client;
    }
}