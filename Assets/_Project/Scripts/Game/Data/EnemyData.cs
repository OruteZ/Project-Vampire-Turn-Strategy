using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    // One minion kind (docs/content/enemies.md 2).
    public sealed class EnemyData : ScriptableObject
    {
        [Tooltip("장 에셋이 하수인을 이 이름으로 찾는다. 바꾸지 않는다. vampire 는 쓸 수 없다")] public string key;
        public int hp, atk, def, mov, rng = 1;

        public EnemyType ToCore() => new EnemyType(key, hp, atk, def, mov, rng);
    }
}
