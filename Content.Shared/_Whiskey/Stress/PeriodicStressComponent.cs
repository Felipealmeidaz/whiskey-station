// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Whiskey.Stress;

/// <summary>
/// Empurra o estresse para cima de tempos em tempos, sozinho.
///
/// É o que a depressão usa: não existe gatilho no ambiente, o baque vem de
/// dentro e em hora imprevisível. Serve igual para química ou evento que
/// precise disso, por isso não se chama Depressão.
///
/// Precisa de um <c>StressComponent</c> junto para ter onde empurrar.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PeriodicStressComponent : Component
{
    /// <summary>
    /// Quanto sobe a cada episódio.
    /// </summary>
    [DataField]
    public float Amount = 35f;

    /// <summary>
    /// Menor espera entre episódios, em segundos.
    /// </summary>
    [DataField]
    public float MinTimeBetween = 240f;

    /// <summary>
    /// Maior espera entre episódios, em segundos. O intervalo é largo de
    /// propósito: episódio em hora previsível vira relógio, e a pessoa passa a
    /// planejar em volta dele em vez de ser pega por ele.
    /// </summary>
    [DataField]
    public float MaxTimeBetween = 900f;

    /// <summary>
    /// Frases que a pessoa lê quando o episódio bate. Opcional: sem isto o
    /// episódio é silencioso e só se percebe pelo efeito.
    /// </summary>
    [DataField]
    public string? Message;

    /// <summary>
    /// Quando vem o próximo. Pausa junto com a entidade, senão a rodada volta
    /// de uma pausa com todos os episódios atrasados disparando de uma vez.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextEpisode;
}
