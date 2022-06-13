using JetBrains.Annotations;
using RPG.AI;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Control
{
    public class Consideration
    {
        // Tier range is
        // -1 = Veto
        //  0 = Ignore
        //  1-10 = Weight Tier
        //  100 = ForceOne

        // Consideration Properties / Logic
        // Load consideration data from google sheets
        // All other stuff is hardcoded.
        public string name;
        public List<string> inputs = new List<string>();

        public int tier;

        public float m, k, b, c;
        public string curveType;
        public float normalizeStart, normalizeEnd;

        public float ignoCutoff, vetoCutoff = -1;
        public float forceCutoff = 2;

        public bool UseHighest;

        // External Data => Might put this into a different object and ref it in here
        public ConsContext Context;
        // Target is always the target, if you want to check hp on yourself, then you are the target

        // Evaluate Method. Can be overridden for more specialized behavior.
        // This implementation uses all consideration properties with default values
        public virtual Appraisal Evaluate(ConsContext context)
        {
            Context = context;

            // Can be straight value from world, output of a consideration or a combined (averaged) appraisal
            int inputCount = 0;
            float x = 0;

            foreach (var input in inputs)
            {
                var app = Reasoner.GetAppraisal(input, this);
                if (app.Tier <= -1)
                {
                    return new Appraisal(-1, 0);
                }

                if (app.Tier == 0)
                    continue;
                else
                {
                    if (UseHighest && app.Weight >= x)
                    {
                        x = app.Weight;
                    }
                    else
                    {
                        x += app.Weight;
                        inputCount++;
                    }

                    if (app.Tier > tier)
                        tier = app.Tier;
                }
            }

            if (tier > 0)
            {
                float y;
                if (UseHighest)
                    y = Mathf.Clamp(Reasoner.Map(x, this), 0, 1);
                else
                {
                    x /= inputCount;
                    y = Mathf.Clamp(Reasoner.Map(x, this), 0, 1);
                }

                if (y <= vetoCutoff)
                {
                    return new Appraisal(-1, 0);
                }
                if (y <= ignoCutoff)
                    return new Appraisal(0, 0);
                if (y >= forceCutoff)
                    return new Appraisal(100, 1);

                return new Appraisal
                {
                    Tier = tier,
                    Weight = y
                };
            }
            else
            {
                Debug.Log("Tier was 0");
                return new Appraisal(0, 0);
            }
        }

        public void LogValues()
        {
            Debug.Log(name);
            Debug.Log(inputs);
            Debug.Log(tier);
            Debug.Log($"{m}, {k}, {b}, {c}");
            Debug.Log(curveType);
            Debug.Log(normalizeStart);
            Debug.Log(normalizeEnd);
            Debug.Log(ignoCutoff);
            Debug.Log(vetoCutoff);
            Debug.Log(forceCutoff);
        }
    }
}