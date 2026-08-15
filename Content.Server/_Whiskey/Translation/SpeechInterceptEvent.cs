using Content.Shared.Chat;

namespace Content.Server._Whiskey.Translation;

/// <summary>
/// Perguntado pelo <c>ChatSystem</c> antes de entregar uma fala local: alguém
/// quer segurar esta frase e entregar depois?
/// </summary>
/// <remarks>
/// <para>
/// Existe porque quem sabe traduzir mora em <c>Content.Trauma.Server</c>, que
/// referencia <c>Content.Server</c>, e não o contrário. Chamar direto exigiria
/// inverter o grafo de projetos ou fazer <c>Content.Server</c> depender do
/// Trauma, e as duas coisas são mudança grande em arquivo que todo mundo mexe.
/// Um evento resolve sem tocar em referência nenhuma.
/// </para>
/// <para>
/// Quem intercepta assume a entrega: marca <see cref="Interceptado"/> e fica
/// responsável por chamar <see cref="Reenviar"/> depois, senão a fala se perde.
/// </para>
/// </remarks>
public sealed class SpeechInterceptEvent : EntityEventArgs
{
    public SpeechInterceptEvent(EntityUid falante, string mensagem, InGameICChatType tipo, Action<string> reenviar)
    {
        Falante = falante;
        Mensagem = mensagem;
        Tipo = tipo;
        Reenviar = reenviar;
    }

    public EntityUid Falante { get; }

    public string Mensagem { get; }

    public InGameICChatType Tipo { get; }

    /// <summary>
    /// Entrega a frase, já tratada, pelo caminho normal do chat.
    /// </summary>
    public Action<string> Reenviar { get; }

    /// <summary>
    /// Marcado por quem assumiu a entrega. Verdadeiro faz o <c>ChatSystem</c>
    /// parar aqui.
    /// </summary>
    public bool Interceptado { get; set; }
}
