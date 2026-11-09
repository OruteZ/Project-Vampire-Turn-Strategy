using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VampireSpawn.Core;
using VampireSpawn.Game;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Editor
{
    public sealed class DebugWindow : EditorWindow
    {
        const string PendingKey = "pvs-debug-test-pending";

        [SerializeField] int gold = 1000, clearedCount, testStage;
        [SerializeField] bool withBonus, deployAnywhere = true, testUpgrades = true;

        public static void Open() => GetWindow<DebugWindow>("디버그");

        static Campaign Current
        {
            get { App.Ensure(DataAssetsBuilder.Root()); return App.Campaign; }
        }

        static void Apply(Campaign next)
        {
            App.UseCampaign(next);
            var scene = SceneManager.GetActiveScene().name;
            if (EditorApplication.isPlaying && scene != App.BattleScene) SceneManager.LoadScene(scene);
        }

        void OnGUI()
        {
            var c = Current;
            var stages = Campaign.Stages;
            EditorGUILayout.HelpBox($"저장: 은화 {c.Gold} · 깬 장 {c.Cleared.Count}/{stages.Length} · 보너스 {c.BonusDone.Count} · 강화 {c.Upgrades.Count}/{GameData.Upgrades.Length}"
                + (EditorApplication.isPlaying ? "\n플레이 중: 바꾸면 지금 화면(전투 밖)을 다시 연다" : ""), MessageType.None);

            EditorGUILayout.LabelField("은화", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                gold = Mathf.Max(0, EditorGUILayout.IntField(gold));
                if (GUILayout.Button("설정")) Apply(DebugCheats.With(c, gold: gold));
                if (GUILayout.Button("+1000")) Apply(DebugCheats.With(c, gold: c.Gold + 1000));
            }

            EditorGUILayout.LabelField("진도", EditorStyles.boldLabel);
            clearedCount = EditorGUILayout.IntSlider(new GUIContent("깬 장 수", "앞에서부터 이만큼 깬 것으로 한다. 다음 장이 열린다"), Mathf.Min(clearedCount, stages.Length), 0, stages.Length);
            withBonus = EditorGUILayout.Toggle("보너스도 받은 것으로", withBonus);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("진도 적용"))
                {
                    var cleared = DebugCheats.FirstStages(clearedCount);
                    Apply(DebugCheats.With(c, cleared: cleared, bonus: withBonus ? cleared : c.BonusDone.Where(cleared.Contains)));
                }
                if (GUILayout.Button("모두 깸")) Apply(DebugCheats.With(c, cleared: DebugCheats.FirstStages(stages.Length), bonus: withBonus ? DebugCheats.FirstStages(stages.Length) : null));
                if (GUILayout.Button("처음부터") && EditorUtility.DisplayDialog("원정 초기화", "은화·진도·강화를 모두 지웁니다.", "지우기", "취소"))
                    Apply(new Campaign());
            }

            EditorGUILayout.LabelField("강화", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("모두 받기", "은화를 쓰지 않고 모든 강화를 산 것으로 한다"))) Apply(DebugCheats.With(c, ups: DebugCheats.AllUpgrades));
                if (GUILayout.Button("비우기")) Apply(DebugCheats.With(c, ups: new string[0]));
            }

            EditorGUILayout.LabelField("테스트 전투", EditorStyles.boldLabel);
            var all = GameData.Stages;
            testStage = EditorGUILayout.Popup("장 (지형·적)", Mathf.Min(testStage, all.Length - 1), all.Select(s => s.Id).ToArray());
            deployAnywhere = EditorGUILayout.Toggle(new GUIContent("빈 평지 어디나 배치", "끄면 그 장의 배치 구역만"), deployAnywhere);
            testUpgrades = EditorGUILayout.Toggle(new GUIContent("모든 강화 적용", "끄면 강화 없이"), testUpgrades);
            EditorGUILayout.HelpBox("모든 병종, 인원·은 " + DebugCheats.NoCap + ". 데모로 돌아 저장하지 않는다. 다시 하기·다시 배치는 같은 설정으로.", MessageType.None);
            if (GUILayout.Button(EditorApplication.isPlaying ? "테스트 전투 시작" : "플레이하고 테스트 전투 시작"))
            {
                if (EditorApplication.isPlaying) StartTest();
                else { SessionState.SetBool(PendingKey, true); EditorApplication.EnterPlaymode(); }
            }
        }

        // Play mode reloads the domain, so the request crosses it in SessionState; App.Campaign is set once the first scene's Start ran.
        void Update()
        {
            if (!EditorApplication.isPlaying || App.Campaign == null || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.EraseBool(PendingKey);
            StartTest();
        }

        void StartTest()
        {
            var all = GameData.Stages;
            var from = all[Mathf.Clamp(testStage, 0, all.Length - 1)];
            App.GoBattle(DebugCheats.TestStage(from, deployAnywhere), demo: true, demoUpgrades: testUpgrades ? DebugCheats.AllUpgrades : null);
        }
    }
}
