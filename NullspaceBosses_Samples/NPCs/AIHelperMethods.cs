using System.Collections.Generic;
using UnityEngine;

namespace RPG.Control
{
    public static class AIHelperMethods
    {
        public static float DistanceCheck(GameObject a, GameObject b)
        {
            return Vector3.Distance(a.transform.position, b.gameObject.transform.position);
        }

        public static int DistanceBetweenMeAndXLessThan(GameObject a, GameObject b, float distance, int scoreChange)
        {
            if (DistanceCheck(a, b) < distance)
                return scoreChange;
            else return 0;
        }

        public static int MeleeRangeTargetCount(GameObject self, List<GameObject> players)
        {
            return players.FindAll(x => DistanceCheck(self, x.gameObject) <= 4).Count;
        }
    }
}