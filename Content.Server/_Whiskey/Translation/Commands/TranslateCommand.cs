using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Whiskey.Translation.Commands;

/// <summary>
/// Traduz uma frase e fala em voz alta o resultado. Ferramenta de teste.
/// </summary>
/// <remarks>
/// <para>
/// <b>Só administração.</b> Quem joga traduz usando o tradutor, que é um item
/// com custo: sai da sci, depende de pesquisa e dá para perder. Deixar este
/// comando aberto para todo mundo daria de graça exatamente o que o item
/// existe para cobrar, e a mecânica inteira perderia o sentido.
/// </para>
/// <para>
/// O que sobra dele é diagnóstico: dá para conferir se o serviço de tradução
/// responde, e qual o erro quando não responde, sem precisar entrar em jogo com
/// um tradutor na mão.
/// </para>
/// <para>
/// A tradução é assíncrona, então o comando devolve na hora e a fala sai alguns
/// décimos depois, quando o resultado volta.
/// </para>
/// </remarks>
[AdminCommand(AdminFlags.Debug)]
public sealed partial class TranslateCommand : LocalizedEntityCommands
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private TranslationSystem _translation = default!;

    /// <summary>
    /// Para onde dá para traduzir. Bate com os pares que o serviço carrega.
    /// </summary>
    private static readonly string[] Idiomas = { "pt", "en", "ru" };

    public override string Command => "tr";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { } falante)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-tr-help"));
            return;
        }

        var alvo = args[0].ToLowerInvariant();

        if (!Idiomas.Contains(alvo))
        {
            shell.WriteError(Loc.GetString("cmd-tr-idioma-invalido",
                ("idioma", alvo),
                ("aceitos", string.Join(", ", Idiomas))));
            return;
        }

        if (!_translation.CanTranslate)
        {
            shell.WriteError(Loc.GetString("cmd-tr-desligado"));
            return;
        }

        var mensagem = string.Join(" ", args.Skip(1)).Trim();

        if (string.IsNullOrEmpty(mensagem))
            return;

        var origem = _translation.DetectarIdioma(mensagem);

        if (origem == alvo)
        {
            shell.WriteError(Loc.GetString("cmd-tr-mesmo-idioma", ("idioma", alvo)));
            return;
        }

        _translation.Translate(mensagem, origem, alvo, resultado =>
        {
            // A resposta chega alguns centésimos depois, e nesse meio tempo o
            // jogador pode ter morrido, saído ou trocado de corpo. Falar por uma
            // entidade que não existe mais derruba o chat.
            if (EntityManager.Deleted(falante) || player.AttachedEntity != falante)
                return;

            if (!resultado.Success)
            {
                shell.WriteError(Loc.GetString("cmd-tr-falhou", ("motivo", resultado.Error ?? "desconhecido")));
                return;
            }

            _chat.TrySendInGameICMessage(
                falante,
                resultado.Text,
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                false,
                shell,
                player);
        });
    }
}
