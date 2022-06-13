using RPG.AI;
using RPG.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Control
{
    public class AIModule
    {
        public string Name { get; set; }
        public List<AIBehavior> AIBehaviors { get; set; }

        public void DumpEverything()
        {
            foreach (var behavior in AIBehaviors)
            {
                Debug.Log($"{behavior.Name} | {behavior.AIAction.Name} | {behavior.StartConsideration} | {behavior.TierMulti}");
            }
        }
        public void InitActions(CharacterStatusAPI statusAPI)
        {
            statusAPI.Cooldowns.ClearCooldowns();

            foreach (var behavior in AIBehaviors)
            {
                behavior.AIAction.Action.Source = statusAPI;
                // Cooldown tracking via string based on the ability, not the behavior!
                statusAPI.Cooldowns.AddCooldown(behavior.AIAction.Action.Name);
                behavior.AIAction.Action.Init();
                behavior.AIAction.IsAvailable = true;
            }
        }
    }
}