// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Whiskey.Stress;

/// <summary>
/// Medida de quanto a pessoa está abalada, de 0 a 100.
///
/// Isto **não** é o sistema de humor do TG, e a diferença é proposital. Lá o
/// humor é um saldo com dezenas de modificadores nomeados, que sobem e descem
/// por comida, higiene, dor e área, e o valor pode ser positivo. Aqui é uma
/// medida só, que sobe com gatilho e desce sozinha com o tempo.
///
/// A escolha foi de escopo: os traços mentais precisam de um número que sobe e
/// dispara efeito por faixa, e nada mais. Portar o humor inteiro significa
/// mexer em comida, higiene e ambiente do jogo todo, que é outra obra e outra
/// decisão de desenho.
///
/// Quem sobe o valor é quem tem o gatilho, chamando <c>Adicionar</c>. Este
/// componente não sabe de traço nenhum de propósito, para servir também a
/// química, evento e o que vier depois.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class StressComponent : Component
{
    /// <summary>
    /// Valor atual, de 0 a 100. Vai networkado porque a interface pode querer
    /// mostrar isso um dia, e porque facilita depurar por View Variables.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Current;

    /// <summary>
    /// Quanto o valor cai por segundo quando nada está empurrando para cima.
    /// No padrão, sair do topo até zero leva pouco mais de um minuto e meio.
    /// </summary>
    [DataField]
    public float DecayPerSecond = 1f;

    /// <summary>
    /// A partir daqui a pessoa começa a enxergar embaçado.
    /// </summary>
    [DataField]
    public float MildThreshold = 40f;

    /// <summary>
    /// A partir daqui ela também começa a gaguejar.
    /// </summary>
    [DataField]
    public float MediumThreshold = 70f;

    /// <summary>
    /// A partir daqui ela também fica mais lenta.
    /// </summary>
    [DataField]
    public float HighThreshold = 90f;

    /// <summary>
    /// Efeitos aplicados em cada faixa. São prototypes que já existem no
    /// repositório, nenhum foi escrito para isto.
    /// </summary>
    [DataField]
    public EntProtoId MildEffect = "StatusEffectBlurryVision";

    /// <inheritdoc cref="MildEffect"/>
    [DataField]
    public EntProtoId MediumEffect = "StatusEffectStutter";

    /// <inheritdoc cref="MildEffect"/>
    [DataField]
    public EntProtoId HighEffect = "StatusEffectWhiskeyStressSlowdown";

    /// <summary>
    /// O sistema não roda todo tique. Este é o instante do próximo passo.
    /// Pausa junto com a entidade, senão o estresse despenca de uma vez quando
    /// a rodada volta de uma pausa.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate;
}
