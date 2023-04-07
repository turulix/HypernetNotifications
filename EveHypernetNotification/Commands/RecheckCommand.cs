using Discord.Interactions;
using EveHypernetNotification.Services;
using EveHypernetNotification.Services.DataCollector;
using JetBrains.Annotations;

namespace EveHypernetNotification.Commands;

[UsedImplicitly]
public class RecheckCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly HypernetCollectionService _hypernetCollectionService;

    public RecheckCommand(HypernetCollectionService hypernetCollectionService)
    {
        _hypernetCollectionService = hypernetCollectionService;
    }

    [UsedImplicitly]
    [SlashCommand("recheck", "Recheck all auctions")]
    public async Task RecheckNow()
    {
        await Context.Interaction.RespondAsync("Rechecking now!", ephemeral: true);
        await _hypernetCollectionService.CheckHypernetAuctions();
    }
}