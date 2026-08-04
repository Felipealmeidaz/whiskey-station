// Copyright (c) 2026 punkzebub <punkzebub@gmail.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Movement.Pulling.Components;
using Content.Medical.Common.Targeting;
using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared.EntityConditions;

public sealed partial class SelfTargetCondition : EntityConditionBase<SelfTargetCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class SelfTargetConditionSystem : EntityConditionSystem<MetaDataComponent, SelfTargetCondition>
{
    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<SelfTargetCondition> args)
    {
        args.Result = args.SourceEnt == ent.Owner;
    }
}

public sealed partial class TargetBodyPartCondition : EntityConditionBase<TargetBodyPartCondition>
{
    [DataField(required: true)]
    public TargetBodyPart Target;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class TargetBodyPartConditionSystem : EntityConditionSystem<TargetingComponent, TargetBodyPartCondition>
{
    protected override void Condition(Entity<TargetingComponent> ent, ref EntityConditionEvent<TargetBodyPartCondition> args)
    {
        args.Result = ent.Comp.Target == args.Condition.Target;
    }
}

public sealed partial class GrabStageCondition : EntityConditionBase<GrabStageCondition>
{
    [DataField(required: true)]
    public GrabStage MinimumStage;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class GrabStageConditionSystem : EntityConditionSystem<PullableComponent, GrabStageCondition>
{
    protected override void Condition(Entity<PullableComponent> ent, ref EntityConditionEvent<GrabStageCondition> args)
    {
        args.Result = ent.Comp.GrabStage >= args.Condition.MinimumStage;
    }
}
