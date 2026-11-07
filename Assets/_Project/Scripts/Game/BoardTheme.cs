using UnityEngine;

namespace VampireSpawn.Game
{
    // One stage look (docs/art/stage-dressing.md 2): what blocks a rock tile and what fills the world around the board.
    [CreateAssetMenu(fileName = "Theme", menuName = "VampireSpawn/Board Theme")]
    public sealed class BoardTheme : ScriptableObject
    {
        [Tooltip("이 모습을 쓰는 장 id (demo, s1 ...)")] public string[] stages;
        [Tooltip("지나갈 수 있는 칸과 보드 밖 바닥")] public Color ground = new Color(0.17f, 0.16f, 0.19f);
        [Tooltip("막힌 칸(#)에 세우는 것")] public GameObject[] blockers;
        [Tooltip("막힌 칸 소품을 칸 줄 방향(가로)으로 세운다. 벽처럼 옆 칸과 이어 보일 때")] public bool alignBlockers;
        public float blockerWidth = 1.6f;
        [Tooltip("보드 둘레의 높은 소품. 카메라 쪽(남쪽)에는 두지 않는다")] public GameObject[] border;
        [Tooltip("보드 둘레의 낮은 소품. 어디든 둔다")] public GameObject[] scatter;
        public Color fog = new Color(0.07f, 0.07f, 0.1f);
        public Color ambient = new Color(0.25f, 0.25f, 0.32f);
    }
}
