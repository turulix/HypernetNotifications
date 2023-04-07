using Discord;
using Discord.Interactions;
using EveHypernetNotification.Services;

namespace EveHypernetNotification.Commands;

public class GetAccessUrl : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EsiService _esiService;

    public GetAccessUrl(EsiService esiService)
    {
        _esiService = esiService;
    }

    [SlashCommand("access", "Get Access URL")]
    public async Task GetAccess()
    {
        var url = _esiService.GetAuthUrl();
        await Context.Interaction.RespondAsync(
            embed: new EmbedBuilder().WithDescription($"[Click here to get access]({url})").Build(),
            ephemeral: true
        );
    }
}