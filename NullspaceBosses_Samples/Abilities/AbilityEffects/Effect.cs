using Cysharp.Threading.Tasks;
using RPG.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public abstract class Effect
    {
        // Dynamic Data
        public Ability OriginAbility { get; set; }

        public int HPDamage { get; set; }
        public int Hphealing { get; set; }
        public int PoiseDamage { get; set; }
        public int PoiseHealing { get; set; }

        // Mostly Static Data
        public string Name { get; set; }
        public School School { get; set; }
        public bool IsDamaging { get; set; }
        public List<Buff> Buffs { get; set; }
        public List<Debuff> Debuffs { get; set; }
        public Ability OnHitAbility { get; set; }

        public string StartSound { get; set; }
        public string DuringSound { get; set; }
        public string EndSound { get; set; }

        public string Model { get; set; }
        public string VFX1 { get; set; }
        public string VFX2 { get; set; }

        public void UpdateValues()
        {
            // For now update it on every execution, easier than to track every fucking thing that might change the values
            // ALPHA FORMULAS, SIMPLE AND FUNCTIONAL
            if (IsDamaging && OriginAbility.HPValue + OriginAbility.PoiseValue > 0)
            {
                float multi = 1 + (OriginAbility.Source.Stats.GetStat(StatEnum.IncGlobalDmg) + OriginAbility.Source.Stats.GetDmgIncForSchool(School));

                HPDamage = Mathf.RoundToInt(OriginAbility.HPValue * multi);
                PoiseDamage = Mathf.RoundToInt(OriginAbility.PoiseValue * multi);
            }

            if (!IsDamaging && OriginAbility.HPValue + OriginAbility.PoiseValue > 0)
            {
                float multi = 1 + (OriginAbility.Source.Stats.GetStat(StatEnum.IncGlobalDmg) + OriginAbility.Source.Stats.GetDmgIncForSchool(School));

                Hphealing = Mathf.RoundToInt(OriginAbility.HPValue * multi);
                PoiseHealing = Mathf.RoundToInt(OriginAbility.PoiseValue * multi);
            }
        }
        public abstract void ExecuteEffect(EffectInstance e);
    }

    // Specific implementations
    // Separate these out into their own files
    //
    public class NoEffectAE : Effect
    {
        public override void ExecuteEffect(EffectInstance e)
        {
        }
    }
    public class StanceEffect : Effect
    {
        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();

            e.Source.Fighter.BroadcastSound(EndSound);

            OriginAbility.OnFinish += RemoveEffects;
            e.Source.ComputeHit(e);
        }

        public void RemoveEffects()
        {
            foreach (var buff in Buffs)
            {
                OriginAbility.Source.RemoveBuffviaID(buff.Name);
            }
            OriginAbility.OnFinish -= RemoveEffects; // effect init
        }
    }

    public class TargetAE : Effect
    {
        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();

            OriginAbility.Source.Fighter.BroadcastSound(EndSound);

            e.Target.ComputeHit(e);
        }
    }

    // inherit from proj ae? consider inheritence
    public class ProjectileAE : Effect
    {
        public float SizeMulti { get; set; }
        public float Speed { get; set; }
        public Vector3 Direction { get; set; }

        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();

            GameObject proj = OriginAbility.Source.AES.ProjectileSpawner(Name);

            proj.transform.position = new Vector3(e.Source.transform.position.x, 2f, e.Source.transform.position.z);
            proj.transform.localScale *= SizeMulti;

            var projScript = proj.GetComponent<StraightProjectile>();

            projScript.OriginAbility = (ProjectileCast)OriginAbility;
            projScript.EffectInstance = e;

            projScript.Speed = Speed;
            var targetPos = new Vector3(e.TargetPoint.x, 2f, e.TargetPoint.z);

            projScript.Direction = targetPos - proj.transform.position;

            projScript.SetTargetAndGo(SizeMulti);
        }
    }
    public class WaveAE : Effect
    {
        public float SizeMulti { get; set; }
        public float Speed { get; set; }
        public Vector3 Direction { get; set; }

        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();

            GameObject proj = OriginAbility.Source.AES.WaveSpawner();
            proj.transform.position = new Vector3(e.Source.transform.position.x, 2f, e.Source.transform.position.z);
            proj.transform.localScale *= SizeMulti;

            var projScript = proj.GetComponent<StraightProjectile>();

            projScript.OriginAbility = (ProjectileCast)OriginAbility;
            projScript.EffectInstance = e;

            projScript.Speed = Speed;

            var targetPos = new Vector3(e.TargetPoint.x, 2f, e.TargetPoint.z);
            projScript.Direction = targetPos - proj.transform.position;

            projScript.passThrough = true;
            projScript.SetTargetAndGo(SizeMulti);
        }
    }
    public class HookAE : Effect
    {
        public float SizeMulti { get; set; }
        public float Speed { get; set; }
        public Vector3 Direction { get; set; }

        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();

            GameObject proj = e.Source.AES.ProjectileSpawner(Name);
            proj.transform.position = new Vector3(e.Target.transform.position.x, 2f, e.Target.transform.position.z);
            proj.transform.localScale *= SizeMulti;

            var projScript = proj.GetComponent<StraightProjectile>();
            projScript.OriginAbility = (ProjectileCast)OriginAbility;
            projScript.EffectInstance = e;

            projScript.Speed = Speed;
            var targetPos = new Vector3(e.Source.transform.position.x, 2f, e.Source.transform.position.z);
            projScript.Direction = targetPos - proj.transform.position;

            projScript.DragTarget = e.Target.gameObject;

            projScript.SetTargetAndGo(SizeMulti);
        }
    }
    public class DelayedHealStrike : Effect
    {
        public float Delay { get; set; }
        public float EffectRadius { get; set; }

        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();
            e.Source = OriginAbility.Source;
            // Initiate stuff @ Position
            // Start VFX / Sound

            AndStrike(e).Forget();
        }

        public async UniTaskVoid AndStrike(EffectInstance e)
        {
            e.Source.AES.RpcSpawnVFXTimed(VFX1, e.TargetPoint, null, Delay);

            await UniTask.Delay(TimeSpan.FromSeconds(Delay), ignoreTimeScale: false);
            if (e.Source != null)
            {
                e.Source.AES.RpcSpawnVFXTimed(VFX2, e.TargetPoint, EndSound, 2);

                foreach (var target in e.Source.AES.PickAllAlliesInRange(e.TargetPoint, EffectRadius, e.Source))
                {
                    target.GetComponent<CharacterStatusAPI>().ComputeHit(e);
                }
            }
        }
    }
    public class DelayedStrikeAE : Effect
    {
        public float Delay { get; set; }
        public float EffectRadius { get; set; }

        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();
            e.Source = OriginAbility.Source;
            // Initiate stuff @ Position
            // Start VFX / Sound

            AndStrike(e).Forget();
        }

        public async UniTaskVoid AndStrike(EffectInstance e)
        {
            e.Source.AES.RpcSpawnVFXTimed(VFX1, e.TargetPoint, null, Delay);

            await UniTask.Delay(TimeSpan.FromSeconds(Delay), ignoreTimeScale: false);
            if (e.Source != null)
            {
                e.Source.AES.RpcSpawnVFXTimed(VFX2, e.TargetPoint, EndSound, 2);

                foreach (var target in e.Source.AES.PickAllEnemiesInRange(e.TargetPoint, EffectRadius, e.Source))
                {
                    target.GetComponent<CharacterStatusAPI>().ComputeHit(e);
                }
            }
        }
    }
    public class Mine : Effect
    {
        public float SizeMulti { get; set; }
        public float Duration { get; set; }

        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();

            GameObject mine = e.Source.AES.MineSpawner();
            mine.transform.position = new Vector3(e.TargetPoint.x, e.TargetPoint.y, e.TargetPoint.z);
            mine.transform.localScale *= SizeMulti;

            var mineScript = mine.GetComponent<MineObject>();
            mineScript.OriginAbility = OriginAbility;
            mineScript.OriginEffect = this;
            mineScript.EffectInstance = e;

            mineScript.Arm();
        }
    }
    public class BlinkStrikeAE : Effect
    {
        public override void ExecuteEffect(EffectInstance e)
        {
            UpdateValues();

            OriginAbility.Source.MovePlayer((e.TargetPoint + e.Target.gameObject.transform.position) / 2f);
            //Owner.RotatePlayerTowards(obj.gameObject.transform.position);
            // client has authority over his own movement => blinkstrike must call it on player obj
            OriginAbility.Source.Fighter.BroadcastSound(EndSound);

            e.Target.ComputeHit(e);
        }
    }
    public class BugAlertAbilityEffect : Effect
    {
        public override void ExecuteEffect(EffectInstance e)
        {
        }
    }
}