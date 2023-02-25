using Discord.Interactions;
using EveHypernetNotification.Services;
using JetBrains.Annotations;

namespace EveHypernetNotification.Commands;

[UsedImplicitly]
public class RecheckCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TimedUpdateService _timedUpdateService;

    public RecheckCommand(TimedUpdateService timedUpdateService)
    {
        _timedUpdateService = timedUpdateService;
    }

    [UsedImplicitly]
    [SlashCommand("recheck", "Recheck all auctions")]
    public async Task RecheckNow()
    {
        await Context.Interaction.RespondAsync("Rechecking now!", ephemeral: true);
        await _timedUpdateService.CheckHypernetAuctions();
    }
}