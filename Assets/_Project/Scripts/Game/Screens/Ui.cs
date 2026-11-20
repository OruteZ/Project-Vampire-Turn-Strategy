using UnityEngine;

namespace VampireSpawn.Game.Screens
{
    // Colour tokens: docs/art/ui-style.md 2-1 (the same values as Assets/_Project/UI/Theme.uss).
    public static class Ui
    {
        public static readonly Color Ink = Rgb(0x12131a), Deep = Rgb(0x0f1016), Panel = Rgb(0x1a1c25), Panel2 = Rgb(0x25212d), Line = Rgb(0x3a3442),
            Bone = Rgb(0xebe3d2), Muted = Rgb(0xb9b2bf), Dim = Rgb(0x7d7684), Candle = Rgb(0xe3a84b), Blood = Rgb(0xc8304a), BloodDim = Rgb(0x6e1e2c),
            Spawn = Rgb(0xa650c4), Steel = Rgb(0x8fb0cc), SteelDim = Rgb(0x4a5d6e),
            Sun = Rgb(0xeadcae), MoonBlood = Rgb(0x9e1b2f);

        public static Color Rgb(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        public static Color Alpha(Color c, float a) { c.a = a; return c; }
    }
}
