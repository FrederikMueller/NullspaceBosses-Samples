using Cysharp.Threading.Tasks;
using System;

namespace RPG.Combat
{
    public class IteratorCast : Cast
    {
        public Ability IteratedAbility { get; set; }
        public int Ticks { get; set; }
        public float Interval { get; set; }

        public override void Init()
        {
            IteratedAbility.Source = Source;
            IteratedAbility.Init();
        }

        public override void Execute()
        {
            // spawn coroutine and inject (if needed) all the needed data
            // Might create a bug, not sure, theoretically it should transfer the values and if the used ability generates those themselves then it will overwrite these
            IteratedAbility.TargetPoint = TargetPoint;
            IteratedAbility.SourcePoint = SourcePoint;
            Source.Fighter.BroadcastSound(ExecSound);

            DemoIter().Forget();
        }

        // Worked immediately, but reseting the game while this runs will cause null ref errors
        private async UniTaskVoid DemoIter()
        {
            for (int i = 0; i < Ticks; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Interval), ignoreTimeScale: false);
                if (Source != null)
                    IteratedAbility.Execute();
            }
        }
    }
}