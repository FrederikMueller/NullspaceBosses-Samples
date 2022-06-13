namespace RPG.Combat
{
    // Projectile Casts
    public class ProjectileCast : Cast
    {
        public Targeting Targeting { get; set; }

        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            var e = new EffectInstance(Source, Target, TargetPoint, AbilityEffect);

            e.Execute();
        }
    }

    public class VolleyCast : ProjectileCast
    {
        public override void Execute()
        {
            foreach (var target in Source.AES.PickAllEnemiesInRange(SourcePoint, Range, Source))
            {
                TargetPoint = target.transform.position;
                base.Execute();
            }
        }
    }
}