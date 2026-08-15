using Content.Shared.Chat;
using Content.Trauma.Common.Language;
using Content.Shared.Radio;

namespace Content.Server.Radio;

// Einstein Engines - Language begin
/// <summary>
/// <param name="OriginalChatMsg">The message to display when the speaker can understand "language"</param>
/// <param name="LanguageObfuscatedChatMsg">The message to display when the Speaker cannot understand "language"</param>
/// </summary>
/// <param name="Remontar">
/// Whiskey: monta a mensagem de novo a partir de outro texto, mantendo o mesmo
/// "Fulano diz pelo canal tal". Existe porque o evento carrega a mensagem já
/// embrulhada, e não os ingredientes que fizeram o embrulho: nome, verbo,
/// ícone e nome do cargo. Quem precisa entregar um texto diferente para um
/// ouvinte específico, como a tradução, chamaria de novo o mesmo código que
/// montou o original. Passar a função evita duplicar essa montagem, ou pior,
/// sair substituindo pedaço de texto dentro do embrulho pronto.
/// </param>
[ByRefEvent]
public readonly record struct RadioReceiveEvent(
    EntityUid MessageSource,
    RadioChannelPrototype Channel,
    ChatMessage OriginalChatMsg,
    ChatMessage LanguageObfuscatedChatMsg,
    LanguagePrototype Language,
    EntityUid RadioSource,
    Func<string, ChatMessage>? Remontar = null // Whiskey
    );
// Einstein Engines - Language end

/// <summary>
/// Event raised on the parent entity of a headset radio when a radio message is received
/// </summary>
[ByRefEvent]
public readonly record struct HeadsetRadioReceiveRelayEvent(RadioReceiveEvent RelayedEvent);

/// <summary>
/// Use this event to cancel sending message per receiver
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}
