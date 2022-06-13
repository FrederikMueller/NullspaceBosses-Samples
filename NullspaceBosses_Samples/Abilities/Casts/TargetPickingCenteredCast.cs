using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public class TargetPickingCenteredCast : CenteredCast
    {
        public Targeting Targeting { get; set; }
        public Func<Vector3, float, CharacterStatusAPI, List<GameObject>> TargetPicker;

        public override void Init()
        {
            base.Init();

            if (Targeting == Targeting.All)
                TargetPicker += Source.AES.PickAllInRange;
            if (Targeting == Targeting.Enemies)
                TargetPicker += Source.AES.PickAllEnemiesInRange;
            if (Targeting == Targeting.Allies)
                TargetPicker += Source.AES.PickAllAlliesInRange;
        }
    }

    // AoE Targeting
    public class AreaTargeting : TargetPickingCenteredCast
    {
        public float Radius { get; set; }

        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            SetCenterPoint();

            foreach (var target in TargetPicker(SourcePoint, Radius, Source))
            {
                var e = new EffectInstance(Source, target.GetComponent<CharacterStatusAPI>(), target.transform.position, AbilityEffect);
                e.Execute();
            }
        }
    }
    public class MultiAreaTargeting : TargetPickingCenteredCast
    {
        public float SpawnRadius { get; set; }
        public int AreaCount { get; set; }
        public float Radius { get; set; }

        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            SetCenterPoint();

            for (int i = 0; i < AreaCount; i++)
            {
                var center = Source.AES.PickRandomPointInRange(SourcePoint, SpawnRadius);

                foreach (var target in TargetPicker(center, Radius, Source))
                {
                    var e = new EffectInstance(Source, target.GetComponent<CharacterStatusAPI>(), target.transform.position, AbilityEffect);
                    e.Execute();
                }
            }
        }
    }
}