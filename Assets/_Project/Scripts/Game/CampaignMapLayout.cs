using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VampireSpawn.Game
{
    public readonly struct MapProp
    {
        public readonly string Prefab;
        public readonly float Dx, Dz, Yaw, Width, MaxHeight, Lift;

        public MapProp(string prefab, float dx, float dz, float yaw, float width, float maxHeight = 0f, float lift = 0f)
        {
            Prefab = prefab; Dx = dx; Dz = dz; Yaw = yaw; Width = width; MaxHeight = maxHeight; Lift = lift;
        }
    }

    public readonly struct MapLight
    {
        public readonly Vector3 At;
        public readonly bool Blood;
        public readonly float Intensity, Range;

        public MapLight(float dx, float dy, float dz, float intensity, float range, bool blood = false)
        {
            At = new Vector3(dx, dy, dz); Intensity = intensity; Range = range; Blood = blood;
        }
    }

    public sealed class MapRegion
    {
        public string Stage;
        public Vector2 Anchor, Brazier, Camp;
        public float MarkerHeight = 4.5f, Radius = 6f, ViewDistance = 40f;
        public MapProp[] Props = System.Array.Empty<MapProp>();
        public MapLight[] Lights = System.Array.Empty<MapLight>();
    }

    public readonly struct ForestBand
    {
        public readonly float MinZ, MaxZ;
        public readonly string[] Trees, Smalls;
        public readonly Vector2 TreeWidth, SmallWidth;

        public ForestBand(float minZ, float maxZ, string[] trees, Vector2 treeWidth, string[] smalls, Vector2 smallWidth)
        {
            MinZ = minZ; MaxZ = maxZ; Trees = trees; TreeWidth = treeWidth; Smalls = smalls; SmallWidth = smallWidth;
        }
    }

    // docs/art/campaign-map.md, first pass (1-1): coordinates are map-local (x east, z north). The prefabs are listed by name
    // here and found in ArtCatalog.mapProps, which the Tools window fills from Names (stage-select-handoff.md 3).
    public static class CampaignMapLayout
    {
        public static readonly Vector3 Origin = new Vector3(0f, 0f, 500f);
        public static readonly Vector2 Start = new Vector2(-30f, -6f);
        public const float RoadWidth = 2.2f, CastleRadius = 9f;
        public static readonly Color Floor = Hex(0x1f211c), RoadLit = Hex(0x5b4a3a), RoadDark = Hex(0x2f2a28), Ambient = Hex(0x262938), Fog = Hex(0x0d0f16),
            Moon = Hex(0xeadcae), Blood = Hex(0xc8304a);

        public const string Brazier = "SM_Prop_Brazier_01", Fire = "FX_Fire_01";

        static string[] Range(string stem, int from, int to) => Enumerable.Range(from, to - from + 1).Select(i => stem + i.ToString("00")).ToArray();

        public static readonly MapRegion[] Regions =
        {
            new MapRegion { Stage = "s1", Anchor = new Vector2(-24, 4), Brazier = new Vector2(3, -3), Camp = new Vector2(-13, 5), Props = new[] {
                new MapProp("SM_Prop_Shed_01", -2.5f, 2, 20, 5), new MapProp("SM_Veh_Wagon_01", 2.5f, 1, 110, 3.5f), new MapProp("SM_Prop_Well_01", -4, -2, 0, 1.8f) } },
            new MapRegion { Stage = "s2", Anchor = new Vector2(-4, 16), Brazier = new Vector2(3, 5), Camp = new Vector2(6, 27), Props = new[] {
                new MapProp("SM_Bld_Bridge_Base_01", 0, 0, 0, 7), new MapProp("SM_Bld_Bridge_Base_01_Damaged_01", -7, -0.4f, 0, 7), new MapProp(Brazier, 1.8f, 3.8f, 0, 0.9f),
                new MapProp(Fire, 1.8f, 3.8f, 0, 0, lift: 0.55f) },
                Lights = new[] { new MapLight(1.8f, 1.4f, 3.8f, 2f, 8f) } },
            new MapRegion { Stage = "s3", Anchor = new Vector2(18, 28), Brazier = new Vector2(-3, -4), Camp = new Vector2(16, 37), MarkerHeight = 5f, Props = new[] {
                new MapProp("SM_Bld_Ruin_02", 0, 2.5f, 180, 9), new MapProp("SM_Bld_Wall_Archway_02", 0, -1.5f, 0, 4), new MapProp("SM_Prop_Statue_01", -4, 0, 90, 1.5f) } },
            new MapRegion { Stage = "s4", Anchor = new Vector2(2, 42), Brazier = new Vector2(-3, -3), Camp = new Vector2(-9, 50), Props = new[] {
                new MapProp("SM_Prop_Crypt_01", 0, 4.5f, 180, 5), new MapProp("SM_Bld_Gates_Cemetary_01", 0, -3.5f, 0, 4), new MapProp("SM_Bld_Fence_Cemetary_01", -3.8f, -3.5f, 0, 3) } },
            new MapRegion { Stage = "s5", Anchor = new Vector2(-22, 54), Brazier = new Vector2(3, -3), Camp = new Vector2(-16, 66), Props = new[] {
                new MapProp("SM_Env_Rock_Cliff_01", -6.5f, 2, 90, 9, 9), new MapProp("SM_Env_Rock_Cliff_03", 6.5f, 3, 270, 9, 9), new MapProp("SM_Env_Cliff_Basalt_02", -5, 7, 0, 7, 8) } },
            new MapRegion { Stage = "s6", Anchor = new Vector2(-4, 68), Brazier = new Vector2(3, -5), Camp = new Vector2(10, 68.5f), Props = new[] {
                new MapProp("SM_Bld_Ruin_05", 0, 3.5f, 180, 8), new MapProp("SM_Bld_Base_Wall_Half_01", -5, -1, 90, 4), new MapProp("SM_Bld_Base_Wall_Half_01", 5, -1, 90, 4) } },
            new MapRegion { Stage = "s7", Anchor = new Vector2(22, 78), Brazier = new Vector2(-4, -3), Camp = new Vector2(18, 86.5f), MarkerHeight = 5f, Props = new[] {
                new MapProp("SM_Bld_Buttress_Pillar_04", -3, 1, 0, 2), new MapProp("SM_Bld_Buttress_Pillar_04", 3, 1, 0, 2), new MapProp("SM_Bld_Pillar_05", 7, 3, 0, 3) } },
            new MapRegion { Stage = "s8", Anchor = new Vector2(4, 90), Brazier = new Vector2(-4, -2), Camp = new Vector2(-2, 95.5f), Props = new[] {
                new MapProp("SM_Prop_Fire_Pit_01", 0, 0, 0, 2), new MapProp(Fire, 0, 0, 0, 0, lift: 0.3f), new MapProp("SM_Prop_Gallows_01", -3.5f, 2.5f, 30, 3),
                new MapProp("SM_Prop_Notice_Board_01", 2, -2.5f, -20, 1.5f) },
                Lights = new[] { new MapLight(0, 1.2f, 0, 2f, 8f) } },
            new MapRegion { Stage = "s9", Anchor = new Vector2(-12, 97), Brazier = new Vector2(3, -2), Camp = new Vector2(-20, 93), MarkerHeight = 5f, Props = new[] {
                new MapProp("SM_Bld_Bridge_Base_01", 0, 3, 0, 6), new MapProp("SM_Bld_Wall_Door_Large_01", 0, 6.5f, 180, 6), new MapProp("SM_Bld_Buttress_Pillar_03", -4, 6.5f, 0, 2.5f) } },
            new MapRegion { Stage = "s10", Anchor = new Vector2(2, 108), Brazier = new Vector2(5, -3), Camp = new Vector2(-20, 93), MarkerHeight = 12f, Radius = CastleRadius, ViewDistance = 52f, Props = new[] {
                new MapProp("SM_Bld_Wall_Window_Huge_01", 0, 0, 180, 10), new MapProp("SM_Bld_Wall_Window_Tall_Double_01", -7, 1, 160, 6),
                new MapProp("SM_Bld_Wall_Window_Tall_Double_01", 7, 1, 200, 6) },
                Lights = new[] { new MapLight(0, 4, 1, 2f, 14f, blood: true) } },
        };

        // 5-1: Catmull-Rom through these, one point per unit.
        public static readonly Vector2[] Road =
        {
            new Vector2(-40, -10), new Vector2(-32, -2), new Vector2(-24, 4), new Vector2(-12, 9), new Vector2(-4, 12), new Vector2(-4, 16), new Vector2(-4, 20),
            new Vector2(8, 24), new Vector2(18, 28), new Vector2(12, 36), new Vector2(2, 42), new Vector2(-10, 46), new Vector2(-22, 54), new Vector2(-18, 61),
            new Vector2(-4, 68), new Vector2(10, 72), new Vector2(22, 78), new Vector2(14, 85), new Vector2(4, 90), new Vector2(-6, 93), new Vector2(-12, 97),
            new Vector2(-12, 104), new Vector2(-4, 106), new Vector2(2, 108),
        };

        // 3-2 forest fill: a 6-unit grid, 45% of cells, kept off the road, regions and the camera edge.
        public const float ForestCell = 6f, ForestShare = 0.45f, ForestMinX = -70f, ForestMaxX = 70f, ForestMinZ = -20f, ForestMaxZ = 125f, ForestNearEdge = -14f;
        public const float ClearOfRoad = 4f, ClearOfRegion = 9f, ClearOfCastle = 12f, ClearOfCamp = 4f;

        public static readonly ForestBand[] Forest =
        {
            new ForestBand(-20, 34, Range("SM_Gen_Env_Tree_", 1, 3), new Vector2(4, 5), Range("SM_Gen_Env_Bush_Large_", 1, 4), new Vector2(2, 3)),
            new ForestBand(34, 58, Range("SM_Env_Tree_Dead_", 1, 4), new Vector2(4, 5), Range("SM_Gen_Env_Tree_Pine_", 1, 3), new Vector2(4, 4)),
            new ForestBand(58, 84, Range("SM_Gen_Env_Tree_Pine_", 1, 3), new Vector2(4, 5), Range("SM_Env_Rock_", 1, 3), new Vector2(2, 3)),
            new ForestBand(84, 125, Range("SM_Env_Tree_Dead_", 1, 4), new Vector2(4, 5), Range("SM_Env_Basalt_", 1, 5), new Vector2(2, 3)),
        };

        public static MapRegion Region(string stageId) => System.Array.Find(Regions, r => r.Stage == stageId);

        // Every prefab name the map places; ArtCatalog.mapProps must hold exactly these (UnitLookTests-style guard in CampaignMapLayoutTests).
        public static IEnumerable<string> Names =>
            Regions.SelectMany(r => r.Props.Select(p => p.Prefab))
                .Concat(Forest.SelectMany(b => b.Trees.Concat(b.Smalls)))
                .Concat(new[] { Brazier, Fire })
                .Distinct().OrderBy(n => n);

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }
}
