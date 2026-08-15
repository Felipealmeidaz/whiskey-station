// <Trauma>
using Content.Shared.Speech;
using Content.Trauma.Common.Chat;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Systems;
using Content.Trauma.Common.Wizard;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Utility;
// </Trauma>
using System.Globalization;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.MoMMI;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Speech.EntitySystems;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Replays;

namespace Content.Server.Chat.Systems;

// TODO refactor whatever active warzone this class and chatmanager have become
/// <summary>
///     ChatSystem is responsible for in-simulation chat handling, such as whispering, speaking, emoting, etc.
///     ChatSystem depends on ChatManager to actually send the messages.
/// </summary>
public sealed partial class ChatSystem : SharedChatSystem
{
    // <Trauma>
    [Dependency] private CommonGhostVisibilitySystem _ghostVisibility = default!;
    [Dependency] private CommonScryingOrbSystem _scrying = default!;
    [Dependency] private CollectiveMindUpdateSystem _collectiveMind = default!;
    [Dependency] private CommonLanguageSystem _language = default!;

    [Dependency] private _Whiskey.Translation.TranslationSystem _translation = default!; // Whiskey

    // <Whiskey> - ligado só durante o reenvio de uma fala já traduzida, que
    // acontece na thread do jogo e termina antes de qualquer outra fala ser
    // processada, então um campo simples basta.
    private bool _reenviandoTraducao;
    // </Whiskey>
    // </Trauma>
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IMoMMILink _mommiLink = default!;
    [Dependency] private IChatSanitizationManager _sanitizer = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private MobStateSystem _mobStateSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ReplacementAccentSystem _wordreplacement = default!;
    [Dependency] private ExamineSystemShared _examineSystem = default!;
    [Dependency] private EntityQuery<GhostHearingComponent> _ghostHearingQuery = default!;

    private bool _loocEnabled = true;
    private bool _deadLoocEnabled;
    private bool _critLoocEnabled;
    private readonly bool _adminLoocEnabled = true;

    private bool _deadChatEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configurationManager, CCVars.LoocEnabled, OnLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.DeadLoocEnabled, OnDeadLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.CritLoocEnabled, OnCritLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.DeadChatEnabled, OnDeadChatEnabledChanged, true);

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameChange);
    }

    private void OnLoocEnabledChanged(bool val)
    {
        if (_loocEnabled == val) return;

        _loocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-looc-chat-enabled-message" : "chat-manager-looc-chat-disabled-message"));
    }

    private void OnDeadLoocEnabledChanged(bool val)
    {
        if (_deadLoocEnabled == val) return;

        _deadLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-dead-looc-chat-enabled-message" : "chat-manager-dead-looc-chat-disabled-message"));
    }

    private void OnCritLoocEnabledChanged(bool val)
    {
        if (_critLoocEnabled == val)
            return;

        _critLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-crit-looc-chat-enabled-message" : "chat-manager-crit-looc-chat-disabled-message"));
    }

    private void OnDeadChatEnabledChanged(bool val)
    {
        if (_deadChatEnabled == val)
            return;

        _deadChatEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-dead-chat-enabled-message" : "chat-manager-dead-chat-disabled-message"));
    }

    private void OnGameChange(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.InRound:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, false);
                break;
            case GameRunLevel.PostRound:
            case GameRunLevel.PreRoundLobby:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, true);
                break;
        }
    }

    /// <inheritdoc />
    public override void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        bool hideChat,
        bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false,
        Color? colorOverride = null, // Goobstation
        LanguagePrototype? languageOverride = null // EE
        )
    {
        TrySendInGameICMessage(source, message, desiredType, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, hideLog, shell, player, nameOverride, checkRadioPrefix, ignoreActionBlocker,
            colorOverride, languageOverride); // Goob
    }

    /// <inheritdoc />
    public override void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        ChatTransmitRange range,
        bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false,
        Color? colorOverride = null, // Goobstation
        LanguagePrototype? languageOverride = null // Einstein Engines - Language
        )
    {
        if (HasComp<GhostComponent>(source))
        {
            // Ghosts can only send dead chat messages, so we'll forward it to InGame OOC.
            TrySendInGameOOCMessage(source, message, InGameOOCChatType.Dead, range == ChatTransmitRange.HideChat, shell, player);
            return;
        }

        // Goobstation - Starlight collective mind port
        if (TryComp<CollectiveMindComponent>(source, out var collective))
            _collectiveMind.UpdateCollectiveMind(source, collective);

        // <Whiskey> - não cobrar duas vezes pela mesma fala, ver o reenvio da
        // tradução mais abaixo.
        if (!_reenviandoTraducao && player != null && _chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;
        // </Whiskey>

        // Sus
        if (player?.AttachedEntity is { Valid: true } entity && source != entity)
        {
            return;
        }

        if (!CanSendInGame(message, shell, player))
            return;

        ignoreActionBlocker = CheckIgnoreSpeechBlocker(source, ignoreActionBlocker);

        // this method is a disaster
        // every second i have to spend working with this code is fucking agony
        // scientists have to wonder how any of this was merged
        // coding any game admin feature that involves chat code is pure torture
        // changing even 10 lines of code feels like waterboarding myself
        // and i dont feel like vibe checking 50 code paths
        // so we set this here
        // todo free me from chat code
        if (player != null)
        {
            _chatManager.EnsurePlayer(player.UserId).AddEntity(GetNetEntity(source));
        }

        if (desiredType == InGameICChatType.Speak && message.StartsWith(LocalPrefix))
        {
            // prevent radios and remove prefix.
            checkRadioPrefix = false;
            message = message[1..];
        }

        // <Trauma> - let softcrit change speaking to whisper
        var typeEv = new SpeechTypeOverrideEvent(desiredType);
        RaiseLocalEvent(source, ref typeEv);
        desiredType = typeEv.DesiredType;
        // </Trauma>

        var language = languageOverride ?? _language.GetLanguage(source); // Einstein Engines - Language

        bool shouldCapitalize = (desiredType != InGameICChatType.Emote);
        bool shouldPunctuate = _configurationManager.GetCVar(CCVars.ChatPunctuation);
        // Capitalizing the word I only happens in English, so we check language here
        bool shouldCapitalizeTheWordI = (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
            || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en");

        message = SanitizeInGameICMessage(source, message, out var emoteStr, shouldCapitalize, shouldPunctuate, shouldCapitalizeTheWordI);

        // Was there an emote in the message? If so, send it.
        if (player != null && emoteStr != message && emoteStr != null)
        {
            SendEntityEmote(source, emoteStr, range, nameOverride, language, ignoreActionBlocker); // Einstein Engines - Language
        }

        // This can happen if the entire string is sanitized out.
        if (string.IsNullOrEmpty(message))
            return;

        // Goobstation start
        var colorEv = new GetMessageColorOverrideEvent();
        RaiseLocalEvent(source, colorEv);
        if (colorEv.Color != null)
            colorOverride = colorEv.Color.Value;
        // Goobstation end

        // This is really terrible. I hate myself for doing this. [-] Einstein Engines - Languages
        if (language.SpeechOverride.ChatTypeOverride is { } chatTypeOverride)
            desiredType = chatTypeOverride;

        // This message may have a radio prefix, and should then be whispered to the resolved radio channel
        if (checkRadioPrefix)
        {
            if (TryProcessRadioMessage(source, message, out var modMessage, out var channel))
            {
                // <Whiskey> - tradução de fala de rádio para quem está com um
                // tradutor. Aqui o prefixo de canal já foi separado da frase,
                // então só o texto vai para o tradutor e o roteamento fica
                // intacto. O reenvio chama direto o envio do rádio, mantendo o
                // canal escolhido.
                var evRadio = new _Whiskey.Translation.SpeechInterceptEvent(source, modMessage, desiredType,
                    traduzida => SendEntityWhisper(source, traduzida, range, channel, nameOverride, language, hideLog, ignoreActionBlocker, colorOverride));
                RaiseLocalEvent(source, evRadio);

                if (evRadio.Interceptado)
                    return;
                // </Whiskey>

                SendEntityWhisper(source, modMessage, range, channel, nameOverride, language, hideLog, ignoreActionBlocker, colorOverride); // Goob edit & Einstein Engines - Language
                return;
            }
        }

        // Goobstation - Starlight collective mind port
        if (desiredType == InGameICChatType.CollectiveMind)
        {
            if (TryProccessCollectiveMindMessage(source, message, out var modMessage, out var channel))
            {
                modMessage = FormattedMessage.RemoveMarkupOrThrow(modMessage); // Sanitize it so markup cannot be shown.

                if (collective != null && collective.RespectAccents)
                {
                    modMessage = TransformSpeech(source, modMessage, language); // Einstein Engines - Languages (I made null since it requires a language input)
                }

                SendCollectiveMindChat(source, modMessage, channel);
                return;
            }
        }

        // <Whiskey> - tradução de fala para quem está com um tradutor.
        // Aqui, e não antes, porque neste ponto a mensagem já foi higienizada e
        // o rádio já retornou, então só sobra fala local e o prefixo de canal
        // não corre o risco de ir parar no tradutor.
        // A frase traduzida volta por aqui, e por isso o limite de taxa é
        // ignorado no reenvio: quem falou já pagou por esta fala na primeira
        // passagem. Sem isso cada frase traduzida custaria o dobro, e quem usa
        // tradutor seria calado na metade do tempo dos outros. Pior: se o
        // limite estourasse justo no reenvio, a fala sumiria depois de já ter
        // sido aceita.
        var interceptEv = new _Whiskey.Translation.SpeechInterceptEvent(source, message, desiredType,
            traduzida =>
            {
                _reenviandoTraducao = true;
                try
                {
                    TrySendInGameICMessage(source, traduzida, desiredType, range, hideLog, shell, player,
                        nameOverride, false, ignoreActionBlocker, colorOverride, languageOverride);
                }
                finally
                {
                    _reenviandoTraducao = false;
                }
            });
        RaiseLocalEvent(source, interceptEv);

        if (interceptEv.Interceptado)
            return;
        // </Whiskey>

        // Otherwise, send whatever type.
        switch (desiredType)
        {
            case InGameICChatType.Speak:
                SendEntitySpeak(source, message, range, nameOverride, language, hideLog, ignoreActionBlocker, colorOverride); // Goob edit & Einstein Engines - Language
                break;
            case InGameICChatType.Whisper:
                SendEntityWhisper(source, message, range, null, nameOverride, language, hideLog, ignoreActionBlocker, colorOverride); // Goob edit & Einstein Engines - Language
                break;
            case InGameICChatType.Emote:
                SendEntityEmote(source, message, range, nameOverride, language, hideLog: hideLog, ignoreActionBlocker: ignoreActionBlocker); // Einstein Engines - Language
                break;
        }
    }

    /// <inheritdoc />
    public override void TrySendInGameOOCMessage(
        EntityUid source,
        string message,
        InGameOOCChatType type,
        bool hideChat,
        IConsoleShell? shell = null,
        ICommonSession? player = null
        )
    {
        if (!CanSendInGame(message, shell, player))
            return;

        if (player != null && _chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;

        // It doesn't make any sense for a non-player to send in-game OOC messages, whereas non-players may be sending
        // in-game IC messages.
        if (player?.AttachedEntity is not { Valid: true } entity || source != entity)
            return;

        message = SanitizeInGameOOCMessage(message);

        var sendType = type;
        // If dead player LOOC is disabled, unless you are an admin with Moderator perms, send dead messages to dead chat
        if ((_adminManager.IsAdmin(player) && _adminManager.HasAdminFlag(player, AdminFlags.Moderator)) // Override if admin
            || _deadLoocEnabled
            || (!HasComp<GhostComponent>(source) && !_mobStateSystem.IsDead(source))) // Check that player is not dead
        {
        }
        else
            sendType = InGameOOCChatType.Dead;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        // Systems can differentiate Looc and DeadChat by type, and cancel the speak attempt if necessary.
        var ev = new InGameOocMessageAttemptEvent(player, sendType);
        RaiseLocalEvent(source, ref ev, true);
        if (ev.Cancelled)
            return;

        switch (sendType)
        {
            case InGameOOCChatType.Dead:
                SendDeadChat(source, player, message, hideChat);
                break;
            case InGameOOCChatType.Looc:
                SendLOOC(source, player, message, hideChat);
                break;
        }
    }
}

/// <summary>
///     This event is raised before chat messages are sent out to clients. This enables some systems to send the chat
///     messages to otherwise out-of view entities (e.g. for multiple viewports from cameras).
/// </summary>
public record ExpandICChatRecipientsEvent(EntityUid Source, float VoiceRange, Dictionary<ICommonSession, ChatSystem.ICChatRecipientData> Recipients)
{
}
