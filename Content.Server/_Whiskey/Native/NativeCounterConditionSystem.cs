using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Adapts a committed native counter mirror to the objective progress API.
/// The mirror lives on the mind-owned objective entity rather than the body.
/// </summary>
public sealed partial class NativeCounterConditionSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NativeCounterConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<NativeCounterConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent.Owner);
        args.Progress = target <= 0 ? 1f : Math.Clamp((float) ent.Comp.Current / target, 0f, 1f);
    }
}
