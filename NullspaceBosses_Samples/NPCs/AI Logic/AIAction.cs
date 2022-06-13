using RPG.Combat;
using RPG.Database;
using UnityEngine;

namespace RPG.Control
{
    public class AIAction
    {
        public string Name { get; set; }
        public Ability Action { get; set; }
        // Cost, CD, State Check
        public bool IsAvailable { get; set; }
        public float LastCastTimestamp { get; set; }

        public void UpdateActions(string type, CharacterStatusAPI statusAPI)
        {
            if (type == Action.ActionType)
            {
                Action = AbilityDB.GetAbility(Name);
                Action.Source = statusAPI;
                Action.Init();
                IsAvailable = true;
            }
        }
    }

    public class AIMovementTest : AIAction
    {
        public void Execute()
        {
            // Custom Logic
        }
    }
}