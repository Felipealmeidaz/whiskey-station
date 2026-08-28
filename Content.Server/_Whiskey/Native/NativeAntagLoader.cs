using System.Runtime.InteropServices;
using System.IO;
using Robust.Shared.Log;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Loads and validates a native antagonist module without binding managed
/// objects into its lifetime. Missing/incompatible modules have no managed
/// gameplay fallback.
/// </summary>
public sealed class NativeAntagLoader : IDisposable
{
    public const string ModuleId = "operative-hidden";
    private const string ModuleFile = "libwhiskey_operativo_oculto.so";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint GetAbiVersionDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint LifecycleDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong CreateDelegate(ulong self);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint DestroyDelegate(ulong handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint DispatchDelegate(
        ref NativeAntagEvent nativeEvent,
        [Out] NativeAntagCommand[] commands,
        uint capacity);

    private readonly ISawmill _log;
    private nint _library;
    private LifecycleDelegate? _initialize;
    private LifecycleDelegate? _shutdown;
    private CreateDelegate? _create;
    private DestroyDelegate? _destroy;
    private DispatchDelegate? _dispatch;

    public bool Available { get; private set; }
    public uint LoadedAbiVersion { get; private set; }
    public string? Failure { get; private set; }

    public NativeAntagLoader(ILogManager logManager)
    {
        _log = logManager.GetSawmill("native.antag");
        Load();
    }

    private void Load()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            Failure = $"unsupported platform {RuntimeInformation.OSDescription}/{RuntimeInformation.ProcessArchitecture}; expected Linux/x64";
            _log.Error($"Hidden Operative native module unavailable: {Failure}");
            return;
        }

        var modulePath = Path.Combine(AppContext.BaseDirectory, ModuleFile);
        if (!NativeLibrary.TryLoad(modulePath, out _library))
        {
            Failure = $"failed to load '{modulePath}'";
            _log.Error($"Hidden Operative native module unavailable: {Failure}");
            return;
        }

        try
        {
            var getAbi = Resolve<GetAbiVersionDelegate>("operative_hidden_get_abi_version");
            _initialize = Resolve<LifecycleDelegate>("operative_hidden_initialize");
            _shutdown = Resolve<LifecycleDelegate>("operative_hidden_shutdown");
            _create = Resolve<CreateDelegate>("operative_hidden_create");
            _destroy = Resolve<DestroyDelegate>("operative_hidden_destroy");
            _dispatch = Resolve<DispatchDelegate>("operative_hidden_dispatch");

            LoadedAbiVersion = getAbi();
            if (LoadedAbiVersion != NativeAntagAbi.Version)
                throw new InvalidOperationException($"expected ABI {NativeAntagAbi.Version}, received ABI {LoadedAbiVersion}");
            if (_initialize() == 0)
                throw new InvalidOperationException("native initialization returned failure");

            Available = true;
            _log.Info($"Loaded {ModuleFile} with ABI {LoadedAbiVersion}");
        }
        catch (Exception exception)
        {
            Failure = exception.Message;
            _log.Error($"Hidden Operative native module unavailable: {exception}");
            NativeLibrary.Free(_library);
            _library = 0;
        }
    }

    private T Resolve<T>(string symbol) where T : Delegate
    {
        if (!NativeLibrary.TryGetExport(_library, symbol, out var address))
            throw new MissingMethodException(ModuleFile, symbol);
        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    public ulong Create(ulong self)
        => Available ? _create!(self) : 0;

    public bool Destroy(ulong handle)
        => Available && _destroy!(handle) != 0;

    public int Dispatch(ref NativeAntagEvent nativeEvent, NativeAntagCommand[] commands, int capacity)
    {
        if (!Available)
            return 0;
        if (capacity is <= 0 or > NativeAntagAbi.CommandCapacity || capacity > commands.Length)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        var count = _dispatch!(ref nativeEvent, commands, (uint) capacity);
        if ((count & NativeAntagAbi.DispatchErrorCommandOverflow) != 0)
            throw new NativeAntagCommandBufferOverflowException(nativeEvent.Type, capacity);
        count &= NativeAntagAbi.DispatchCommandCountMask;
        if (count > capacity)
            throw new InvalidOperationException($"Native command count {count} exceeds buffer capacity {capacity}");
        return (int) count;
    }

    public bool Reset()
    {
        if (!Available)
            return false;
        _shutdown!();
        Available = _initialize!() != 0;
        Failure = Available ? null : "native reinitialization returned failure";
        return Available;
    }

    public void Dispose()
    {
        if (_library == 0)
            return;

        if (Available)
            _shutdown!();
        Available = false;
        NativeLibrary.Free(_library);
        _library = 0;
    }
}

public sealed class NativeAntagCommandBufferOverflowException(uint eventType, int capacity)
    : InvalidOperationException($"Native event {eventType} exceeded the {capacity}-command ABI buffer; state was rolled back")
{
    public uint EventType { get; } = eventType;
    public int Capacity { get; } = capacity;
}
