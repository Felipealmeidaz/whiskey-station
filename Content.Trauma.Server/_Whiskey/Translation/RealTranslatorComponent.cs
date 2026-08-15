namespace Content.Trauma.Server._Whiskey.Translation;

/// <summary>
/// Marca um tradutor como tradutor de verdade, e diz qual idioma ele atende.
/// </summary>
/// <remarks>
/// <para>
/// Vai junto de <c>HandheldTranslator</c>, <c>TranslatorImplant</c> ou
/// <c>IntrinsicTranslator</c>, e não no lugar deles. Os componentes herdados do
/// Einstein Engines já cuidam de ligar e desligar, gastar célula, aparecer no
/// exame e mostrar a luzinha, e nada disso precisa ser reescrito. Este
/// componente só acrescenta a parte que não existia, que é traduzir de verdade
/// em vez de embaralhar sílaba.
/// </para>
/// <para>
/// O idioma aqui é o <b>do dono</b>, não o de destino. Um russo põe "ru", e daí
/// saem as duas direções sozinhas: o que ele fala vira o idioma da estação, e o
/// que ele escuta vira russo. É por isso que um item só resolve os dois
/// sentidos, sem precisar de um tradutor para cada lado.
/// </para>
/// </remarks>
[RegisterComponent]
public sealed partial class RealTranslatorComponent : Component
{
    /// <summary>
    /// O idioma de quem carrega, em código curto: "pt", "en" ou "ru".
    /// </summary>
    [DataField]
    public string Idioma = "ru";
}
