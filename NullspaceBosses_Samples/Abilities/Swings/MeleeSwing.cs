using RPG.Database;
using System.Linq;
using UnityEngine;

namespace RPG.Combat
{
    public class MeleeSwing : Ability
    {
        public float Windup { get; set; }
        public float DownSwing { get; set; }
        public float Recovery { get; set; }
        public bool IsMovingSwing { get; set; }
        public int Arc { get; set; }
        public string CustomType { get; set; }

        public override void Init()
        {
            // Get Effect from DB
        }

        public virtual void CalculateValues()
        {
            //HPValue = Mathf.RoundToInt(HPValue * Owner.stats.GetPercStat(StatEnum.IncGlobalDmg));
            //PoiseValue = Mathf.RoundToInt(PoiseValue * Owner.stats.GetPercStat(StatEnum.IncPoiseDmg));
        }

        public override void Execute()
        {
            CalculateValues();
            Source.Fighter.BroadcastSound(ExecSound);

            foreach (var hit in Physics.OverlapSphere(Source.gameObject.transform.position, Range))
            {
                if (AbilityExecutionSystem.MeleeHitArcCheck(Source.gameObject, hit.gameObject, Arc))
                {
                    if (hit.gameObject.GetComponent<CharacterStatusAPI>() && hit.gameObject.tag != Source.gameObject.tag)
                    {
                        Source.Fighter.BroadcastSound(HitSound);

                        new EffectInstance(Source, hit.gameObject.GetComponent<CharacterStatusAPI>(), hit.transform.position, AbilityEffect).Execute();
                    }
                }
            }
        }
    }

    // Specialized versions of the MeleeSwing
    // Separate out once you add more
    public class OverheadSmash : MeleeSwing
    {
        // Extra Props
        public int ProcDamage { get; set; }
        public override void Execute()
        {
            CalculateValues();
            Source.Fighter.BroadcastSound(ExecSound);

            foreach (var hit in Physics.OverlapSphere(Source.gameObject.transform.position, Range))
            {
                if (AbilityExecutionSystem.MeleeHitArcCheck(Source.gameObject, hit.gameObject, Arc))
                {
                    if (hit.gameObject.GetComponent<CharacterStatusAPI>() && hit.gameObject.tag != Source.gameObject.tag)
                    {
                        Source.Fighter.BroadcastSound(HitSound);

                        new EffectInstance(Source, hit.gameObject.GetComponent<CharacterStatusAPI>(), hit.transform.position, AbilityEffect).Execute();
                    }
                }
            }
        }
    }

    public class TestSmash : MeleeSwing
    {
        public override void Execute()
        {
            HPValue = Mathf.RoundToInt(HPValue * Source.Stats.CurrentMaxHP * 0.1f);
        }
    }
}