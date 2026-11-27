using UnityEditor;
using UnityEngine;

namespace VampireSpawn.Editor
{
    // The project's only menu entry (CLAUDE.md "Unity 메뉴·에디터 도구"): each area adds one row of buttons here.
    public sealed class ToolsWindow : EditorWindow
    {
        [MenuItem("VampireSpawn/도구 모음 (Tools)")]
        public static void Open() => GetWindow<ToolsWindow>("도구 모음");

        void OnGUI()
        {
            EditorGUILayout.LabelField("데이터", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("게임 데이터 편집기")) GameDataWindow.Open();
                if (GUILayout.Button("데이터 에셋 선택")) Selection.activeObject = DataAssetsBuilder.Root();
            }

            EditorGUILayout.LabelField("디버그", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("디버그 창", "은화·진도·강화 바꾸기, 자유 배치 테스트 전투 (#96)"))) DebugWindow.Open();

            EditorGUILayout.LabelField("유료 에셋", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Synty 다시 불러오기", "빠진 Synty 패키지를 UPackage/ 에서 임포트하고 Sidekick 레시피를 굽는다 (/synty-reload)"))) SyntyReload.Run(false);
                if (GUILayout.Button(new GUIContent("캐릭터 모두 다시 굽기", "레시피가 그대로여도 전부 다시 굽는다"))) SyntyReload.Run(true);
            }
            var last = SyntyReload.LastResult;
            if (last.Length > 0)
                EditorGUILayout.HelpBox(last + "\n확인: Window > General > Test Runner 의 SidekickRecipeTests, RequiredAssetsTests",
                    last.Contains("FAIL") || last.Contains("MISSING") ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.LabelField("지도", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("지도 층 굽기", "Camp 씬의 World Map 에 없는 것(지역·표식·화로·시점 점, 야영지, 보조광, 시점)만 더하고 예전 지도를 지운다. 손으로 옮긴 것은 그대로 (docs/art/campaign-map.md 8)")))
                Debug.Log(CampaignMapBaker.Bake());

            EditorGUILayout.LabelField("전투 보드", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("꾸미기 프리팹 굽기", "프리팹이 없는 장만 규칙대로 Assets/_Project/Art/Stages/<장 id>.prefab 을 짓는다. 있는 프리팹은 그대로 (docs/art/stage-dressing.md 4)")))
                    Debug.Log(StageDressingBaker.Bake());
                if (GUILayout.Button(new GUIContent("세트피스 뽑기 (묘지)", "Synty Demo_Graveyard_01 의 소품 무리를 Assets/_Project/Art/Setpieces/Cemetery 프리팹으로. 있는 파일은 그대로 (docs/art/stage-dressing.md 5-1)")))
                    Debug.Log(SetpieceExtractor.Extract("Assets/Synty/PolygonDarkFantasy/Scenes/Demo_Graveyard_01.unity", "Cemetery"));
            }

            EditorGUILayout.LabelField("유닛 모습", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("유닛 모습 한눈에", "모든 UnitLook 을 앞 대기·옆 대기·옆 공격으로 찍어 verify-artifacts/unit-lineup.png 로 연다 (docs/art/unit-visuals.md 6)")))
            {
                var path = System.IO.Path.GetFullPath("verify-artifacts/unit-lineup.png");
                UnitLineup.Capture(path);
                EditorUtility.OpenWithDefaultApp(path);
            }
        }
    }
}
