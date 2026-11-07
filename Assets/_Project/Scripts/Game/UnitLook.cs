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
        [Tooltip("Sidekick 레시피 이름 (Assets/_Project/Art/Characters/<이름>.sk). 있으면 구운 캐릭터를 쓴다")] public string recipe;
        [Tooltip("레시피가 없을 때 쓰는 프리팹. 레시피가 있으면 굽기 전 클론의 대체 모델")] public GameObject model;
        [Tooltip("무기를 붙일 뼈 이름을 정한다")] public Rig rig;
        public Grip[] weapons;
        [Tooltip("비우면 ArtCatalog.unitAnimator")] public RuntimeAnimatorController animator;

        public const string BakedFolder = "Assets/_Generated/Resources/Characters";

        public static string BakedPath(string recipe) => BakedFolder + "/" + recipe + "/" + recipe + ".prefab";

        // Baked prefabs are gitignored and get fresh fileIDs on every clone, so they are found by recipe name, never by reference.
        public GameObject Model
        {
            get
            {
                if (string.IsNullOrEmpty(recipe)) return model;
                var baked = Resources.Load<GameObject>("Characters/" + recipe + "/" + recipe);
                if (baked != null) return baked;
                Debug.LogWarning(name + ": 레시피 " + recipe + " 를 굽지 않아 대체 모델을 쓴다 (/synty-reload)");
                return model;
            }
        }

        public static string Bone(Rig rig, Hand hand) => rig == Rig.Sidekick
            ? hand == Hand.Right ? "prop_r" : "prop_l"
            : hand == Hand.Right ? "Hand_R" : "Hand_L";
    }
}
