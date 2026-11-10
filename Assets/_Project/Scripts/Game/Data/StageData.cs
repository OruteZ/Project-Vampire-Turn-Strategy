using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    [Serializable] public struct EnemySpot { [Tooltip("하수인 key 또는 vampire")] public string kind; public int col, row; [Tooltip("증원 턴. 0 = 처음부터 (docs/systems/waves.md)")] public int arrive;
        [Tooltip("이 트리거 id 가 이루어지면 나타난다 (docs/systems/triggers.md)")] public string wave;
        public Stance stance; [Tooltip("경계일 때 깨는 거리")] public int alert; }
    [Serializable] public struct TriggerSpot { public string id; public TriggerKind kind; [Tooltip("턴 수 또는 처치 수")] public int value; [Tooltip("칸 도달일 때 칸 (열, 행)")] public List<Vector2Int> hexes; }
    [Serializable] public struct DoorSpot { [Tooltip("지도에서 # 인 칸 (열, 행)")] public List<Vector2Int> hexes; public string trigger; }
    [Serializable] public struct UnitSpot { [Tooltip("병종 key")] public string kind; public int col, row; }

    public sealed class StageData : ScriptableObject
    {
        [Tooltip("캠페인 저장이 이 id 로 클리어를 기억한다. 첫 장은 demo")] public string id;
        [Tooltip("출전 병종 key. 배치 화면 목록 순서")] public List<string> units = new List<string>();
        public int pop, silver, reward;
        [Tooltip("이기는 조건. docs/systems/goals.md")] public Goal goal;
        [Tooltip("방어전의 지킬 칸, 호위의 목적지 칸 (열, 행)")] public List<Vector2Int> spots = new List<Vector2Int>();
        [Tooltip("방어전: 버틸 턴 수")] public int hold;
        [Header("호위 목표 유닛 (호위일 때만)")]
        public int escortHp, escortDef, escortMov;
        public Vector2Int escortAt;
        [Header("추가 목표")]
        public BonusType bonusType;
        [Tooltip("턴 이내 승리일 때 턴 수")] public int bonusValue;
        public int bonusGold;
        [Header("보스")]
        public int bossHp, bossAtk, bossDef, bossMov, bossHeal;
        [Header("지도 (. 평지, # 바위, T 화톳불, ~ 물, S 햇빛, d 배치 구역, , 거친 땅)")]
        public List<string> map = new List<string>();
        [Tooltip("목록 순서가 하수인 행동 순서")] public List<EnemySpot> enemies = new List<EnemySpot>();
        [Header("트리거·문 (docs/systems/triggers.md)")]
        public List<TriggerSpot> triggers = new List<TriggerSpot>();
        public List<DoorSpot> doors = new List<DoorSpot>();
        [Tooltip("배치 화면의 예시 편성. 배치 구역(d) 안에 둔다")] public List<UnitSpot> sample = new List<UnitSpot>();

        public Stage ToCore() => new Stage(id, units.ToArray(), pop, silver, reward, goal,
            new Bonus(bonusType, bonusValue, bonusGold), new Boss(bossHp, bossAtk, bossDef, bossMov, bossHeal), map.ToArray(),
            enemies.Select(e => new Placement(e.kind, e.col, e.row, e.arrive, e.wave, e.stance, e.alert)).ToArray(),
            sample.Select(e => new Placement(e.kind, e.col, e.row)).ToArray(),
            spots.Select(p => new Hex(p.x, p.y)).ToArray(), hold,
            goal == Goal.Escort ? new EscortStats(escortHp, escortDef, escortMov, new Hex(escortAt.x, escortAt.y)) : null,
            triggers.Select(t => new Trigger(t.id, t.kind, t.value, Hexes(t.hexes))).ToArray(),
            doors.Select(d => new Door(Hexes(d.hexes), d.trigger)).ToArray());

        static Hex[] Hexes(List<Vector2Int> list) => list == null ? Array.Empty<Hex>() : list.Select(p => new Hex(p.x, p.y)).ToArray();
    }
}
