using RPG.Control;

namespace RPG.AI
{
    public class AIBehavior
    {
        public string Name { get; set; }
        public AIAction AIAction { get; set; }
        public AIMind AIMind { get; set; }

        public string StartConsideration { get; set; }
        // Tier multiplier to elevate or negate tier level. For an additional layer of control to separate stuff like scripted or emergency actions more easily.
        public int TierMulti { get; set; }
        public float BehaviorScore { get; set; }
        public int TierScore { get; set; }

        public void CalculateScore(ConsContext context)
        {
            TierScore = 0;
            BehaviorScore = 0;

            if (AIAction.IsAvailable && (AIMind.Target.transform.position - AIMind.AIController.transform.position).magnitude <= AIAction.Action.Range)
            {
                context.AIBehavior = this;
                var a = Reasoner.StartConsideration(StartConsideration, context);

                TierScore = TierMulti * a.Tier;
                BehaviorScore = a.Weight;
            }
        }
    }
}