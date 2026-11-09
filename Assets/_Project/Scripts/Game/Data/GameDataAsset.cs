using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    // The root the game loads: one asset per rule set, unit type, minion and stage (docs/guides/data-editor.md).
    public sealed class GameDataAsset : ScriptableObject
    {
        public RulesData rules;
        [Tooltip("병영 탭·편성 목록 순서")] public List<UnitTypeData> units = new List<UnitTypeData>();
        [Tooltip("하수인 종류. 뱀파이어 수치는 장마다")] public List<EnemyData> minions = new List<EnemyData>();
        [Tooltip("0번은 데모, 그 뒤가 캠페인 순서")] public List<StageData> stages = new List<StageData>();

        public List<string> Missing()
        {
            var m = new List<string>();
            if (rules == null) m.Add("규칙 에셋이 비었다");
            for (var i = 0; i < units.Count; i++) if (units[i] == null) m.Add($"병종 {i}번 에셋이 비었다");
            for (var i = 0; i < minions.Count; i++) if (minions[i] == null) m.Add($"하수인 {i}번 에셋이 비었다");
            for (var i = 0; i < stages.Count; i++) if (stages[i] == null) m.Add($"장 {i}번 에셋이 비었다");
            return m;
        }

        public DataSet ToDataSet() => new DataSet(rules.ToCore(), units.Select(u => u.ToCore()).ToArray(),
            units.SelectMany(u => u.UpgradesToCore()).ToArray(), minions.Select(e => e.ToCore()).ToArray(), stages.Select(s => s.ToCore()).ToArray());

        // Problems that would stop the game from using these assets; empty means ToDataSet is safe to Use.
        public List<string> Problems()
        {
            var m = Missing();
            return m.Count > 0 ? m : ToDataSet().Validate();
        }
    }
}
