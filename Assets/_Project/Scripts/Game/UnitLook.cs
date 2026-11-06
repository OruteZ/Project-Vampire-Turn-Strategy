using System;
using UnityEngine;

namespace VampireSpawn.Game
{
    public enum Rig { Sidekick, Polygon }

    public enum Hand { Right, Left }

    [Serializable]
    public struct Grip
    {
        public GameObject prefab;
        public Hand hand;
        public Vector3 position;
        public Vector3 rotation;
    }

    // One unit's look (docs/art/unit-visuals.md 3). Swapping the model for another of the same rig keeps the grips.
    [CreateAssetMenu(fileName = "UnitLook", menuName = "VampireSpawn/Unit Look")]
    public sealed class UnitLook : ScriptableObject
    {
        [Tooltip("캐릭터 프리팹. Sidekick 으로 만든 새 캐릭터는 여기에 넣는다")] public GameObject model;
        [Tooltip("무기를 붙일 뼈 이름을 정한다")] public Rig rig;
        public Grip[] weapons;
        [Tooltip("비우면 ArtCatalog.unitAnimator")] public RuntimeAnimatorController animator;

        public static string Bone(Rig rig, Hand hand) => rig == Rig.Sidekick
            ? hand == Hand.Right ? "prop_r" : "prop_l"
            : hand == Hand.Right ? "Hand_R" : "Hand_L";
    }
}
