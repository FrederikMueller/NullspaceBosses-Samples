using Mirror;
using RPG.Control;
using System.Collections.Generic;

namespace RPG.Combat
{
    public class CooldownTracker : NetworkBehaviour
    {
        public delegate void CDReadyDelegate(string id);

        public delegate void CDIncurredDelegate(string id, float castTime, float rdyTime);

        public event CDIncurredDelegate EventCDIncurred;

        public event CDReadyDelegate ServerCDReady;

        // Cooldown List
        private Dictionary<string, Cooldown> cooldownDict = new Dictionary<string, Cooldown>();
        private List<string> CDNames = new List<string>();

        // Cooldown methods
        public bool IsCDRdy(string id) => cooldownDict[id].IsReady;

        public float GetCDProgress(string id) => cooldownDict[id].PercentComplete;

        public void IncurCooldown(string id, float duration)
        {
            cooldownDict[id].IncurCD(duration);
            RpcIncurCD(id, cooldownDict[id].TimeCast, cooldownDict[id].TimeReady);
        }

        [ClientRpc] private void RpcIncurCD(string id, float castTime, float rdyTime) => EventCDIncurred?.Invoke(id, castTime, rdyTime);

        public void ReduceCooldown(string id, float amount) => cooldownDict[id].ReduceCD(amount);

        [Server]
        public void AddCooldown(string id)
        {
            cooldownDict.Add(id, new Cooldown(id));
            CDNames.Add(id);
        }

        public void SendCDsToClient() => RpcSendCDListoClient(CDNames);

        [ClientRpc]
        public void RpcSendCDListoClient(List<string> cdnames) => GetComponent<PlayerController>().UpdateUICDs(cdnames);
        [Command]
        public void CmdRequestCDList() => RpcSendCDListoClient(CDNames);

        [Server]
        public void ClearCooldowns()
        {
            cooldownDict.Clear();
            CDNames.Clear();
        }

        [Server]
        public void UpdateCooldown(string id)
        {
        }
    }
}