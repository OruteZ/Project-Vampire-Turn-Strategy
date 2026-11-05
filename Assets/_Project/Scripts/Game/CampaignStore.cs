using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    public static class CampaignStore
    {
        public static Campaign Load() => Campaign.Parse(PlayerPrefs.GetString(Campaign.SaveKey, null));

        public static void Save(Campaign campaign)
        {
            PlayerPrefs.SetString(Campaign.SaveKey, campaign.ToJson());
            PlayerPrefs.Save();
        }
    }
}
