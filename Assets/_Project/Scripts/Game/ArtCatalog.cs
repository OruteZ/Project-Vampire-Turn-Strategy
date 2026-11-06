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

        [Header("Units")]
        [Tooltip("Indexed by UnitKind: pspear, parcher, sword, archer, knight, hunter")]
        public GameObject[] allyModels;
        public GameObject skeleton, vampire;
        public RuntimeAnimatorController unitAnimator;
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

        public Sprite Ui(string name) => System.Array.Find(ui ?? System.Array.Empty<Sprite>(), s => s != null && s.name == name);
    }
}
