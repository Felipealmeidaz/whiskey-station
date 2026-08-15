namespace Content.Server._Whiskey.Translation;

/// <summary>
/// Perguntado a cada ouvinte antes de entregar uma fala local: você quer isto
/// em algum idioma específico?
/// </summary>
/// <remarks>
/// <para>
/// Existe pelo mesmo motivo do <see cref="SpeechInterceptEvent"/>: quem sabe
/// responder mora em <c>Content.Trauma.Server</c>, que enxerga
/// <c>Content.Server</c> e não o contrário. Um evento resolve sem inverter o
/// grafo de projetos.
/// </para>
/// <para>
/// Deixar <see cref="Idioma"/> nulo significa "manda como está", que é o caso
/// da esmagadora maioria dos ouvintes.
/// </para>
/// </remarks>
public sealed class ListenerLanguageEvent : EntityEventArgs
{
    public ListenerLanguageEvent(EntityUid ouvinte)
    {
        Ouvinte = ouvinte;
    }

    public EntityUid Ouvinte { get; }

    /// <summary>
    /// Idioma em que este ouvinte quer receber, ou nulo para não traduzir.
    /// </summary>
    public string? Idioma { get; set; }
}
