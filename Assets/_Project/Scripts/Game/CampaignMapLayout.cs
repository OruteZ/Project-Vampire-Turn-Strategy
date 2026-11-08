using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VampireSpawn.Game
{
    public readonly struct MapProp
    {
        public readonly string Prefab;
        public readonly float Dx, Dz, Yaw, Width, MaxHeight, Lift;
        public readonly bool OnTop;

        // onTop: sits on the top of the previous prop's bounds (4-2 "위에 얹기").
        public MapProp(string prefab, float dx, float dz, float yaw, float width, float maxHeight = 0f, float lift = 0f, bool onTop = false)
        {
            Prefab = prefab; Dx = dx; Dz = dz; Yaw = yaw; Width = width; MaxHeight = maxHeight; Lift = lift; OnTop = onTop;
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

    // 4-2 채움: Count props picked by hash from Names, in a ring R1..R2 around the anchor; only where dz < SouthOf.
    public sealed class MapFill
    {
        public (string Name, float Width)[] Names;
        public int Count;
        public float R1, R2, SouthOf = float.PositiveInfinity;
    }

    // 3-1 줄 소품: one prop every Spacing from From to To, prefabs in turn.
    public readonly struct MapLine
    {
        public readonly string Name;
        public readonly string[] Prefabs;
        public readonly Vector2 From, To;
        public readonly float Spacing, Width, MaxHeight;
        public readonly bool Wall;

        public MapLine(string name, string[] prefabs, Vector2 from, Vector2 to, float spacing, float width, float maxHeight, bool wall = false)
        {
            Name = name; Prefabs = prefabs; From = from; To = to; Spacing = spacing; Width = width; MaxHeight = maxHeight; Wall = wall;
        }
    }

    public sealed class MapRegion
    {
        public string Stage;
        public Vector2 Anchor, Brazier, Camp;
        public float MarkerHeight = 4.5f, Radius = 6f, ViewDistance = 40f;
        public MapProp[] Props = System.Array.Empty<MapProp>();
        public MapLight[] Lights = System.Array.Empty<MapLight>();
        public MapFill Fill;
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

    // docs/art/campaign-map.md: coordinates are map-local (x east, z north). The prefabs are listed by name
    // here and found in ArtCatalog.mapProps, which the Tools window fills from Names (docs/art/campaign-map.md 구현 메모).
    public static class CampaignMapLayout
    {
        public static readonly Vector3 Origin = new Vector3(0f, 0f, 500f);
        public static readonly Vector2 Start = new Vector2(-30f, -6f);
        public const float RoadWidth = 2.2f, CastleRadius = 9f;
        public static readonly Color Floor = Hex(0x1f211c), RoadLit = Hex(0x5b4a3a), RoadDark = Hex(0x2f2a28), Ambient = Hex(0x262938), Fog = Hex(0x0d0f16),
            Moon = Hex(0xeadcae), Blood = Hex(0xc8304a), SideRoad = Hex(0x2a2624);

        public const string Brazier = "SM_Prop_Brazier_01", Fire = "FX_Fire_01";

        static string[] Range(string stem, int from, int to) => Enumerable.Range(from, to - from + 1).Select(i => stem + i.ToString("00")).ToArray();

        static (string, float)[] W(float width, params string[] names) => names.Select(n => (n, width)).ToArray();
        static (string, float)[] W(params (string, float)[][] groups) => groups.SelectMany(g => g).ToArray();

        // 4-2, every row. Fire on braziers and torches sits on top of them (lift).
        public static readonly MapRegion[] Regions =
        {
            new MapRegion { Stage = "s1", Anchor = new Vector2(-24, 4), Brazier = new Vector2(3, -3), Camp = new Vector2(-13, 5), Props = new[] {
                new MapProp("SM_Prop_Shed_01", -2.5f, 2, 20, 5), new MapProp("SM_Veh_Wagon_01", 2.5f, 1, 110, 3.5f), new MapProp("SM_Prop_Well_01", -4, -2, 0, 1.8f),
                new MapProp("SM_Prop_Rubble_Pile_Burnt_01", 1, 3.5f, 0, 2.5f), new MapProp("FX_Smoke_Small_01", 1, 3.5f, 0, 0), new MapProp("SM_Prop_Sack_01", 3.5f, -0.5f, 40, 0.8f) },
                Fill = new MapFill { Count = 8, R1 = 3.5f, R2 = 6, Names = W(W(0.9f, "SM_Prop_Crate_01", "SM_Prop_Crate_02", "SM_Prop_Barrel_01", "SM_Prop_Sack_03"),
                    W(1f, Range("SM_Gen_Env_Grass_Tall_", 1, 2)), W(2f, "SM_Gen_Env_Bush_Large_01"), W(4f, "SM_Gen_Env_Tree_02")) } },
            new MapRegion { Stage = "s2", Anchor = new Vector2(-4, 16), Brazier = new Vector2(3, 5), Camp = new Vector2(6, 27), Props = new[] {
                new MapProp("SM_Bld_Bridge_Base_01", 0, 0, 0, 7), new MapProp("SM_Bld_Bridge_Base_01_Damaged_01", -7, -0.4f, 0, 7), new MapProp(Brazier, 1.8f, 3.8f, 0, 0.9f),
                new MapProp(Fire, 1.8f, 3.8f, 0, 0, lift: 0.55f), new MapProp("SM_Prop_Shed_01", -4, 5, -30, 4), new MapProp("SM_Gen_Env_Log_01", -6, -3.5f, 70, 3) },
                Lights = new[] { new MapLight(1.8f, 1.4f, 3.8f, 2f, 8f) },
                Fill = new MapFill { Count = 8, R1 = 4, R2 = 7, Names = W(W(1.2f, Range("SM_Gen_Env_Rock_", 1, 4)), W(1f, "SM_Gen_Env_Fern_01"), W(1.5f, "SM_Gen_Env_Bush_01"),
                    W(4f, "SM_Gen_Env_Tree_01")) } },
            new MapRegion { Stage = "s3", Anchor = new Vector2(18, 28), Brazier = new Vector2(-3, -4), Camp = new Vector2(16, 37), MarkerHeight = 5f, Props = new[] {
                new MapProp("SM_Bld_Ruin_02", 0, 2.5f, 180, 9), new MapProp("SM_Bld_Wall_Archway_02", 0, -1.5f, 0, 4), new MapProp("SM_Prop_Statue_01", -4, 0, 90, 1.5f),
                new MapProp("SM_Prop_Pew_Large_01_Damaged_01", 1.5f, 0.8f, 0, 2.5f), new MapProp("SM_Prop_Bell_Large_01", 4, 3, 20, 1.6f) },
                Fill = new MapFill { Count = 7, R1 = 5, R2 = 7, Names = W(W(3.5f, Range("SM_Env_Tree_Dead_", 1, 4)), W(1f, "SM_Env_Rocks_Small_01"), W(3f, "SM_Bld_Wall_Ruin_End_01")) } },
            new MapRegion { Stage = "s4", Anchor = new Vector2(2, 42), Brazier = new Vector2(-3, -3), Camp = new Vector2(-9, 50), Props = new[] {
                new MapProp("SM_Prop_Crypt_01", 0, 4.5f, 180, 5), new MapProp("SM_Bld_Gates_Cemetary_01", 0, -3.5f, 0, 4), new MapProp("SM_Bld_Fence_Cemetary_01", -3.8f, -3.5f, 0, 3),
                new MapProp("SM_Bld_Fence_Cemetary_02", 3.8f, -3.5f, 0, 3), new MapProp("SM_Env_Tree_Dead_02", 4, 3, 40, 4) },
                Fill = new MapFill { Count = 10, R1 = 2, R2 = 6, Names = W(W(0.9f, Range("SM_Prop_Tomb_Stone_", 1, 6)), W(2f, "SM_Prop_Tomb_01"), W(1f, "SM_Prop_Bone_Pile_01")) } },
            new MapRegion { Stage = "s5", Anchor = new Vector2(-22, 54), Brazier = new Vector2(3, -3), Camp = new Vector2(-16, 66), Props = new[] {
                new MapProp("SM_Env_Rock_Cliff_01", -6.5f, 2, 90, 9, 9), new MapProp("SM_Env_Rock_Cliff_03", 6.5f, 3, 270, 9, 9), new MapProp("SM_Env_Cliff_Basalt_02", -5, 7, 0, 7, 8),
                new MapProp("SM_Prop_Torch_01", -1.8f, -3, 0, 0.6f), new MapProp("SM_Prop_Torch_02", 1.8f, 0, 0, 0.6f), new MapProp("SM_Prop_Torch_01", -1.8f, 3, 0, 0.6f),
                new MapProp(Fire, -1.8f, -3, 0, 0, lift: 1.6f), new MapProp(Fire, 1.8f, 0, 0, 0, lift: 1.6f), new MapProp(Fire, -1.8f, 3, 0, 0, lift: 1.6f) },
                Lights = new[] { new MapLight(-1.8f, 1.8f, -3, 1.4f, 6f), new MapLight(1.8f, 1.8f, 0, 1.4f, 6f), new MapLight(-1.8f, 1.8f, 3, 1.4f, 6f) },
                Fill = new MapFill { Count = 7, R1 = 5, R2 = 8, Names = W(W(3f, Range("SM_Gen_Env_Tree_Pine_", 1, 3)), W(2f, Range("SM_Env_Basalt_", 1, 3)),
                    W(1f, "SM_Gen_Env_Rock_Pebbles_01")) } },
            new MapRegion { Stage = "s6", Anchor = new Vector2(-4, 68), Brazier = new Vector2(3, -5), Camp = new Vector2(10, 68.5f), Props = new[] {
                new MapProp("SM_Bld_Ruin_05", 0, 3.5f, 180, 8), new MapProp("SM_Bld_Base_Wall_Half_01", -5, -1, 90, 4), new MapProp("SM_Bld_Base_Wall_Half_01", 5, -1, 90, 4),
                new MapProp("SM_Bld_Base_Wall_Half_02", -3.5f, -4, 0, 4), new MapProp("SM_Bld_Base_Wall_Half_02", 3.5f, -4, 0, 4), new MapProp("SM_Prop_Statue_People_01", 0, 0.5f, 180, 1.5f) },
                Fill = new MapFill { Count = 8, R1 = 4, R2 = 7, Names = W(W(2f, Range("SM_Gen_Env_Bush_Large_", 1, 4)), W(1.2f, Range("SM_Gen_Env_Shrub_", 1, 3)),
                    W(0.8f, Range("SM_Gen_Env_Flowers_", 1, 4)), W(4f, "SM_Gen_Env_Tree_03")) } },
            new MapRegion { Stage = "s7", Anchor = new Vector2(22, 78), Brazier = new Vector2(-4, -3), Camp = new Vector2(18, 86.5f), MarkerHeight = 5f, Props = new[] {
                new MapProp("SM_Bld_Buttress_Pillar_04", -3, 1, 0, 2), new MapProp("SM_Bld_Buttress_Pillar_04", 3, 1, 0, 2), new MapProp("SM_Bld_Pillar_05", 7, 3, 0, 3),
                new MapProp("SM_Prop_Flag_Hero_Damaged_01", -3, -0.5f, 0, 1.2f), new MapProp("SM_Prop_Flag_Hero_Damaged_02", 3, -0.5f, 0, 1.2f), new MapProp(Brazier, 5, -1, 0, 0.9f),
                new MapProp(Fire, 5, -1, 0, 0, lift: 0.55f) },
                Lights = new[] { new MapLight(5, 1.4f, -1, 1.6f, 7f) },
                Fill = new MapFill { Count = 6, R1 = 4, R2 = 7, SouthOf = 0, Names = W(W(3f, Range("SM_Gen_Env_Tree_Pine_", 1, 3)), W(1f, "SM_Env_Rocks_Small_02")) } },
            new MapRegion { Stage = "s8", Anchor = new Vector2(4, 90), Brazier = new Vector2(-4, -2), Camp = new Vector2(-2, 95.5f), Props = new[] {
                new MapProp("SM_Prop_Fire_Pit_01", 0, 0, 0, 2), new MapProp(Fire, 0, 0, 0, 0, lift: 0.3f), new MapProp("SM_Prop_Gallows_01", -3.5f, 2.5f, 30, 3),
                new MapProp("SM_Prop_Notice_Board_01", 2, -2.5f, -20, 1.5f), new MapProp("SM_Prop_Shed_01", 4.5f, 3, -60, 4), new MapProp("SM_Bld_Ruin_03", -5.5f, 4.5f, 0, 6),
                new MapProp("SM_Prop_Barricade_01", -2.5f, -4, 10, 3), new MapProp("SM_Prop_Barricade_03", 3, -4.5f, -15, 3) },
                Lights = new[] { new MapLight(0, 1.2f, 0, 2f, 8f) },
                Fill = new MapFill { Count = 8, R1 = 3.5f, R2 = 7, Names = W(W(0.9f, "SM_Prop_Crate_01", "SM_Prop_Crate_02", "SM_Prop_Barrel_01", "SM_Prop_Sack_02"),
                    W(0.5f, "SM_Prop_Lantern_01"), W(2f, "SM_Prop_Gibbet_01"), W(3f, "SM_Prop_Wagon_01_Damaged_01")) } },
            new MapRegion { Stage = "s9", Anchor = new Vector2(-12, 97), Brazier = new Vector2(3, -2), Camp = new Vector2(-20, 93), MarkerHeight = 5f, Props = new[] {
                new MapProp("SM_Bld_Bridge_Base_01", 0, 3, 0, 6), new MapProp("SM_Bld_Wall_Door_Large_01", 0, 6.5f, 180, 6), new MapProp("SM_Bld_Buttress_Pillar_03", -4, 6.5f, 0, 2.5f),
                new MapProp("SM_Bld_Buttress_Pillar_03", 4, 6.5f, 0, 2.5f), new MapProp("SM_Prop_Gargoyle_Statue_01", -2.5f, -1, 0, 1.4f),
                new MapProp("SM_Prop_Gargoyle_Statue_01", 2.5f, -1, 0, 1.4f), new MapProp("SM_Prop_Flag_Dark_01", -4, 5.5f, 0, 1.2f), new MapProp("SM_Prop_Flag_Dark_02", 4, 5.5f, 0, 1.2f),
                new MapProp(Brazier, 1.8f, 1, 0, 0.9f), new MapProp(Fire, 1.8f, 1, 0, 0, lift: 0.55f) },
                Lights = new[] { new MapLight(1.8f, 1.4f, 1, 1.6f, 7f) },
                Fill = new MapFill { Count = 5, R1 = 4, R2 = 7, SouthOf = 2, Names = W(W(5f, "SM_Env_Rock_Cliff_04"), W(1f, "SM_Gen_Env_Rock_Pebbles_02"), W(3f, "SM_Env_Tree_Dead_03")) } },
            new MapRegion { Stage = "s10", Anchor = new Vector2(2, 108), Brazier = new Vector2(5, -3), Camp = new Vector2(-20, 93), MarkerHeight = 12f, Radius = CastleRadius, ViewDistance = 52f, Props = new[] {
                new MapProp("SM_Bld_Wall_Window_Huge_01", 0, 0, 180, 10), new MapProp("SM_Bld_Wall_Window_Tall_Double_01", -7, 1, 160, 6),
                new MapProp("SM_Bld_Wall_Window_Tall_Double_01", 7, 1, 200, 6),
                new MapProp("SM_Bld_Buttress_Pillar_04", -10, 0, 0, 2.5f), new MapProp("SM_Bld_Topper_02", -10, 0, 0, 2.5f, onTop: true),
                new MapProp("SM_Bld_Buttress_Pillar_04", -4.5f, -0.5f, 0, 2.5f), new MapProp("SM_Bld_Topper_02", -4.5f, -0.5f, 0, 2.5f, onTop: true),
                new MapProp("SM_Bld_Buttress_Pillar_04", 4.5f, -0.5f, 0, 2.5f), new MapProp("SM_Bld_Topper_02", 4.5f, -0.5f, 0, 2.5f, onTop: true),
                new MapProp("SM_Bld_Buttress_Pillar_04", 10, 0, 0, 2.5f), new MapProp("SM_Bld_Topper_02", 10, 0, 0, 2.5f, onTop: true),
                new MapProp("SM_Prop_Flag_Dark_03", -10, -1.5f, 0, 1.4f), new MapProp("SM_Prop_Flag_Dark_03", 10, -1.5f, 0, 1.4f),
                new MapProp("SM_Prop_Gargoyle_Statue_01", 0, -3.5f, 180, 1.6f), new MapProp("SM_Env_Rock_Cliff_05", -6, 8, 0, 14, 14), new MapProp("SM_Env_Rock_Cliff_06", 10, 8, 0, 14, 14),
                new MapProp("FX_Bats_01", 0, 0, 0, 0, lift: 10), new MapProp("FX_Lightning_Background_01", 0, 28, 0, 0, lift: 20) },
                Lights = new[] { new MapLight(0, 4, 1, 2f, 14f, blood: true) } },
        };

        // 3-1. Lines skip any prop within 1.1 + 0.5 + width/2 of a road's centre line; that opens the pass, the wall gaps and the gate.
        public static readonly MapLine[] Lines =
        {
            new MapLine("Ridge", Range("SM_Env_Rock_Cliff_", 1, 6), new Vector2(-60, 58), new Vector2(40, 58), 7, 9, 10),
            new MapLine("Border Wall", Range("SM_Bld_Wall_Ruin_", 1, 4), new Vector2(8, 79), new Vector2(46, 79), 4, 4, 5, wall: true),
            new MapLine("Border Wall End", new[] { "SM_Env_Rock_Cliff_02" }, new Vector2(5, 79), new Vector2(5, 79), 1, 7, 8),
            new MapLine("Castle Wall", new[] { "SM_Bld_Base_Wall_01" }, new Vector2(-26, 103.5f), new Vector2(22, 103.5f), 4, 4, 6, wall: true),
            new MapLine("Castle Towers", new[] { "SM_Bld_Buttress_Pillar_04" }, new Vector2(-26, 103.5f), new Vector2(22, 103.5f), 12, 2.5f, 9),
            new MapLine("North Mountains", Range("SM_Gen_Env_Mountain_", 1, 3), new Vector2(-60, 132), new Vector2(60, 132), 40, 50, 40),
            new MapLine("Far Mountains", Range("SM_Gen_Env_Background_Mountain_", 1, 3), new Vector2(-80, 165), new Vector2(80, 165), 80, 90, 60),
            new MapLine("West Hills", Range("SM_Gen_Env_Hill_", 1, 5), new Vector2(-64, -10), new Vector2(-64, 120), 22, 24, 12),
            new MapLine("East Hills", Range("SM_Gen_Env_Hill_", 1, 5), new Vector2(60, -10), new Vector2(60, 120), 22, 24, 12),
        };
        public const float LineClearOfRoad = 1.1f + 0.5f, FillClearOfRoad = 2.5f, FillClearOfProp = 1.5f, FillClearOfWater = 3f, ClearOfWater = 4f, ClearOfLine = 3f;

        // 3 water and 5-2 side roads: ribbons like the road.
        public static readonly Vector2[] River = { new Vector2(-90, 18), new Vector2(-40, 14), new Vector2(-4, 16), new Vector2(30, 19), new Vector2(90, 15) };
        public static readonly Vector2[] Moat = { new Vector2(-34, 100), new Vector2(26, 100) };
        public const float RiverWidth = 6f, MoatWidth = 3.5f, SideRoadWidth = 1.6f;
        public static readonly Vector2[][] SideRoads =
        {
            new[] { new Vector2(2, 42), new Vector2(16, 45), new Vector2(34, 48) },
            new[] { new Vector2(2, 42), new Vector2(-8, 36), new Vector2(-26, 31) },
        };

        // 7: the camp, turned to face the next region.
        public static readonly MapProp[] CampProps =
        {
            new MapProp("SM_Prop_Campfire_Setup_01", 0, 0, 0, 1.6f), new MapProp(Fire, 0, 0, 0, 0, lift: 0.3f),
            new MapProp("SM_Prop_Flag_Hero_02", 1.5f, 0.5f, 0, 1.2f), new MapProp("SM_Veh_Wagon_01", -2.2f, 1, 0, 3.2f),
        };
        public static readonly MapLight CampLight = new MapLight(0, 1.2f, 0, 2f, 8f);

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
            Regions.SelectMany(r => r.Props.Select(p => p.Prefab).Concat(r.Fill?.Names.Select(n => n.Name) ?? Enumerable.Empty<string>()))
                .Concat(Forest.SelectMany(b => b.Trees.Concat(b.Smalls)))
                .Concat(Lines.SelectMany(l => l.Prefabs))
                .Concat(CampProps.Select(p => p.Prefab))
                .Concat(new[] { Brazier, Fire })
                .Distinct().OrderBy(n => n);

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }
}
