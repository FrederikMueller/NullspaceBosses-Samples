using Mirror;
using RPG.Control;
using RPG.UI;
using System;
using System.Collections;
using UnityEngine;

namespace RPG.Combat
{
    public class ResourceTracker : NetworkBehaviour
    {
        public delegate void CurrentValueDelegate(int amount, int maxvalue);
        public delegate void TriggerDelegate();
        public delegate void SoundDelegate(string fileName);

        public event CurrentValueDelegate EventUpdatePoise;
        public event CurrentValueDelegate EventUpdateHP;
        public event CurrentValueDelegate EventUpdateResource;

        public event TriggerDelegate EventPostureBroken;
        public event TriggerDelegate EventDeath;
        public event SoundDelegate EventSoundTrigger;
        public event TriggerDelegate EventGetHitTrigger;

        public event Action<CharacterStatusAPI> OnPoiseRecovery;

        // A few stats and values should be synchronized across all clients for all player objects. HP, Poise, their Resource. Max values for those. And that's mostly it.
        [SyncVar(hook = nameof(BroadcastHP))] public int CurrentHP;
        [SyncVar] public int MaxHP;
        [SyncVar(hook = nameof(BroadcastPoise))] public int CurrentPoise;
        [SyncVar] public int MaxPoise;
        [SyncVar(hook = nameof(BroadcastResource))] public int CurrentResourceValue;
        [SyncVar] public int MaxResource;
        public Resource ResourceType;

        public float HealthPercentage => CurrentHP / (float)MaxHP;
        public float PoisePercentage => CurrentPoise / (float)MaxPoise;
        public float ResourcePercentage => CurrentResourceValue / (float)MaxResource;

        // SERVER EVENTS
        public event TriggerDelegate ServerDeathTrigger;

        // Fields
        private CharacterStatusAPI statusAPI;
        private StatsTracker stats;
        private WaitForSeconds tickWait;

        public void BroadcastResources()
        {
            EventUpdateHP?.Invoke(CurrentHP, MaxHP);
            EventUpdatePoise?.Invoke(CurrentPoise, MaxPoise);
        }

        private void BroadcastHP(int oldValue, int newValue)
        {
            EventUpdateHP?.Invoke(newValue, MaxHP);
        }
        private void BroadcastPoise(int oldValue, int newValue)
        {
            EventUpdatePoise?.Invoke(newValue, MaxPoise);
        }
        private void BroadcastResource(int oldValue, int newValue)
        {
            if (CurrentResourceValue < 0)
                CurrentResourceValue = 0;
            EventUpdateResource?.Invoke(newValue, MaxResource);
        }
        private void Awake()
        {
            statusAPI = GetComponent<CharacterStatusAPI>();
            stats = GetComponent<StatsTracker>();
        }

        //*** SERVER METHODS
        [Server]
        public void InitResources()
        {
            statusAPI.BuffTracker.UpdateAllCompositeStats();
            tickWait = new WaitForSeconds(.5f);

            CurrentHP = stats.CurrentMaxHP;
            MaxHP = stats.CurrentMaxHP;
            CurrentPoise = stats.CurrentMaxPoise;
            MaxPoise = stats.CurrentMaxPoise;
            CurrentResourceValue = 0;
            MaxResource = stats.CurrentMaxResource;


            InitTickSystems();
        }
        [Server]
        public void InitTickSystems()
        {
            StartCoroutine(ResourceTickRoutine());
        }
        [Server]
        public void TakePoiseDamage(int amount, EffectInstance e)
        {
            if (CurrentPoise <= 0)
            {
                TakeHPDamage(amount, e);
                return;
            }

            amount = Mathf.Clamp(amount, 0, 999999);
            if (amount >= CurrentPoise)
            {
                amount -= CurrentPoise;
                CurrentPoise = 0;
                PostureCheck();

                if (amount > 0)
                    TakeHPDamage(amount, e);
            }
            else
            {
                CurrentPoise = Mathf.Clamp((CurrentPoise - amount), 0, 999999);
                if (e.Source == null)
                    Debug.Log($"source was null for {e.Effect.Name}");
                else
                    e.Source.GetComponent<DamageNumbers>().CreateNumber(amount, gameObject.transform.position, 0);
            }
        }
        [Server]
        public void TakePoiseHealing(int amount, EffectInstance e)
        {
            if (CurrentPoise <= 0)
                return;

            amount = Mathf.Clamp(amount, 0, 999999);
            CurrentPoise = Mathf.Clamp((CurrentPoise + amount), 0, stats.CurrentMaxPoise);

            if (e.Source == null)
                Debug.Log($"source was null for {e.Effect.Name}");
            else
                e.Source.GetComponent<DamageNumbers>().CreateNumber(amount, gameObject.transform.position, 2);
        }
        [Server]
        public void TakeHPDamage(int amount, EffectInstance e)
        {
            if (CurrentHP <= 0)
                return;

            amount = Mathf.Clamp(amount, 0, 999999);
            CurrentHP = Mathf.Clamp((CurrentHP - amount), 0, 999999);
            if (e.Source == null)
                Debug.Log($"source was null for {e.Effect.Name}");
            else
                e.Source.GetComponent<DamageNumbers>().CreateNumber(amount, gameObject.transform.position, 1);
            HPCheck();

            EventGetHitTrigger?.Invoke();
            RpcGetHit();
        }
        [Server]
        public void TakeHPHealing(int amount, EffectInstance e)
        {
            if (CurrentHP <= 0)
                return;
            amount = Mathf.Clamp(amount, 0, 999999);
            CurrentHP = Mathf.Clamp((CurrentHP + amount), 0, stats.CurrentMaxHP);

            if (e.Source == null)
                Debug.Log($"source was null for {e.Effect.Name}");
            else
                e.Source.GetComponent<DamageNumbers>().CreateNumber(amount, gameObject.transform.position, 3);

            HPCheck();
        }

        [Server]
        private void HPCheck()
        {
            if (CurrentHP <= 0)
            {
                statusAPI.States.AddState(State.Dead);

                DeathEvent();
            }
        }
        [Server]
        private void DeathEvent()
        {
            CurrentResourceValue = 0;

            ServerDeathTrigger?.Invoke();

            EventDeath?.Invoke();
            RpcDeath();
        }
        [Server]
        private void PostureCheck()
        {
            if (CurrentPoise <= 0)
                PostureBreak();
        }

        [ClientRpc] private void RpcPostureBroken() => EventPostureBroken?.Invoke();
        [ClientRpc] private void RpcDeath() => EventDeath?.Invoke();
        [ClientRpc] private void RpcGetHit() => EventGetHitTrigger?.Invoke();
        [Server]
        private void PostureBreak()
        {
            statusAPI.Fighter.PostureBreak(3);
            statusAPI.Fighter.BroadcastSound("PBreak2");
            RpcPostureBroken();
        }
        [Server]
        public void ResetPosture()
        {
            if (!statusAPI.States.IsDead)
            {
                CurrentPoise = Mathf.RoundToInt(MaxPoise * 0.5f);
                statusAPI.gameObject.GetComponent<Animator>().SetBool("Stunned", false);
                statusAPI.gameObject.GetComponent<Animator>().SetInteger("Action", 1);

                OnPoiseRecovery?.Invoke(statusAPI);
            }
        }
        [Server]
        public void SetToMaxHP() => CurrentHP = stats.CurrentMaxHP;
        [Server]
        public void SetToMaxPosture() => CurrentPoise = stats.CurrentMaxPoise;

        [Server]
        public IEnumerator ResourceTickRoutine()
        {
            while (true)
            {
                yield return tickWait;
                ResourceTicks();
            }
        }
        [Server]
        private void ResourceTicks()
        {
            if (statusAPI.States.PoiseBroken || statusAPI.States.IsDead)
                return;
            CurrentPoise = Mathf.Clamp(CurrentPoise + 1, 0, stats.CurrentMaxPoise);
            CurrentResourceValue = Mathf.Clamp(CurrentResourceValue + 1, 0, stats.CurrentMaxResource);
        }

    }
}