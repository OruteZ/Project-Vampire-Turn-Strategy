using UnityEngine;

namespace VampireSpawn.Game
{
    // docs/art/stage-dressing.md 8: what a gap cell (_) looks like. The rules never see it.
    public enum GapLook { Pit, Water, LowWall }

    // One stage look (docs/art/stage-dressing.md 2): what blocks a rock tile and what fills the world around the board.
    [CreateAssetMenu(fileName = "Theme", menuName = "VampireSpawn/Board Theme")]
    public sealed class BoardTheme : ScriptableObject
    {
        [Tooltip("이 모습을 쓰는 장 id (demo, s1 ...)")] public string[] stages;
        [Tooltip("원정 지도의 지역 바닥색")] public Color ground = new Color(0.17f, 0.16f, 0.19f);
        [Tooltip("전투 지형 바탕 칠 (docs/art/battle-terrain.md 2-1)")] public TerrainLayer floor;
        [Tooltip("바탕 위 얼룩 칠")] public TerrainLayer patches;
        [Range(0f, 1f)] public float patchShare = 0.3f;
        [Tooltip("막힌 칸(#)에 세우는 것")] public GameObject[] blockers;
        [Tooltip("막힌 칸 소품을 칸 줄 방향(가로)으로 세운다. 벽처럼 옆 칸과 이어 보일 때")] public bool alignBlockers;
        public float blockerWidth = 1.6f;
        [Tooltip("모습 칸을 비운 틈(_) 칸의 모습 (docs/art/stage-dressing.md 8)")] public GapLook gap;
        [Tooltip("낮은 담 모습의 틈 칸에 세우는 것. 비우면 ArtCatalog.lowWallProps")] public GameObject[] lowWalls;
        [Tooltip("보드 둘레의 높은 소품. 카메라 쪽(남쪽)에는 두지 않는다")] public GameObject[] border;
        [Tooltip("보드 둘레의 낮은 소품. 어디든 둔다")] public GameObject[] scatter;
        [Tooltip("비우지 않으면 맨바닥(블록아웃, docs/art/stage-dressing.md 6): 지형·소품 없이 이 재질의 납작한 판 하나")] public Material bareFloor;
        [Tooltip("굽기 때 꾸미기 프리팹에 넣는 처음 안개·주변광. 구운 뒤엔 프리팹의 StageDressing 값이 쓰인다")] public Color fog = new Color(0.07f, 0.07f, 0.1f);
        public Color ambient = new Color(0.25f, 0.25f, 0.32f);
    }
}
