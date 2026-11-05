using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Data
{
    // The root the game loads: one asset per rule set, unit type, enemy and stage (docs/guides/data-editor.md).

    public sealed class GameDataAsset : ScriptableObject
    {
        public RulesData rules;
        [Tooltip("UnitKind 순서: 농민 창병, 농민 궁수, 검사, 궁수, 기사, 사냥꾼")] public UnitTypeData[] units = new UnitTypeData[6];
        public EnemyData skeleton;
        [Tooltip("0번은 데모, 그 뒤가 캠페인 순서")] public List<StageData> stages = new List<StageData>();

        public List<string> Missing()
        {
            var m = new List<string>();
            if (rules == null) m.Add("규칙 에셋이 비었다");
            if (skeleton == null) m.Add("스켈레톤 에셋이 비었다");
            for (var i = 0; i < units.Length; i++) if (units[i] == null) m.Add($"병종 {i}번 에셋이 비었다");
            for (var i = 0; i < stages.Count; i++) if (stages[i] == null) m.Add($"장 {i}번 에셋이 비었다");
            return m;
        }

        public DataSet ToDataSet() => new DataSet(rules.ToCore(), units.Select(u => u.ToCore()).ToArray(),
            units.SelectMany(u => u.UpgradesToCore()).ToArray(), skeleton.ToCore(), stages.Select(s => s.ToCore()).ToArray());

        // Problems that would stop the game from using these assets; empty means ToDataSet is safe to Use.
        public List<string> Problems()
        {
            var m = Missing();
            return m.Count > 0 ? m : ToDataSet().Validate();
        }
    }
}
