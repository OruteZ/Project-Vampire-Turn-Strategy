using UnityEngine;

namespace VampireSpawn.Game
{
    [CreateAssetMenu(fileName = "ArtCatalog", menuName = "VampireSpawn/Art Catalog")]
    public sealed class ArtCatalog : ScriptableObject
    {
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

        [Header("Units")]
        [Tooltip("UnitKind 순서: pspear, parcher, sword, archer, knight, hunter")]
        public UnitLook[] allyLooks;
        public UnitLook skeletonLook, vampireLook;
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
        public Color coffin = new Color(0.25f, 0.06f, 0.11f);

        [Header("UI")]
        [Tooltip("Synty UI 스프라이트. 이름으로 찾는다 (docs/art/ui-style.md)")]
        public Sprite[] ui;

        [Tooltip("Synty UI 장식 프리팹 (Crest, Section_Break). 이름으로 찾는다")]
        public GameObject[] uiPrefabs;

        [Header("UI Toolkit trial (#44)")]
        public UnityEngine.UIElements.PanelSettings panel;
        public UnityEngine.UIElements.VisualTreeAsset menuDocument;

        public Sprite Ui(string name) => System.Array.Find(ui ?? System.Array.Empty<Sprite>(), s => s != null && s.name == name);

        public GameObject UiPrefab(string name) => System.Array.Find(uiPrefabs ?? System.Array.Empty<GameObject>(), p => p != null && p.name == name);
    }
}
