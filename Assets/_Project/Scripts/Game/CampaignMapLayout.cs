using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    public readonly struct MapSpot
    {
        public readonly string Stage;
        public readonly Vector3 At;

        public MapSpot(string stage, float x, float y, float z) { Stage = stage; At = new Vector3(x, y, z); }
    }

    public readonly struct MapShot
    {
        public readonly float Pitch, Yaw, Fov, Distance;

        public MapShot(float pitch, float yaw, float fov, float distance) { Pitch = pitch; Yaw = yaw; Fov = fov; Distance = distance; }

        public Vector3 Offset => Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.back * Distance;
    }

    // docs/art/campaign-map.md. Bake input only: the running map reads the Camp scene's World Map.
    public static class CampaignMapLayout
    {
        public const string WorldScene = "Assets/_Project/Scenes/World.unity";
        public const string WorldSource = "Assets/Synty/PolygonDarkFantasy/Scenes/Demo_Cathedral_01.unity";

        public static readonly MapSpot[] Spots =
        {
            new MapSpot("s1", 64f, -9.7f, 51.7f), new MapSpot("s2", 33.7f, -9.8f, 45.1f), new MapSpot("s3", 22f, -10.9f, 44f),
            new MapSpot("s4", 11f, -11f, 31f), new MapSpot("s5", 4f, -12f, 38f), new MapSpot("s6", -20f, -10.9f, 30f),
            new MapSpot("s7", -16.8f, -3.7f, 14.3f), new MapSpot("s8", 34f, -9.2f, 16.5f), new MapSpot("s9", 2.3f, -3.5f, 11f),
            new MapSpot("s10", 2f, 0f, 0.8f), new MapSpot("s11", 10f, -1.4f, 8f), new MapSpot("s12", 5f, 0f, 3f),
            new MapSpot("s13", -4.5f, 0f, 2.5f), new MapSpot("s14", -8.6f, 0f, -3.4f), new MapSpot("s15", -6f, 0f, -16f),
            new MapSpot("s16", -0.9f, 0f, -21.8f), new MapSpot("s17", -9.6f, 3f, -27.4f), new MapSpot("s18", 11.5f, 3f, -27.4f),
            new MapSpot("s19", 0f, 0.7f, -31.9f), new MapSpot("s20", -0.7f, 0f, -40.5f),
        };

        public static readonly MapShot Road = new MapShot(16f, 222f, 45f, 150f), RoadStage = new MapShot(16f, 222f, 45f, 40f),
            Castle = new MapShot(52f, 229f, 40f, 85f), CastleStage = new MapShot(52f, 229f, 40f, 32f), Camp = new MapShot(16f, 222f, 45f, 22f);

        public static MapShot Wide(CampFace face) => face == CampFace.Castle ? Castle : Road;

        public static MapShot Stage(CampFace face) => face == CampFace.Castle ? CastleStage : RoadStage;

        public static readonly Vector3 MarkerLift = new Vector3(0f, 2f, 0f), ViewLift = new Vector3(0f, 1f, 0f),
            CampAt = new Vector3(83.5f, -9.8f, 56f), CampUnit = new Vector3(1.2f, 0f, -1.4f);

        public const float CutHeight = 5f, FogEnd = 3000f;
        public static readonly string[] CutByLowestPoint = { "Floors", "Props", "Lighting" };

        public static readonly Color FillColor = new Color(0.75f, 0.8f, 1f), Moon = Screens.Ui.Rgb(0xeadcae);
        public const float FillIntensity = 0.7f;
        public static readonly Vector3 FillAngles = new Vector3(45f, 200f, 0f);
    }
}
