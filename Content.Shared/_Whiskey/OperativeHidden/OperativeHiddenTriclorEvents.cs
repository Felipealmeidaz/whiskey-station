// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Whiskey.OperativeHidden;

/// <summary>
/// Applies the Hidden Operative's hypercatalyzed triclor dose to one target.
/// The server owns the full eight-second sequence.
/// </summary>
public sealed partial class OperativeHiddenTriclorActionEvent : EntityTargetActionEvent;

/// <summary>
/// Relays the Hidden Operative's controls to a reconditioned patient without
/// moving either mind out of its current body.
/// </summary>
public sealed partial class OperativeHiddenReceptionActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public enum OperativeHiddenHallucinationType : byte
{
    Hand,
    Eye,
}

/// <summary>
/// Sent only to the client controlling the poisoned victim.
/// </summary>
[Serializable, NetSerializable]
public sealed class OperativeHiddenHallucinationEvent : EntityEventArgs
{
    public readonly OperativeHiddenHallucinationType Hallucination;

    public OperativeHiddenHallucinationEvent(OperativeHiddenHallucinationType hallucination)
    {
        Hallucination = hallucination;
    }
}
