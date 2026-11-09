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
        public Material plain, lit, sunlit, rock, water;
        public GameObject[] rockProps;
        public GameObject brazier, fire;
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
        [Tooltip("Polygon 캐릭터와, 묶음이 빈 UnitLook 이 쓰는 기본 애니메이터")] public RuntimeAnimatorController unitAnimator;
        [Tooltip("Sidekick 캐릭터가 스폰이 되었을 때 (맨손 근접)")] public RuntimeAnimatorController spawnAnimator;
        public Material flat;
        public float unitHeight = 1.5f;
        public Color peasantRing = new Color(0.72f, 0.61f, 0.45f);
        public Color regularRing = new Color(0.56f, 0.69f, 0.80f);
        public Color knightRing = new Color(0.85f, 0.87f, 0.89f);
        public Color hunterRing = new Color(0.53f, 0.72f, 0.65f);
        public Color skeletonRing = new Color(0.71f, 0.66f, 0.56f);
        public Color vampireRing = new Color(0.78f, 0.19f, 0.29f);
        public Color spawnRing = new Color(0.65f, 0.31f, 0.77f);
        [Tooltip("잠든 뱀파이어의 관. 색칠 없이 그대로 (docs/art/unit-visuals.md 5)")] public GameObject coffin;

        public UnitLook Look(string key) => Find(allyLooks, key) ?? Find(minionLooks, key);

        // Weapon icon sprite name for a unit type, from its look (docs/art/unit-visuals.md).
        public string Icon(string key) => Look(key)?.icon;

        static UnitLook Find(UnitLook[] looks, string key) => System.Array.Find(looks ?? System.Array.Empty<UnitLook>(), l => l != null && l.key == key);

        public Color TierRing(VampireSpawn.Core.Tier tier) => tier switch
        {
            VampireSpawn.Core.Tier.Peasant => peasantRing,
            VampireSpawn.Core.Tier.Knight => knightRing,
            VampireSpawn.Core.Tier.Hunter => hunterRing,
            _ => regularRing,
        };

        [Header("UI")]
        [Tooltip("Synty UI 스프라이트. 이름으로 찾는다 (docs/art/ui-style.md)")]
        public Sprite[] ui;

        [Tooltip("Synty UI 장식 프리팹 (Crest, Section_Break). 이름으로 찾는다")]
        public GameObject[] uiPrefabs;

        [Header("UI Toolkit trial (#44)")]
        public UnityEngine.UIElements.PanelSettings panel;
        public UnityEngine.UIElements.VisualTreeAsset menuDocument;
        [Tooltip("장 선택 (docs/ui/stage-select.md 10)")] public UnityEngine.UIElements.VisualTreeAsset campDocument;

        public Sprite Ui(string name) => System.Array.Find(ui ?? System.Array.Empty<Sprite>(), s => s != null && s.name == name);

        public GameObject UiPrefab(string name) => System.Array.Find(uiPrefabs ?? System.Array.Empty<GameObject>(), p => p != null && p.name == name);

        [Header("Campaign map (docs/art/campaign-map.md)")]
        [Tooltip("CampaignMapLayout.Names 의 프리팹. 도구 모음 > 지도 > 프리팹 목록 채우기로 채운다")]
        public GameObject[] mapProps;

        public GameObject MapProp(string name) => System.Array.Find(mapProps ?? System.Array.Empty<GameObject>(), p => p != null && p.name == name);
    }
}
