using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // Development entry point until the menu exists (P9 replaces and deletes it).
    public sealed class GameBootstrap : MonoBehaviour
    {
        public BattleView battle;
        public string stageId = "demo";

        void Start()
        {
            foreach (var s in GameData.Stages)
                if (s.Id == stageId) { battle.Begin(new Battle(s)); return; }
        }
    }
}
