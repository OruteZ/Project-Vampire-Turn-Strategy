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
            var view = UnitView.Create(Art, look, null, 0, new Vector3(0f, -500f, 0f), 0f, true);
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
                    // Any melee hold, a new weapon left on AsIs included: the long end must not trail behind the unit.
                    // A bow is symmetric and a levelled crossbow's butt points back, so shooters are checked in the attack pose instead.
                    if (!Shoots(look)) Assert.Greater(Vector3.Dot(LongEnd(w), Vector3.forward), -0.3f, name + " " + g.prefab.name + " (" + g.hold + ") trails behind, a reverse grip");
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

        static IEnumerable<string> PolygonArmed() => All.Where(l => l.rig == Rig.Polygon && l.weapons != null && l.weapons.Any(g => g.prefab != null)).Select(l => l.name);

        // Polygon hand bones sit at the wrist; UnitView moves the grip toward the knuckles (Finger_01). A model without that bone would hold by the wrist again.
        [TestCaseSource(nameof(PolygonArmed))]
        public void PolygonWeaponsSitInThePalmNotTheWrist(string name)
        {
            var look = Look(name);
            var view = UnitView.Create(Art, look, null, 0, new Vector3(0f, -500f, 0f), 0f, true);
            try
            {
                var bones = view.GetComponentsInChildren<Transform>(true);
                foreach (var g in look.weapons.Where(g => g.prefab != null))
                {
                    var hand = bones.First(b => b.name == UnitLook.Bone(look.rig, g.hand));
                    Assert.Greater(UnitView.Palm(look.rig, hand).sqrMagnitude, 0f, name + " " + hand.name + " has no Finger_01 to find the palm by");
                }
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }

        static bool Shoots(UnitLook look) => look.animator != null && look.animator.name.Contains("Bow");

        static IEnumerable<string> Shooters() => All.Where(Shoots).Select(l => l.name);

        // docs/art/unit-visuals.md 3-1: at the shot the bow or crossbow stands up straight on the drawn arm. Polygon hand bones need a grip rotation for this.
        [TestCaseSource(nameof(Shooters))]
        public void ShootersHoldTheirWeaponUprightOnTheBowArm(string name)
        {
            var look = Look(name);
            var view = UnitView.Create(Art, look, null, 0, new Vector3(0f, -500f, 0f), 0f, true);
            try
            {
                var animator = view.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Play("Attack", 0, 0.25f);
                animator.Update(0f);
                var g = look.weapons.First(x => x.prefab != null);
                var w = view.GetComponentsInChildren<Transform>(true).First(t => t.name == g.prefab.name + "(Clone)");
                Assert.AreEqual(Hand.Left, g.hand, name + " shoots with the left arm out");
                var arm = (w.parent.position - w.parent.parent.position).normalized;
                var thin = Thinnest(w);
                // Bow: the thin axis is across the arm (limbs stand in the arm's vertical plane). Crossbow: the thin axis points up (limbs level).
                var up = Mathf.Abs(Vector3.Dot(thin, Vector3.up));
                var across = Mathf.Abs(Vector3.Dot(thin, arm));
                Assert.IsTrue(up > 0.8f || across < 0.6f && Mathf.Abs(Vector3.Dot(Longest(w), Vector3.up)) > 0.7f,
                    name + " " + g.prefab.name + " tilted at the shot: thin " + thin + ", arm " + arm);
                // The mesh's bulk sits on the body side of the grip when the bow's belly faces the target (a flipped bow points it forward), and when a crossbow's stock runs back along the arm.
                var bulk = (w.TransformPoint(MeshBounds(w).center) - w.parent.TransformPoint(UnitView.Palm(look.rig, w.parent))).normalized;
                Assert.Less(Vector3.Dot(bulk, arm), -0.5f, name + " " + g.prefab.name + " faces backwards at the shot");
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }

        static Bounds MeshBounds(Transform w)
        {
            var meshes = w.GetComponentsInChildren<MeshFilter>(true).Select(m => m.sharedMesh)
                .Concat(w.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s => s.sharedMesh)).Where(m => m != null).ToList();
            var b = meshes[0].bounds;
            foreach (var m in meshes) b.Encapsulate(m.bounds);
            return b;
        }

        static Vector3 Axis(Transform w, int i) { var v = Vector3.zero; v[i] = 1f; return w.TransformDirection(v).normalized; }

        static Vector3 Thinnest(Transform w) { var s = MeshBounds(w).size; return Axis(w, s.x <= s.y && s.x <= s.z ? 0 : s.y <= s.z ? 1 : 2); }

        static Vector3 Longest(Transform w) { var s = MeshBounds(w).size; return Axis(w, s.x >= s.y && s.x >= s.z ? 0 : s.y >= s.z ? 1 : 2); }

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

        // docs/art/unit-visuals.md 4-1: an unarmed corpse lies in the last frame of the death its armed unit fell with, not in the bind (T) pose.
        [TestCaseSource(nameof(Looks))]
        public void CorpseLiesInTheLastFrameOfItsUnitsDeath(string name)
        {
            var look = Look(name);
            var at = new Vector3(0f, -500f, 0f);
            var fell = UnitView.Create(Art, look, null, 0, at, 0f, true);
            var corpse = UnitView.Create(Art, look, null, 1, at, 0f, false);
            try
            {
                var dying = fell.GetComponentInChildren<Animator>();
                dying.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                dying.Play("Death", 0, 1f);
                dying.Update(0f);
                corpse.GetComponentInChildren<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
                corpse.LieDown();
                corpse.GetComponentInChildren<Animator>().Update(0f);
                var dead = Bones(fell);
                var lying = Bones(corpse);
                Assert.Less(lying.Values.Max(p => p.y), corpse.Height * 0.5f, name + " corpse lies on the ground");
                foreach (var b in lying.Where(b => dead.ContainsKey(b.Key)))
                    Assert.Less(Vector3.Distance(dead[b.Key], b.Value), 0.05f, name + " " + b.Key + " differs from the end of the fall");
            }
            finally { Object.DestroyImmediate(fell.gameObject); Object.DestroyImmediate(corpse.gameObject); }
        }

        // Sidekick ik_* helper bones are not animated and stay where the bind pose put them.
        static Dictionary<string, Vector3> Bones(UnitView view) => view.GetComponentInChildren<SkinnedMeshRenderer>().bones
            .Where(b => b != null && !b.name.StartsWith("ik_")).GroupBy(b => b.name).ToDictionary(g => g.Key, g => view.transform.InverseTransformPoint(g.First().position));

        [Test]
        public void SleepingVampireIsOnlyACoffinUntilItWakes()
        {
            var view = UnitView.Create(Art, Art.vampireLook, null, 0, new Vector3(0f, -500f, 0f), 180f, true, inCoffin: true);
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
        public void AlliesFaceNorthAndEnemiesFaceSouthWhenPlacedOnASideRing()
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
                // ui-style.md 8-1 8: a side ring under every standing unit, none under a coffin.
                foreach (var u in battle.Units)
                {
                    var ring = view.ViewOf(u.Id).transform.Find("Ring");
                    Assert.IsNotNull(ring, u.Name.Key + " ring");
                    Assert.AreEqual(!u.Asleep, ring.gameObject.activeSelf, u.Name.Key + " ring shown only when standing");
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
