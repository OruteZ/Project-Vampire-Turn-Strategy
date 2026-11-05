using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // Development entry point until the menu exists (P9 replaces and deletes it).
    public sealed class GameBootstrap : MonoBehaviour
    {
        public BoardInput input;
        public string stageId = "demo";

        void Start()
        {
            foreach (var s in GameData.Stages)
                if (s.Id == stageId) { input.Begin(new BattleSession(new Battle(s))); return; }
        }
    }
}
