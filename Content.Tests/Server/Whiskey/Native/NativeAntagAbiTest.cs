using System.Runtime.InteropServices;
using Content.Server.Whiskey.Native;
using NUnit.Framework;

namespace Content.Tests.Server.Whiskey.Native;

[TestFixture]
public sealed class NativeAntagAbiTest
{
    [Test]
    public void EventLayoutMatchesNativeContract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Marshal.SizeOf<NativeAntagEvent>(), Is.EqualTo(NativeAntagAbi.EventSize));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Type)), Is.EqualTo(0));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Flags)), Is.EqualTo(4));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Handle)), Is.EqualTo(8));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.ServerTick)), Is.EqualTo(16));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Self)), Is.EqualTo(24));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Target)), Is.EqualTo(32));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Input)), Is.EqualTo(40));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Value0)), Is.EqualTo(44));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.SelfX)), Is.EqualTo(48));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.SelfY)), Is.EqualTo(52));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.TargetX)), Is.EqualTo(56));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.TargetY)), Is.EqualTo(60));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.Random)), Is.EqualTo(64));
            Assert.That(OffsetOf<NativeAntagEvent>(nameof(NativeAntagEvent.ActiveItem)), Is.EqualTo(68));
        });
    }

    [Test]
    public void CommandLayoutMatchesNativeContract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Marshal.SizeOf<NativeAntagCommand>(), Is.EqualTo(NativeAntagAbi.CommandSize));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Type)), Is.EqualTo(0));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Flags)), Is.EqualTo(4));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Source)), Is.EqualTo(8));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Target)), Is.EqualTo(16));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Value0)), Is.EqualTo(24));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Value1)), Is.EqualTo(28));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Value2)), Is.EqualTo(32));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Value3)), Is.EqualTo(36));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Token)), Is.EqualTo(40));
            Assert.That(OffsetOf<NativeAntagCommand>(nameof(NativeAntagCommand.Reserved)), Is.EqualTo(44));
        });
    }

    private static int OffsetOf<T>(string field) where T : struct
        => checked((int) Marshal.OffsetOf<T>(field));
}
