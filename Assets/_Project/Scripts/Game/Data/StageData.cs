using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    [Serializable] public struct EnemySpot { [Tooltip("하수인 key 또는 vampire")] public string kind; public int col, row; }
    [Serializable] public struct UnitSpot { [Tooltip("병종 key")] public string kind; public int col, row; }

    public sealed class StageData : ScriptableObject
    {
        [Tooltip("캠페인 저장이 이 id 로 클리어를 기억한다. 첫 장은 demo")] public string id;
        [Tooltip("출전 병종 key. 배치 화면 목록 순서")] public List<string> units = new List<string>();
        public int pop, silver, reward;
        [Tooltip("이기는 조건. docs/dev/battle-goals.md")] public Goal goal;
        [Header("추가 목표")]
        public BonusType bonusType;
        [Tooltip("턴 이내 승리일 때 턴 수")] public int bonusValue;
        public int bonusGold;
        [Header("보스")]
        public int bossHp, bossAtk, bossDef, bossMov, bossHeal;
        [Header("지도 (. 평지, # 바위, T 화톳불, ~ 물, S 햇빛, d 배치 구역)")]
        public List<string> map = new List<string>();
        [Tooltip("목록 순서가 하수인 행동 순서")] public List<EnemySpot> enemies = new List<EnemySpot>();
        [Tooltip("배치 화면의 예시 편성. 배치 구역(d) 안에 둔다")] public List<UnitSpot> sample = new List<UnitSpot>();

        public Stage ToCore() => new Stage(id, units.ToArray(), pop, silver, reward, goal,
            new Bonus(bonusType, bonusValue, bonusGold), new Boss(bossHp, bossAtk, bossDef, bossMov, bossHeal), map.ToArray(),
            enemies.Select(e => new Placement(e.kind, e.col, e.row)).ToArray(),
            sample.Select(e => new Placement(e.kind, e.col, e.row)).ToArray());
    }
}
