using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/art/unit-visuals.md 3-1, 4, 5.
    public class UnitLookTests
    {
        static ArtCatalog Art => AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");

        static IEnumerable<UnitLook> All => Art.allyLooks.Concat(Art.minionLooks).Append(Art.vampireLook);

        static IEnumerable<string> Looks() => All.Select(l => l.name);

        static UnitLook Look(string name) => All.First(l => l.name == name);

        [TestCaseSource(nameof(Looks))]
        public void HeldWeaponsPointTheWayTheirHoldSaysAtRest(string name)
        {
            var look = Look(name);
            var view = UnitView.Create(Art, look, null, 0, new Vector3(0f, -500f, 0f), 0f, Color.gray, true);
            try
            {
                var animator = view.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Play("Idle", 0, 0.4f);
                animator.Update(0f);
                var children = view.GetComponentsInChildren<Transform>(true);
                foreach (var g in look.weapons.Where(g => g.prefab != null))
                {
                    var w = children.First(t => t.name == g.prefab.name + "(Clone)");
                    // Any hold, a new weapon left on AsIs included: the long end must not trail behind the unit.
                    Assert.Greater(Vector3.Dot(LongEnd(w), Vector3.forward), -0.3f, name + " " + g.prefab.name + " (" + g.hold + ") trails behind, a reverse grip");
                    if (g.hold == Hold.AsIs) continue;
                    var blade = w.up;
                    if (g.hold == Hold.Pole)
                        Assert.Greater(Vector3.Dot(blade, Vector3.up), 0.7f, name + " " + g.prefab.name + " held upright, got " + blade);
                    else
                    {
                        Assert.Greater(Vector3.Dot(blade, Vector3.forward), 0.3f, name + " " + g.prefab.name + " points ahead, not behind (reverse grip), got " + blade);
                        var tip = w.TransformPoint(Vector3.up * w.GetComponentsInChildren<MeshFilter>().Max(m => m.sharedMesh.bounds.max.y));
                        Assert.Greater(tip.y, view.transform.position.y - 0.02f, name + " " + g.prefab.name + " tip above the floor");
                    }
                }
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }

        // World direction from the weapon's pivot to the far end of its longest axis.
        static Vector3 LongEnd(Transform w)
        {
            var meshes = w.GetComponentsInChildren<MeshFilter>(true).Select(m => m.sharedMesh)
                .Concat(w.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s => s.sharedMesh)).Where(m => m != null).ToList();
            var b = meshes[0].bounds;
            foreach (var m in meshes) b.Encapsulate(m.bounds);
            var axis = b.size.x >= b.size.y && b.size.x >= b.size.z ? 0 : b.size.y >= b.size.z ? 1 : 2;
            var local = Vector3.zero;
            local[axis] = Mathf.Abs(b.max[axis]) >= Mathf.Abs(b.min[axis]) ? 1f : -1f;
            return w.TransformDirection(local).normalized;
        }

        [Test]
        public void SleepingVampireIsOnlyACoffinUntilItWakes()
        {
            var view = UnitView.Create(Art, Art.vampireLook, null, 0, new Vector3(0f, -500f, 0f), 180f, Color.gray, true, inCoffin: true);
            try
            {
                Assert.IsTrue(view.Lying);
                Assert.IsNull(view.GetComponentInChildren<Animator>(), "no vampire model before it wakes");
                Assert.IsNotNull(view.transform.Find("Coffin"));
                view.StandUp();
                Assert.IsFalse(view.Lying);
                Assert.IsNotNull(view.GetComponentInChildren<Animator>(), "model built when the coffin opens");
                Assert.IsNull(view.transform.Find("Coffin"));
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }

        [Test]
        public void AlliesFaceNorthAndEnemiesFaceSouthWhenPlaced()
        {
            var go = new GameObject("UnitLookTests");
            try
            {
                var board = go.AddComponent<BoardView>();
                board.art = Art;
                var view = go.AddComponent<BattleView>();
                view.board = board;
                var stage = GameData.Stages[0];
                var battle = new Battle(stage, null, stage.Sample);
                view.Begin(battle);
                Assert.IsTrue(battle.Units.Any(u => u.Side == Side.Ally), "sample formation placed");
                var north = board.transform.forward;
                foreach (var u in battle.Units.Where(u => !u.Asleep))
                {
                    var model = view.ViewOf(u.Id).GetComponentInChildren<Animator>().transform;
                    var facing = Vector3.Dot(model.forward, north);
                    if (u.Side == Side.Ally) Assert.Greater(facing, 0.99f, u.Name.Key + " faces north");
                    else Assert.Less(facing, -0.99f, u.Name.Key + " faces south");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                foreach (var stray in Object.FindObjectsByType<UnitView>(FindObjectsSortMode.None)) Object.DestroyImmediate(stray.gameObject);
            }
        }
    }
}
