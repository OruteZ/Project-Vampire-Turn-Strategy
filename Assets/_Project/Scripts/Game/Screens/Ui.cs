using UnityEngine;

namespace VampireSpawn.Game.Screens
{
    // Colour tokens: docs/art/ui-style.md 2-1 (the same values as Assets/_Project/UI/Theme.uss).
    public static class Ui
    {
        public static readonly Color Ink = C(0x12131a), Deep = C(0x0f1016), Panel = C(0x1a1c25), Panel2 = C(0x25212d), Line = C(0x3a3442),
            Bone = C(0xebe3d2), Muted = C(0xb9b2bf), Dim = C(0x7d7684), Candle = C(0xe3a84b), Blood = C(0xc8304a), BloodDim = C(0x6e1e2c),
            Spawn = C(0xa650c4), Steel = C(0x8fb0cc), SteelDim = C(0x4a5d6e),
            Sun = C(0xeadcae), MoonBlood = C(0x9e1b2f);

        static Color C(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        public static Color Alpha(Color c, float a) { c.a = a; return c; }
    }
}
