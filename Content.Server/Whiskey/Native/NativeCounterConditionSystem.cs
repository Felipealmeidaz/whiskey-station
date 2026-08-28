using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Adapts a native counter to the generic objective progress API. Counter
/// mutation and selection remain entirely inside the native scenario.
/// </summary>
public sealed partial class NativeCounterConditionSystem : EntitySystem
{
    [Dependency] private NativeAntagBridgeSystem _bridge = default!;
    [Dependency] private NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NativeCounterConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<NativeCounterConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } body ||
            !_bridge.TryQueryCounter(body, ent.Comp.Token, out var current))
        {
            args.Progress = 0f;
            return;
        }

        var target = _number.GetTarget(ent.Owner);
        args.Progress = target <= 0 ? 1f : Math.Clamp((float) current / target, 0f, 1f);
    }
}
