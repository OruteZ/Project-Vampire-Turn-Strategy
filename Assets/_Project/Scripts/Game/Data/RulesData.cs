using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    public sealed class RulesData : ScriptableObject
    {
        [Tooltip("보름달 흡혈로 뱀파이어가 회복하는 체력의 기본값")] public int drainHeal = 8;
        [Tooltip("이 값까지 달이 차면 보름달")] public int moonMax = 6;
        [Tooltip("보름달 표적·흡혈이 닿는 칸 수")] public int eatRange = 2;
        [Tooltip("이 값까지 달이 차면 관이 열린다")] public int moonWake = 3;
        [Tooltip("치료가 닿는 칸 수")] public int mendRange = 2;

        public RuleSet ToCore() => new RuleSet(drainHeal, moonMax, eatRange, moonWake, mendRange);
    }
}
