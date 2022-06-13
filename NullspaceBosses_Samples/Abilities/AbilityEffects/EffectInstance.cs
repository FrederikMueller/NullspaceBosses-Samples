using Cysharp.Threading.Tasks;
using RPG.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NullspaceBosses_Samples.Abilities.AbilityEffects;
public class EffectInstance
{
    public EffectInstance(CharacterStatusAPI source, CharacterStatusAPI target, Vector3 targetPoint, Effect effect)
    {
        Source = source;
        Target = target;
        TargetPoint = targetPoint;
        Effect = effect;
    }
    public CharacterStatusAPI Source { get; set; }
    public CharacterStatusAPI Target { get; set; }
    public Vector3 TargetPoint { get; set; }
    public Effect Effect { get; set; }

    public void Execute() => Effect.ExecuteEffect(this);
}
