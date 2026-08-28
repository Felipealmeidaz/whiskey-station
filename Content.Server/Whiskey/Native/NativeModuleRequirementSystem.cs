using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Whiskey.Native;
using Robust.Shared.Log;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Fails a rule closed when its required native runtime is unavailable or uses
/// a different ABI. It never activates a managed gameplay substitute.
/// </summary>
public sealed partial class NativeModuleRequirementSystem : GameRuleSystem<NativeModuleRequirementComponent>
{
    [Dependency] private NativeAntagBridgeSystem _bridge = default!;
    [Dependency] private ILogManager _logManager = default!;

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();
        _log = _logManager.GetSawmill("native.antag.rule");
    }

    protected override void Added(EntityUid uid,
        NativeModuleRequirementComponent component,
        GameRuleComponent gameRule,
        GameRuleAddedEvent args)
    {
        if (_bridge.SupportsModule(component.Module, component.AbiVersion))
            return;

        _log.Error($"Hidden Operative native module unavailable. GameRule disabled. module={component.Module} " +
                   $"expected ABI={component.AbiVersion} error={_bridge.NativeFailure ?? "ABI mismatch"}");
        GameTicker.EndGameRule(uid);
    }
}
