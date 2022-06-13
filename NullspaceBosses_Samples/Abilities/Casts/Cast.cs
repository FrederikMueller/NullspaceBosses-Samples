using RPG.Control;

namespace RPG.Combat
{
    public enum CastVariant { Cast, Channel, Charge, Stance }
    public enum Targeting { All, Enemies, Allies }
    public enum CenterSource { Caster, RandomEnemy, RandomPointInRange, AverageEnemyPos, TargetPoint }

    public class Cast : Ability
    {
        public float CastTime { get; set; }
        public float CastReady { get; set; }
        public int Charges { get; set; }
        public int ChargeCap { get; set; }

        public CastVariant CastVariant { get; set; }
        public bool IsMovingCast { get; set; }
        public string CustomType { get; set; }
    }

    // If you use CastLocation in a direct Cast, set it during Execute. OnHit Abilities get their's set in the onhit call. Otherwise its empty.
    public class SelfCast : Cast
    {
        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            // Get effect instance (pooled)
            new EffectInstance(Source, Source, TargetPoint, AbilityEffect).Execute();
        }
    }
    public class TauntCast : Cast
    {
        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            // Get effect instance (pooled)
            new EffectInstance(Source, Source, TargetPoint, AbilityEffect).Execute();
            foreach (var enemy in Source.AES.PickAllEnemiesInRange(Source.transform.position, Range, Source))
            {
                if (enemy.TryGetComponent<AIController>(out var aIController))
                {
                    aIController.AIMind.PickTarget(Source.gameObject);
                }
            }
        }
    }
    public class ClosestEnemyToPoint : Cast
    {
        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            var e = new EffectInstance(
                Source,
                Source.AES.PickClosestEnemy(TargetPoint, Source)?.GetComponent<CharacterStatusAPI>(),
                TargetPoint,
                AbilityEffect);

            if (e.Target != null)
                e.Execute();
        }
    }
    public class ClosestAllyToPoint : Cast
    {
        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            var e = new EffectInstance(
                Source,
                Source.AES.PickClosestAlly(TargetPoint, Source)?.GetComponent<CharacterStatusAPI>(),
                TargetPoint,
                AbilityEffect);

            if (e.Target != null)
                e.Execute();
        }
    }
    public class ClosestEntityToPoint : Cast
    {
        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            var e = new EffectInstance(
                Source,
                Source.AES.PickClosestAll(TargetPoint)?.GetComponent<CharacterStatusAPI>(),
                TargetPoint,
                AbilityEffect);

            if (e.Target != null)
                e.Execute();
        }
    }
    public class PhaseTransition : Cast
    {
        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            // IMPLEMENT asd
            // Get effect instance (pooled)
            new EffectInstance(Source, Source, TargetPoint, AbilityEffect).Execute();
        }
    }
}