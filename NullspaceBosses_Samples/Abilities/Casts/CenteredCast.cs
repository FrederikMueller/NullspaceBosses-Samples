namespace RPG.Combat
{
    public class CenteredCast : Cast
    {
        public virtual CenterSource CenterSource { get; set; }
        public virtual void SetCenterPoint()
        {
            if (CenterSource == CenterSource.Caster)
                SourcePoint = Source.transform.position;
            if (CenterSource == CenterSource.RandomEnemy)
                SourcePoint = Source.AES.PickRandomEnemyInCircle(SourcePoint, Range, Source).transform.position;
            if (CenterSource == CenterSource.AverageEnemyPos)
                SourcePoint = Source.AES.FindAveragePosOfEnemies(SourcePoint, Range, Source);
            if (CenterSource == CenterSource.RandomPointInRange)
                SourcePoint = Source.AES.PickRandomPointInRange(SourcePoint, Range);
            if (CenterSource == CenterSource.TargetPoint)
                SourcePoint = TargetPoint;
        }
    }

    // Spawn Points / Zones
    public class SpawnPoints : CenteredCast
    {
        public float SpawnRadius { get; set; }
        public int Count { get; set; }

        public override void Execute()
        {
            Source.Fighter.BroadcastSound(ExecSound);

            SetCenterPoint();
            for (int i = 0; i < Count; i++)
            {
                var e = new EffectInstance(Source, null, Source.AES.PickRandomPointInRange(SourcePoint, SpawnRadius), AbilityEffect);
                e.Execute();
            }
        }
    }
}