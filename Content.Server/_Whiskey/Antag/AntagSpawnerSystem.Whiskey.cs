using Content.Server.Antag.Components;
using Content.Server.Mind;

namespace Content.Server.Antag;

public sealed partial class AntagSpawnerSystem
{
    [Dependency] private MindSystem _mind = default!;

    private void PreserveSelectedMind(Entity<AntagSpawnerComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (!ent.Comp.PreserveMind || args.Session is not { } session ||
            !_mind.TryGetMind(session, out var mind, out var mindComponent))
            return;

        _mind.TransferTo(mind, args.Entity, ghostCheckOverride: true, mind: mindComponent);
    }
}
