using RPG.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Control
{
    public class LocalBlackboard
    {
        private AIMind Mind { get; }
        public GameObject Self { get; set; }
        public CharacterStatusAPI SelfAPI { get; set; }

        public LocalBlackboard(AIMind mind, GameObject self, CharacterStatusAPI api)
        {
            Mind = mind;
            Self = self;
            SelfAPI = api;
        }

        public GameObject Target { get; set; }
        public CharacterStatusAPI TargetAPI { get; set; }

        public GameObject FocusTarget { get; set; }
        public CharacterStatusAPI FocusAPI { get; set; }

        public List<GameObject> Players => Mind.Players;

        public float AggressionFactor { get; set; }
        public float RandomnessFactor { get; set; }
        public float AwarenessFactor { get; set; }
        public float IntelligenceFactor { get; set; }
        public float TeamplayFactor { get; set; }

        public float TargetHPPercentage => TargetAPI.Resources.HealthPercentage;
        public float OwnHPPercentage => SelfAPI.Resources.HealthPercentage;
        public int TargetPostureValue => TargetAPI.Resources.CurrentPoise;
        public int TargetsInMeleeRange => AIHelperMethods.MeleeRangeTargetCount(Self, Players);

        public void UpdateBlackboard()
        {
        }
    }
}