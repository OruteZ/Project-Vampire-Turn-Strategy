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
    }
}
