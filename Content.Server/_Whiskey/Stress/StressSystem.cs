// SPDX-FileCopyrightText: 2026 Zequinza <felipe828218@gmail.com>
// SPDX-FileCopyrightText: 2026 Whiskey Station Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Whiskey.Stress;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server._Whiskey.Stress;

/// <summary>
/// Faz o estresse cair sozinho e aplica o efeito da faixa em que ele está.
///
/// Fica no servidor porque aplicar status effect é autoridade dele. A conta em
/// si é determinística e caberia em shared, mas dividir o sistema em dois para
/// prever uma barra que ninguém desenhou ainda seria trabalho sem uso.
/// </summary>
public sealed partial class StressSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _efeitos = default!;

    /// <summary>
    /// De quanto em quanto tempo o sistema mexe no valor. Um segundo é o
    /// suficiente: estresse é coisa de dezenas de segundos, e rodar todo tique
    /// só gastaria CPU para mudar centésimo.
    /// </summary>
    private static readonly TimeSpan Passo = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Duração dada ao efeito de faixa. Precisa ser maior que o passo, senão o
    /// efeito pisca entre uma atualização e outra.
    /// </summary>
    private static readonly TimeSpan DuracaoDoEfeito = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Sobe o estresse de alguém. É por aqui que traço, química ou evento
    /// empurram a medida, e é a única forma de subir.
    /// </summary>
    public void Adicionar(EntityUid uid, float quanto, StressComponent? estresse = null)
    {
        if (!Resolve(uid, ref estresse, false))
            return;

        Definir(uid, estresse.Current + quanto, estresse);
    }

    /// <summary>
    /// Escreve o valor, sempre preso entre 0 e 100.
    /// </summary>
    public void Definir(EntityUid uid, float valor, StressComponent? estresse = null)
    {
        if (!Resolve(uid, ref estresse, false))
            return;

        var novo = Math.Clamp(valor, 0f, 100f);
        if (MathHelper.CloseTo(novo, estresse.Current))
            return;

        estresse.Current = novo;
        Dirty(uid, estresse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var agora = _timing.CurTime;

        // O StressComponent é raro, então ele corta a busca primeiro.
        var consulta = EntityQueryEnumerator<StressComponent>();
        while (consulta.MoveNext(out var uid, out var estresse))
        {
            if (agora < estresse.NextUpdate)
                continue;

            estresse.NextUpdate = agora + Passo;

            if (estresse.Current > 0f)
                Definir(uid, estresse.Current - estresse.DecayPerSecond * (float) Passo.TotalSeconds, estresse);

            AplicarFaixa(uid, estresse);
        }
    }

    /// <summary>
    /// Renova o efeito de cada faixa que o valor alcançou.
    ///
    /// Renovar, e não somar: o <c>TryAddStatusEffectDuration</c> acrescenta
    /// tempo ao que já existe, então chamar de segundo em segundo empilharia
    /// duração para sempre e a pessoa ficaria embaçada muito depois de ter se
    /// acalmado. O <c>TryUpdateStatusEffectDuration</c> define, e é o certo aqui.
    /// </summary>
    private void AplicarFaixa(EntityUid uid, StressComponent estresse)
    {
        if (estresse.Current < estresse.MildThreshold)
            return;

        _efeitos.TryUpdateStatusEffectDuration(uid, estresse.MildEffect, DuracaoDoEfeito);

        if (estresse.Current >= estresse.MediumThreshold)
            _efeitos.TryUpdateStatusEffectDuration(uid, estresse.MediumEffect, DuracaoDoEfeito);

        if (estresse.Current >= estresse.HighThreshold)
            _efeitos.TryUpdateStatusEffectDuration(uid, estresse.HighEffect, DuracaoDoEfeito);
    }
}
