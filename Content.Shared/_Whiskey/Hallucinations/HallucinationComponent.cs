// SPDX-FileCopyrightText: 2026 Zequinza <felipe828218@gmail.com>
// SPDX-FileCopyrightText: 2026 Whiskey Station Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Whiskey.Hallucinations;

/// <summary>
/// Faz a pessoa perceber coisas que não estão lá. Serve de motor para traços
/// como esquizofrenia, e também para efeito temporário de química ou de evento.
///
/// São dois canais independentes, e cada um pode ser ligado sozinho:
///
/// 1. Som, delegado ao <c>ParacusiaComponent</c>, que já toca som posicional
///    falso no cliente. Não vale reescrever isso aqui.
/// 2. Fala, que aparece só para quem tem o componente, como popup ou como
///    linha de chat.
///
/// Cada canal tem o próprio relógio, porque a cadência natural dos dois é
/// diferente: som ambiente cansa menos que voz falando com você.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class HallucinationComponent : Component
{
    /// <summary>
    /// Sons falsos. Quando preenchido, o sistema cuida de por e configurar o
    /// <c>ParacusiaComponent</c> sozinho, e de tirar junto quando a alucinação
    /// acabar.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sounds;

    /// <summary>
    /// Distância máxima de onde o som falso parece vir.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxSoundDistance = 7f;

    /// <summary>
    /// Intervalo do canal de som, em segundos, repassado à paracusia.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinTimeBetweenSounds = 20f;

    /// <inheritdoc cref="MinTimeBetweenSounds"/>
    [DataField, AutoNetworkedField]
    public float MaxTimeBetweenSounds = 60f;

    /// <summary>
    /// Conjunto de frases que a pessoa vai ler no chat como se alguém tivesse
    /// falado. Usa dataset localizado para a tradução não depender de código.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Messages;

    /// <summary>
    /// Escolhe o canal da frase: popup em cima do personagem quando ligado,
    /// linha de chat quando desligado. **Um ou outro, nunca os dois.**
    ///
    /// Ligado por padrão, por sugestão de quem testou. No chat a frase cai no
    /// meio dos comunicados da estação e se perde, ou pior, é confundida com
    /// mensagem de verdade. E mandar nos dois mostrava a mesma frase duas
    /// vezes seguidas, o que faz voz na cabeça virar mensagem de sistema.
    /// </summary>
    [DataField]
    public bool Popup = true;

    /// <summary>
    /// Intervalo do canal de fala, em segundos.
    /// </summary>
    [DataField]
    public float MinTimeBetweenMessages = 90f;

    /// <inheritdoc cref="MinTimeBetweenMessages"/>
    [DataField]
    public float MaxTimeBetweenMessages = 240f;

    /// <summary>
    /// Quando a próxima fala falsa acontece. Pausa junto com a entidade, senão
    /// a alucinação dispara em rajada quando a rodada volta de uma pausa.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextMessageTime;

    /// <summary>
    /// Se foi esta alucinação que pôs a paracusia na pessoa. Quem já tinha o
    /// traço de paracusia por conta própria não pode perdê-lo quando a
    /// alucinação acabar. Estado de execução, não vai no YAML.
    /// </summary>
    [ViewVariables]
    public bool PosParacusia;

    // O canal de visão ainda não existe, e por isso não tem campo aqui.
    //
    // A saída óbvia seria uma camada do VisibilityFlags, como o fantasma usa,
    // mas ela não serve: a camada é global, então todo mundo que alucinasse
    // enxergaria a aparição dos outros, o que estraga a coisa toda. Isolamento
    // por jogador exige entidade criada só no cliente de quem alucina.
    //
    // Fica para PR própria, para este motor poder entrar já funcionando nos
    // dois canais que estão prontos.
}
