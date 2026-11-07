using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    public sealed class EnemyData : ScriptableObject
    {
        public int hp, atk, def, mov, rng = 1;

        public EnemyStats ToCore() => new EnemyStats(hp, atk, def, mov, rng);

        public void Fill(EnemyStats e) { hp = e.Hp; atk = e.Atk; def = e.Def; mov = e.Mov; rng = e.Rng; }
    }
}
