using System;
using UnityEngine;

namespace VampireSpawn.Game
{
    public enum Rig { Sidekick, Polygon }

    public enum Hand { Right, Left }

    // docs/art/unit-visuals.md 3-1. AsIs is 0 so grips made before holds existed keep their look.
    public enum Hold { AsIs, Blade, Pole, Spear }

    [Serializable]
    public struct Grip
    {
        public GameObject prefab;
        public Hand hand;
        [Tooltip("칼날: 칼끝이 앞 아래. 창: 칼날처럼 겨누고 자루 35% 를 쥔다. 자루: 세워 들고 자루 35% 를 쥔다. 그대로: 그 뼈대용 Synty 무기 (활·석궁·방패)")] public Hold hold;
        [Tooltip("쥐는 법 위에 더하는 미세 조정 (뼈 공간)")] public Vector3 position;
        public Vector3 rotation;
    }

    // One unit's look (docs/art/unit-visuals.md 3). Swapping the model for another of the same rig keeps the grips.
    [CreateAssetMenu(fileName = "UnitLook", menuName = "VampireSpawn/Unit Look")]
    public sealed class UnitLook : ScriptableObject
    {
        [Tooltip("병종 key 또는 하수인 key. ArtCatalog 가 이것으로 찾는다")] public string key;
        [Tooltip("병종 아이콘 (Synty UI 스프라이트 이름, ArtCatalog.ui 에 있어야 한다)")] public string icon;
        [Tooltip("Sidekick 레시피 이름 (Assets/_Project/Art/Characters/<이름>.sk). 있으면 구운 캐릭터를 쓴다")] public string recipe;
        [Tooltip("레시피가 없을 때 쓰는 프리팹. 레시피가 있으면 굽기 전 클론의 대체 모델")] public GameObject model;
        [Tooltip("무기를 붙일 뼈 이름을 정한다")] public Rig rig;
        public Grip[] weapons;
        [Tooltip("비우면 ArtCatalog.unitAnimator")] public RuntimeAnimatorController animator;
        [Tooltip("빈 칸은 SoundCatalog.unitDefault (docs/audio/sounds.md 6)")] public UnitSounds sounds;

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

        public const float PoleGrip = 0.35f;

        // Weapon frame (blade +Y, edge +X) into the hand bone. Measured on the Sidekick prop bones: -X runs along the forearm
        // in a thrust and down the leg at rest, -Y points forward at rest. Blades tilt 55 deg from -X toward -Y so a long
        // Dark Fantasy sword clears the floor at rest and still leads a thrust. Polygon hand bones are the same frame, axes swapped.
        public static Quaternion Socket(Rig rig, Hand hand, Hold hold)
        {
            if (hold == Hold.AsIs) return Quaternion.identity;
            var prop = Quaternion.Euler(0f, 0f, hold == Hold.Pole ? -90f : 145f);
            if (rig == Rig.Sidekick) return prop;
            var fromProp = Quaternion.LookRotation(Vector3.up, hand == Hand.Right ? Vector3.forward : Vector3.back);
            return fromProp * prop;
        }
    }
}
