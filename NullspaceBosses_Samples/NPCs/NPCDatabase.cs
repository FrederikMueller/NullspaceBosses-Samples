using System.Collections.Generic;

namespace RPG.Database
{
    public static class NPCDatabase
    {
        public static GTable BossData;

        public static void UpdateBossdata(GTable data)
        {
            BossData = data;
            LoadingManager.OnUpdateComplete(Table.Bosses, $"Added/Updated {data.DataDict.Count} Bosses.");
        }

        public static Dictionary<string, string> GetBossData(string id)
        {
            if (BossData.DataDict.ContainsKey(id))
                return BossData.DataDict[id];
            else
                throw new KeyNotFoundException("Boss data was not found");
        }
    }
}