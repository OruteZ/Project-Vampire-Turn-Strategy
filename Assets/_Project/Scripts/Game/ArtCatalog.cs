using UnityEngine;

namespace VampireSpawn.Game
{
    [CreateAssetMenu(fileName = "ArtCatalog", menuName = "VampireSpawn/Art Catalog")]
    public sealed class ArtCatalog : ScriptableObject
    {
        [Tooltip("모든 사운드 (docs/audio/sounds.md)")] public SoundCatalog sound;

        [Header("Board")]
        public float hexSize = 1f;
        public float tileHeight = 0.2f;
        public Material plain, water;
        [Tooltip("칸 격자·칸 표시의 정점색 재질 (docs/art/battle-terrain.md 3)")] public Material overlay;
        public GameObject[] rockProps;
        public GameObject brazier, fire;
        [Tooltip("화톳불 강조 불티, 햇빛 칸 먼지 (docs/art/battle-terrain.md 3-1)")] public GameObject embers, dust;
        [Tooltip("햇빛 칸 빛기둥 (docs/art/battle-terrain.md 3-1)")] public Shader lightBeam;
        public Color torchLight = new Color(0.89f, 0.66f, 0.29f);
        public Color sunLight = new Color(0.92f, 0.86f, 0.68f);
        [Tooltip("장별 모습 (docs/art/stage-dressing.md). 장 id 가 어느 테마에도 없으면 맨 앞 테마")] public BoardTheme[] themes;

        public BoardTheme Theme(string stageId)
        {
            if (themes == null || themes.Length == 0) return null;
            foreach (var t in themes) if (t != null && t.stages != null && System.Array.IndexOf(t.stages, stageId) >= 0) return t;
            return themes[0];
        }

        [Tooltip("장별 꾸미기 프리팹, 이름 = 장 id (docs/art/stage-dressing.md 4). 도구 모음 > 전투 보드 > 꾸미기 프리팹 굽기로 채운다")]
        public GameObject[] stageDressings;

        public GameObject Dressing(string stageId) => System.Array.Find(stageDressings ?? System.Array.Empty<GameObject>(), p => p != null && p.name == stageId);

        [Header("Units")]
        [Tooltip("병종 모습. 순서 상관없이 UnitLook.key (병종 key) 로 찾는다")]
        public UnitLook[] allyLooks;
        [Tooltip("하수인 모습. UnitLook.key = 하수인 key")]
        public UnitLook[] minionLooks;
        public UnitLook vampireLook;
        [Tooltip("달의 피로 변신한 웨어울프 (docs/content/units.md 판타지 직업 개편 C)")] public UnitLook wolfLook;
        [Tooltip("Polygon 캐릭터와, 묶음이 빈 UnitLook 이 쓰는 기본 애니메이터")] public RuntimeAnimatorController unitAnimator;
        [Tooltip("Sidekick 캐릭터가 스폰이 되었을 때 (맨손 근접)")] public RuntimeAnimatorController spawnAnimator;
        public float unitHeight = 1.5f;
        [Tooltip("잠든 뱀파이어의 관. 색칠 없이 그대로 (docs/art/unit-visuals.md 5)")] public GameObject coffin;

        public UnitLook Look(string key) => Find(allyLooks, key) ?? Find(minionLooks, key);

        // Weapon icon sprite name for a unit type, from its look (docs/art/unit-visuals.md).
        public string Icon(string key) => Look(key)?.icon;

        static UnitLook Find(UnitLook[] looks, string key) => System.Array.Find(looks ?? System.Array.Empty<UnitLook>(), l => l != null && l.key == key);

        [Header("UI")]
        [Tooltip("Synty UI 스프라이트. 이름으로 찾는다 (docs/art/ui-style.md)")]
        public Sprite[] ui;

        [Tooltip("Synty UI 장식 프리팹 (Crest, Section_Break). uGUI 프리팹이지만 그리지 않고 ToolkitKit.Prefab 이 그림 정보만 읽는다")]
        public GameObject[] uiPrefabs;

        [Header("UI Toolkit (Assets/_Project/UI)")]
        public UnityEngine.UIElements.PanelSettings panel;
        public UnityEngine.UIElements.VisualTreeAsset menuDocument;
        [Tooltip("장 선택 (docs/ui/stage-select.md 10)")] public UnityEngine.UIElements.VisualTreeAsset campDocument;
        [Tooltip("전투 화면 (docs/ui/battle-hud.md)")] public UnityEngine.UIElements.VisualTreeAsset battleDocument;

        public Sprite Ui(string name) => System.Array.Find(ui ?? System.Array.Empty<Sprite>(), s => s != null && s.name == name);

        public GameObject UiPrefab(string name) => System.Array.Find(uiPrefabs ?? System.Array.Empty<GameObject>(), p => p != null && p.name == name);

        [Header("Campaign map (docs/art/campaign-map.md)")]
        [Tooltip("CampaignMapLayout.Names 의 프리팹. 도구 모음 > 지도 > 프리팹 목록 채우기로 채운다")]
        public GameObject[] mapProps;

        public GameObject MapProp(string name) => System.Array.Find(mapProps ?? System.Array.Empty<GameObject>(), p => p != null && p.name == name);
    }
}
