using Cysharp.Threading.Tasks;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RPG.Combat
{
    public class BuffTracker : NetworkBehaviour
    {
        public delegate void StatChangeBroadcaster(float valueChange);
        public delegate void BuffApplicationDelegate(Buff buff);
        public delegate void BuffRemovalDelegate(Buff buff);
        public delegate void BuffInfoBroadcast(string text);

        // Can be called both on the server or the client, depending on what you need.
        public event BuffInfoBroadcast EventBuffInfoBroadcast;
        public event BuffInfoBroadcast EventDebuffInfoBroadcast;

        public event StatChangeBroadcaster EventMovementSpeedChange;
        public event StatChangeBroadcaster EventAttackSpeedChange;
        public event BuffApplicationDelegate EventAddBuffToClients;
        public event BuffRemovalDelegate EventRemoveBuffOnClients;

        public event Action<BuffInstance> OnBuffApplication;
        public event Action<BuffInstance> OnBuffRemoval;
        public event Action<DebuffInstance> OnDebuffApplication;
        public event Action<DebuffInstance> OnDebuffRemoval;

        public StatsTracker StatsTracker;
        public StateTracker StateTracker;
        public CharacterStatusAPI StatusAPI;

        private StringBuilder stringBuilder = new StringBuilder();
        public List<BuffInstance> BuffContainerBasic = new List<BuffInstance>();
        public List<DebuffInstance> DebuffContainerBasic = new List<DebuffInstance>();

        public List<string> BuffListStrings = new List<string>();

        public Dictionary<StatEnum, Action<StatEnum>> EffectUpdateRouter = new Dictionary<StatEnum, Action<StatEnum>>();

        private void Awake()
        {
            StatsTracker = GetComponent<StatsTracker>();
            StatusAPI = GetComponent<CharacterStatusAPI>();
            StateTracker = GetComponent<StateTracker>();
        }

        //*** SERVER METHODS
        public override void OnStartServer()
        {
            EffectUpdateRouter.Add(StatEnum.FlatHP, UpdateMaxHP);
            EffectUpdateRouter.Add(StatEnum.PercHP, UpdateMaxHP);
            EffectUpdateRouter.Add(StatEnum.FlatPoise, UpdateMaxPoise);
            EffectUpdateRouter.Add(StatEnum.PercPoise, UpdateMaxPoise);
            EffectUpdateRouter.Add(StatEnum.FlatArmor, UpdateArmor);
            EffectUpdateRouter.Add(StatEnum.PercArmor, UpdateArmor);
            EffectUpdateRouter.Add(StatEnum.SpecialAvoidance, UpdateSpecialAvoidance);
            EffectUpdateRouter.Add(StatEnum.SchoolImmunity, UpdateSchoolAvoidance);
        }

        //////////////////////////////////////// CORE FUNCTIONALITY ////////////////////////////////////////////
        /** <summary>
        Call to add a buff to the object's buff container
        </summary>
        <param name="b">Buff to add to the Buffcontainer.</param>
        */

        // BUFFS
        [Server]
        public void AddBuff(BuffInstance b)
        {
            int buffCount = 0;
            BuffInstance oldest = null;
            foreach (var buffInstance in BuffContainerBasic)
            {
                if (buffInstance.Buff.Name == b.Buff.Name)
                {
                    buffCount++;
                    if (oldest == null)
                        oldest = buffInstance;
                    else if (oldest.TimeApplied > buffInstance.TimeApplied)
                        oldest = buffInstance;
                }
            }

            if (b.Buff.MaxStacks < buffCount + 1)
                RemoveBuff(oldest);

            BuffContainerBasic.Add(b);

            b.TimeApplied = Time.time;
            b.TimeToRemove = b.TimeApplied + b.Buff.Duration;
            b.InitialDuration = b.Buff.Duration;
            b.Coroutine = StartCoroutine(BuffTimer(b.Buff.Duration, b));

            // changes to the duration of the buff/debuff simply happens by calculating the new duration based
            // on the timeapplied etc.

            foreach (StatMod effect in b.Buff.StatMods)
            {
                CallUpdateMethod(effect);
            }
            OnBuffApplication?.Invoke(b);
            b.Buff.OnApplication(b);

            foreach (var buffinstance in BuffContainerBasic)
            {
                stringBuilder.Append(buffinstance.Buff.Name).Append(": ").Append(buffinstance.Buff.Name).Append("\n"); // SCUFFED MISREPRESENTATION BCS OF IMMUNITIES
            }
            RpcBuffInfoBroadcast(stringBuilder.ToString());
            //EventBuffInfoBroadcast(stringBuilder.ToString());
            stringBuilder.Clear();
        }

        /// <summary>
        /// Call to remove a buff from the buff container.
        /// </summary>
        /// <param name="b">The buff to remove from the buff container.</param>
        /// <remarks>Is called whenever a buff timer runs out and recalculates the removed buff's underlying effect.</remarks>
        /// "
        /// <param name="b"></param>
        [Server]
        public void RemoveBuff(BuffInstance b)
        {
            BuffContainerBasic.Remove(b);
            if (b.Coroutine != null)
                StopCoroutine(b.Coroutine);

            foreach (StatMod effect in b.Buff.StatMods)
            {
                CallUpdateMethod(effect);
            }
            b.Buff.OnRemove(b);
            OnBuffRemoval?.Invoke(b);

            foreach (var buffinstance in BuffContainerBasic)
            {
                stringBuilder.Append(buffinstance.Buff.Name).Append(": ").Append(buffinstance.Buff.Name).Append("\n");
            }
            RpcBuffInfoBroadcast(stringBuilder.ToString());
            //EventBuffInfoBroadcast(stringBuilder.ToString());
            stringBuilder.Clear();
        }
        [Server]
        public void RemoveBuffviaID(string id)
        {
            foreach (var b in BuffContainerBasic.Where(x => x.Buff.Name == id).ToList())
            {
                RemoveBuff(b);
            }
        }
        // DEBUFFS
        [Server]
        public void AddDebuff(DebuffInstance d)
        {
            int debuffCount = 0;
            DebuffInstance oldest = null;
            foreach (var debuffInstance in DebuffContainerBasic)
            {
                if (debuffInstance.Debuff.Name == d.Debuff.Name)
                {
                    debuffCount++;
                    if (oldest == null)
                        oldest = debuffInstance;
                    else if (oldest.TimeApplied > debuffInstance.TimeApplied)
                        oldest = debuffInstance;
                }
            }

            if (d.Debuff.MaxStacks < debuffCount + 1)
                RemoveDebuff(oldest);

            DebuffContainerBasic.Add(d);

            d.TimeApplied = Time.time;
            d.TimeToRemove = d.TimeApplied + d.Debuff.Duration;
            d.InitialDuration = d.Debuff.Duration;
            d.Coroutine = StartCoroutine(DebuffTimer(d.Debuff.Duration, d));

            foreach (StatMod effect in d.Debuff.StatMods)
            {
                CallUpdateMethod(effect);
            }
            OnDebuffApplication?.Invoke(d);
            d.Debuff.OnApplication(d);

            foreach (var debuffInstance in DebuffContainerBasic)
            {
                stringBuilder.Append(debuffInstance.Debuff.Name).Append(": ").Append(debuffInstance.Debuff.Name).Append("\n");
            }
            RpcDebuffInfoBroadcast(stringBuilder.ToString());
            //EventDebuffInfoBroadcast?.Invoke(stringBuilder.ToString());
            stringBuilder.Clear();
        }
        [Server]
        public void RemoveDebuff(DebuffInstance d)
        {
            DebuffContainerBasic.Remove(d);
            if (d.Coroutine != null)
                StopCoroutine(d.Coroutine);

            foreach (StatMod effect in d.Debuff.StatMods)
            {
                CallUpdateMethod(effect);
            }
            d.Debuff.OnRemove(d);
            OnDebuffRemoval?.Invoke(d);

            foreach (var debuffInstance in DebuffContainerBasic)
            {
                stringBuilder.Append(debuffInstance.Debuff.Name).Append(": ").Append(debuffInstance.Debuff.Name).Append("\n");
            }
            RpcDebuffInfoBroadcast(stringBuilder.ToString());
            //EventDebuffInfoBroadcast?.Invoke(stringBuilder.ToString());
            stringBuilder.Clear();
        }
        [Server]
        public void RemoveDebuffviaID(string id)
        {
            foreach (var d in DebuffContainerBasic.Where(x => x.Debuff.Name == id).ToList())
            {
                RemoveDebuff(d);
            }
        }
        [Server]
        private void CallUpdateMethod(StatMod statmod)
        {
            if (EffectUpdateRouter.ContainsKey(statmod.ID))
                EffectUpdateRouter[statmod.ID].Invoke(statmod.ID);
            else
                UpdateStat(statmod.ID);
        }

        // TIMERS
        [Server]
        public IEnumerator BuffTimer(float duration, BuffInstance b)
        {
            yield return new WaitForSeconds(duration);
            if (BuffContainerBasic.Contains(b))
                RemoveBuff(b);
        }
        [Server]
        public async UniTask BuffTimerAsync(float duration, BuffInstance b)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration)).SuppressCancellationThrow();
            if (BuffContainerBasic.Contains(b))
                RemoveBuff(b);
        }

        [Server]
        public IEnumerator DebuffTimer(float duration, DebuffInstance d)
        {
            yield return new WaitForSeconds(duration);
            if (DebuffContainerBasic.Contains(d))
                RemoveDebuff(d);
        }

        // BUFF AND DEBUFF CHECKING (ITERATRE THROUGH ELEMENTS AND CALC TOTAL VALUE)

        [Server]
        public float BuffChecker(StatEnum effectName)
        {
            float effectValue = 0;
            if (BuffContainerBasic.Count > 0)
            {
                foreach (var effect in BuffContainerBasic.SelectMany(x => x.Buff.StatMods).Where(x => x.ID == effectName))
                    effectValue += effect.Value;
            }
            //Debug.Log($"{effectName} had a value of {effectValue}");
            return effectValue;
        }
        [Server]
        public float DebuffChecker(StatEnum effectName)
        {
            float effectValue = 0;
            if (DebuffContainerBasic.Count > 0)
            {
                foreach (var effect in DebuffContainerBasic.SelectMany(x => x.Debuff.StatMods).Where(x => x.ID == effectName))
                    effectValue += effect.Value;
            }
            //Debug.Log($"{effectName} had a value of {effectValue}");
            return effectValue;
        }
        [Server]
        public float EffectValueChecker(StatEnum effectName)
        {
            float result = 0;
            result += BuffChecker(effectName);
            result += DebuffChecker(effectName);
            return result;
        }

        //
        //////////////////////////////////////// END OF CORE FUNCTIONALITY ////////////////////////////////////////
        // Split the updating of stats out of here. Maybe in statstracker. This class should only manage buffs, debuffs
        // their uptimes and the calculation of effects from those buffs and debuffs.
        //////////////////////////////////////// Update Methods ///////////////////////////////////////////////////

        [Server]
        public void UpdateMaxHP(StatEnum stat)
        {
            float flatValue = StatsTracker.GetBaselineStat(StatEnum.FlatHP) + EffectValueChecker(StatEnum.FlatHP);
            float percMod = 1 + StatsTracker.GetBaselineStat(StatEnum.PercHP) + EffectValueChecker(StatEnum.PercHP);

            StatsTracker.SetCurrentStat(StatEnum.CurrentMaxHP, flatValue * percMod);
            StatusAPI.Resources.MaxHP = StatsTracker.CurrentMaxHP;

            if (StatusAPI.Resources.CurrentHP > StatsTracker.CurrentMaxHP)
                StatusAPI.Resources.SetToMaxHP();
        }
        [Server]
        public void UpdateMaxPoise(StatEnum stat)
        {
            float flatValue = StatsTracker.GetBaselineStat(StatEnum.FlatPoise) + EffectValueChecker(StatEnum.FlatPoise);
            float percMod = 1 + StatsTracker.GetBaselineStat(StatEnum.PercPoise) + EffectValueChecker(StatEnum.PercPoise);

            StatsTracker.SetCurrentStat(StatEnum.CurrentMaxPoise, flatValue * percMod);
            StatusAPI.Resources.MaxPoise = StatsTracker.CurrentMaxPoise;
            if (StatusAPI.Resources.CurrentPoise > StatsTracker.CurrentMaxPoise)
                StatusAPI.Resources.SetToMaxPosture();
        }

        [Server]
        public void UpdateArmor(StatEnum stat)
        {
            float flatValue = StatsTracker.GetBaselineStat(StatEnum.FlatArmor) + EffectValueChecker(StatEnum.FlatArmor);
            float percMod = 1 + StatsTracker.GetBaselineStat(StatEnum.PercArmor) + EffectValueChecker(StatEnum.PercArmor);

            StatsTracker.SetCurrentStat(StatEnum.CurrentArmor, flatValue * percMod);
        }

        [Server]
        public void UpdateStat(StatEnum stat)
        {
            StatsTracker.SetCurrentStat(stat, StatsTracker.GetBaselineStat(stat) + EffectValueChecker(stat));
        }


        [Server]
        public void UpdateSpecialAvoidance(StatEnum stat)
        {
            float f = BuffChecker(stat);

            StatusAPI.States.SpecialAvoidance = (SpecialAvoidance)Mathf.RoundToInt(f);
        }
        [Server]
        public void UpdateSchoolAvoidance(StatEnum stat)
        {
            float f = BuffChecker(stat);

            StatusAPI.States.SchoolImmunity = (SchoolImmunity)Mathf.RoundToInt(f);
        }

        // Client RPCs after SyncEvent Change
        [ClientRpc] private void RpcBuffInfoBroadcast(string info) => EventBuffInfoBroadcast?.Invoke(info);
        [ClientRpc] private void RpcDebuffInfoBroadcast(string info) => EventDebuffInfoBroadcast?.Invoke(info);
        [ClientRpc] private void RpcMovementSpeedChange(float value) => EventMovementSpeedChange?.Invoke(value);
        [ClientRpc] private void RpcAttackspeedChange(float value) => EventAttackSpeedChange?.Invoke(value);
    }
}