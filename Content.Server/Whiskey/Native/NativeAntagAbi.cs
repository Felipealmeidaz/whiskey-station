using System.Runtime.InteropServices;

namespace Content.Server.Whiskey.Native;

public static class NativeAntagAbi
{
    public const uint Version = 1;
    public const int EventSize = 72;
    public const int CommandSize = 48;
    public const int CommandCapacity = 16;
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = NativeAntagAbi.EventSize)]
public struct NativeAntagEvent
{
    [FieldOffset(0)] public uint Type;
    [FieldOffset(4)] public uint Flags;
    [FieldOffset(8)] public ulong Handle;
    [FieldOffset(16)] public ulong ServerTick;
    [FieldOffset(24)] public ulong Self;
    [FieldOffset(32)] public ulong Target;
    [FieldOffset(40)] public uint Input;
    [FieldOffset(44)] public float Value0;
    [FieldOffset(48)] public float SelfX;
    [FieldOffset(52)] public float SelfY;
    [FieldOffset(56)] public float TargetX;
    [FieldOffset(60)] public float TargetY;
    [FieldOffset(64)] public uint Random;
    [FieldOffset(68)] public uint ActiveItem;
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = NativeAntagAbi.CommandSize)]
public struct NativeAntagCommand
{
    [FieldOffset(0)] public uint Type;
    [FieldOffset(4)] public uint Flags;
    [FieldOffset(8)] public ulong Source;
    [FieldOffset(16)] public ulong Target;
    [FieldOffset(24)] public int Value0;
    [FieldOffset(28)] public int Value1;
    [FieldOffset(32)] public float Value2;
    [FieldOffset(36)] public float Value3;
    [FieldOffset(40)] public uint Token;
    [FieldOffset(44)] public uint Reserved;
}

public enum NativeAntagEventType : uint
{
    Spawn = 1,
    Update = 2,
    TouchAction = 4,
    ProcedureAction = 5,
    SelfHealAction = 6,
    PatientHealAction = 7,
    PatientKillAction = 8,
    ProcedureInterrupted = 9,
    EntityDeleted = 10,
    Disconnected = 11,
    Died = 12,
    PatientCreated = 14,
    PatientRemoved = 15,
    RoundEnded = 16,
    ObjectiveQuery = 17,
    PlayerAttached = 18,
    Spoke = 19,
}

[Flags]
public enum NativeAntagFlags : uint
{
    None = 0,
    TargetValid = 1 << 0,
    TargetAlive = 1 << 1,
    TargetDead = 1 << 2,
    TargetHumanoid = 1 << 3,
    TargetConverted = 1 << 4,
    TargetInMeleeRange = 1 << 5,
    RequiredToolHeld = 1 << 14,
    TargetOwnPatient = 1 << 16,
    TargetProtected = 1 << 17,
    TargetHasSession = 1 << 19,
    SelfHasSession = 1 << 20,
    TargetCanDie = 1 << 21,
}

public enum NativeAntagCommandType : uint
{
    None,
    AddAction,
    SetActionCooldown,
    SetMobState = 6,
    AddComponentBundle = 9,
    RemoveComponentBundle = 10,
    ZombifyEntity = 11,
    UnzombifyEntity = 12,
    RejuvenateEntity = 13,
    Popup = 14,
    SetFaction = 16,
    SetNativeOwner = 17,
    ReportCounter = 18,
    ClearRoutedTarget = 20,
    PlaySound = 21,
    StopSound = 22,
    NotifyEvent = 23,
}

[Flags]
public enum NativeAntagCommandFlags : uint
{
    None = 0,
    RequirePreviousSuccess = 1 << 0,
    PreserveVisualSkin = 1 << 1,
}
