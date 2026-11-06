using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    [Serializable] public struct EnemySpot { public EnemyKind kind; public int col, row; }
    [Serializable] public struct UnitSpot { public UnitKind kind; public int col, row; }


    public sealed class StageData : ScriptableObject
    {
        [Tooltip("캠페인 저장이 이 id 로 클리어를 기억한다. 첫 장은 demo")] public string id;
        public List<UnitKind> units = new List<UnitKind>();
        public int pop, silver, reward;
        [Header("추가 목표")]
        public BonusType bonusType;
        [Tooltip("턴 이내 승리일 때 턴 수")] public int bonusValue;
        public int bonusGold;
        [Header("보스")]
        public int bossHp, bossAtk, bossDef, bossMov, bossHeal;
        [Header("지도 (. 평지, # 바위, T 화톳불, ~ 물, S 햇빛, d 배치 구역)")]
        public List<string> map = new List<string>();
        public List<EnemySpot> enemies = new List<EnemySpot>();
        [Tooltip("배치 화면의 예시 편성. 배치 구역(d) 안에 둔다")] public List<UnitSpot> sample = new List<UnitSpot>();

        public Stage ToCore() => new Stage(id, units.ToArray(), pop, silver, reward,
            new Bonus(bonusType, bonusValue, bonusGold), new Boss(bossHp, bossAtk, bossDef, bossMov, bossHeal), map.ToArray(),
            enemies.Select(e => new Placement<EnemyKind>(e.kind, e.col, e.row)).ToArray(),
            sample.Select(e => new Placement<UnitKind>(e.kind, e.col, e.row)).ToArray());

        public void Fill(Stage s)
        {
            id = s.Id; units = s.Units.ToList(); pop = s.Pop; silver = s.Silver; reward = s.Reward;
            bonusType = s.Bonus.Type; bonusValue = s.Bonus.Value; bonusGold = s.Bonus.Gold;
            bossHp = s.Boss.Hp; bossAtk = s.Boss.Atk; bossDef = s.Boss.Def; bossMov = s.Boss.Mov; bossHeal = s.Boss.Heal;
            map = s.Map.ToList();
            enemies = s.Enemies.Select(e => new EnemySpot { kind = e.Kind, col = e.Col, row = e.Row }).ToList();
            sample = s.Sample.Select(e => new UnitSpot { kind = e.Kind, col = e.Col, row = e.Row }).ToList();
        }
    }
}
