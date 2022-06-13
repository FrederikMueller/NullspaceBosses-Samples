using UnityEngine;

namespace RPG.Combat
{
    public class Cooldown
    {
        public string ID { get; }
        public bool IsReady => IsReadyCheck();
        public float InitialDuration { get; set; }
        public float PercentComplete => (Time.time - TimeCast) / (TimeReady - TimeCast);
        public float TimeCast { get; set; }
        public float TimeReady { get; set; }

        public Cooldown(string id) => ID = id;

        public void IncurCD(float duration)
        {
            InitialDuration = duration;
            TimeCast = Time.time;
            TimeReady = TimeCast + InitialDuration;
        }

        public void ReduceCD(float amount) => TimeReady -= amount;

        public bool IsReadyCheck()
        {
            if (Time.time > TimeReady)
                return true;
            else
                return false;
        }
    }
}