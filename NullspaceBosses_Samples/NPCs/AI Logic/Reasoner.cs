using RPG.Combat;
using RPG.Control;
using RPG.Database;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.AI
{
    public static class Reasoner
    {
        private static Dictionary<string, Consideration> cachedCons = new Dictionary<string, Consideration>();
        public static void UpdateConsiderations(List<Consideration> considerations)
        {
            cachedCons.Clear();
            foreach (var con in considerations)
            {
                cachedCons.Add(con.name, con);
            }

            LoadingManager.OnUpdateComplete(Table.Considerations, $"Added/Updated {cachedCons.Count} Considerations");
        }

        // Public method that can take in all inputIds. Internally routed to the right container.
        public static Appraisal GetAppraisal(string inputId, Consideration consideration)
        {
            // All considerations are loaded via GTable and cached here. The name is used as the key, so any other thing wanting to use a consideration as input uses that name
            if (cachedCons.ContainsKey(inputId))
            {
                return cachedCons[inputId].Evaluate(consideration.Context);
            }
            else
                return GetAppraisalInternal(inputId, consideration);
        }

        // Top-level initiation of the consideration chain
        public static Appraisal StartConsideration(string inputId, ConsContext context)
        {
            if (cachedCons.ContainsKey(inputId))
            {
                return cachedCons[inputId].Evaluate(context);
            }
            else
            {
                Debug.Log("Consideration ID was not found in CachedCons. Coulndt start Consideration Process. Returning 0,0 Appraisal.");
                return new Appraisal(0, 0);
            }
        }
        // These should only consist of direct data and special combinations. Considerations themselves are imported and stored in cachedCons. Considerations can also combine any number
        // of other considerations OR any of these inputs here. If you want to do WEIGHTED or other methods of combining inputs you must hardcode it here.

        // "Hardcoded Appraisals". These can also call imported considerations if you know the name. Return some base value if it's not found for some reason.
        private static Appraisal GetAppraisalInternal(string consInput, Consideration cons)
        {
            return consInput switch
            {
                // These might produce errors if no target is found or a property is not set => only ever generate considerations with a target.

                "TargetHealth" => new Appraisal(1, cons.Context.Target.Resources.HealthPercentage),
                "TargetPoise" => new Appraisal(1, cons.Context.Target.Resources.PoisePercentage),
                "TargetResource" => new Appraisal(1, cons.Context.Target.Resources.ResourcePercentage),

                "SelfHealth" => new Appraisal(1, cons.Context.Source.Resources.HealthPercentage),
                "SelfPoise" => new Appraisal(1, cons.Context.Source.Resources.PoisePercentage),
                "SelfResource" => new Appraisal(1, cons.Context.Source.Resources.ResourcePercentage),

                "TargetVeto" => TargetVeto(cons),

                "Distance" => Distance(cons),
                "TimeSinceLastCast" => CalcTimeSinceLastCast(cons),
                "Threat" => Threat(cons),

                // Random from 0 - X
                "Random10" => new Appraisal(1, UnityEngine.Random.Range(0f, 1f)),
                "Random9" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.9f)),
                "Random8" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.8f)),
                "Random7" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.7f)),
                "Random6" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.6f)),
                "Random5" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.5f)),
                "Random4" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.4f)),
                "Random3" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.3f)),
                "Random2" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.2f)),
                "Random1" => new Appraisal(1, UnityEngine.Random.Range(0f, 0.1f)),

                // Via b, relies on b value in curve params
                "bvalue" => new Appraisal(1, 1),
                // Hardcoded Floats
                "0" => new Appraisal(1, 0),
                "0.1" => new Appraisal(1, 0.1f),
                "0.2" => new Appraisal(1, 0.2f),
                "0.3" => new Appraisal(1, 0.3f),
                "0.4" => new Appraisal(1, 0.4f),
                "0.5" => new Appraisal(1, 0.5f),
                "0.6" => new Appraisal(1, 0.6f),
                "0.7" => new Appraisal(1, 0.7f),
                "0.8" => new Appraisal(1, 0.8f),
                "0.9" => new Appraisal(1, 0.9f),
                "1" => new Appraisal(1, 1f),
                _ => IdNotFound()
            };
        }

        // Functions to transform floats
        private static Appraisal Normalize(float value, float min, float max)
        {
            if (max - min == 0)
            {
                Debug.Log("Normalization returned IGNORE bcs of divide by 0");
                return new Appraisal(0, 0);
            }

            float n = (value - min) / (max - min);

            n = Mathf.Clamp(n, 0, 1);

            return new Appraisal(1, n);
        }
        public static float Map(float input, Consideration cons)
        {
            return cons.curveType switch
            {
                "curve" => (cons.m * Mathf.Pow(input - cons.c, cons.k)) + cons.b,
                "const" => cons.b,
                "id" => input,
                "inv" => 1 - input,
                _ => input
            };
        }

        // Hardcoded "Considerations"
        private static Appraisal IdNotFound()
        {
            Debug.Log("Consideration ID was neither found in CachedCons nor in Hardcoded Table. Returning 0,0 Appraisal.");
            return new Appraisal(0, 0);
        }

        private static Appraisal Distance(Consideration cons)
        {
            float d = Vector3.Distance(cons.Context.Source.transform.position, cons.Context.Target.transform.position);

            return Normalize(d, cons.normalizeStart, cons.normalizeEnd);
        }

        private static Appraisal Threat(Consideration cons)
        {
            // currently just combines raw inputs, not actual considerations

            var apps = new List<Appraisal>();
            apps.Add(GetAppraisalInternal("Health", cons));
            apps.Add(GetAppraisalInternal("Poise", cons));

            return AverageAppraisals(apps);
        }

        private static Appraisal TargetVeto(Consideration cons)
        {
            if (cons.Context.Target == cons.Context.AIBehavior.AIMind.Target.GetComponent<CharacterStatusAPI>())
                return new Appraisal(-1, 0);
            else
                return new Appraisal(0, 0);
        }
        private static Appraisal CalcTimeSinceLastCast(Consideration cons)
        {
            if (cons.Context.AIBehavior.AIAction.LastCastTimestamp == 0)
            {
                Debug.Log($"CDs passed for {cons.Context.AIBehavior.AIAction.Action.Name} is (not casted yet).");

                return new Appraisal(1, 1);
            }

            var cd = cons.Context.AIBehavior.AIAction.Action.Cooldown;
            var timeSinceLastCast = Time.time - cons.Context.AIBehavior.AIAction.LastCastTimestamp;

            return Normalize(timeSinceLastCast / cd, 1, 3);
        }
        //
        // Combining Methods (Averaging, Weighted Averaging..)
        private static Appraisal AverageAppraisals(List<Appraisal> appraisals)
        {
            int count = 0;
            float weightSum = 0;
            int highestTier = 0;

            foreach (var appraisal in appraisals)
            {
                if (appraisal.Tier <= -1)
                    return new Appraisal(-1, 0);

                if (appraisal.Tier > 0)
                {
                    weightSum += appraisal.Weight;
                    count++;
                    if (appraisal.Tier > highestTier)
                        highestTier = appraisal.Tier;
                }
            }
            return new Appraisal(highestTier, weightSum / count);
        }
    }
}