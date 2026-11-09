using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    public static class CampaignStore
    {
        public static Campaign Load()
        {
            var c = Campaign.Parse(PlayerPrefs.GetString(Campaign.SaveKey, null));
            if (c.Repaired) Save(c);
            return c;
        }

        public static void Save(Campaign campaign)
        {
            PlayerPrefs.SetString(Campaign.SaveKey, campaign.ToJson());
            PlayerPrefs.Save();
        }
    }
}
