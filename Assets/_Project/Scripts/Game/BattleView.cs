using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    public sealed class BattleView : MonoBehaviour
    {
        public BoardView board;
        [Tooltip("Playback speed multiplier; 1 uses the prototype's delays.")]
        public float speed = 1f;

        // Delays copied from the prototype's wait() calls (ms): one walk step 120, strike 380, rise batch 600, coffin opens 700, phase banner 500.
        const float StepSeconds = 0.12f, StrikeSeconds = 0.38f, RiseSeconds = 0.6f, WakeSeconds = 0.7f, BannerSeconds = 0.5f;

        public Battle Battle { get; private set; }
        public event Action<BattleEvent> Played;
        public bool IsBusy => playing || pending.Count > 0;

        readonly Dictionary<int, UnitView> units = new Dictionary<int, UnitView>();
        readonly Dictionary<int, UnitView> corpses = new Dictionary<int, UnitView>();
        readonly Queue<IReadOnlyList<BattleEvent>> pending = new Queue<IReadOnlyList<BattleEvent>>();
        Transform layer;
        bool playing;

        ArtCatalog Art => board.art;

        public void Begin(Battle battle)
        {
            StopAllCoroutines();
            pending.Clear();
            playing = false;
            Battle = battle;
            board.Show(battle.Stage);
            if (layer != null) Destroy(layer.gameObject);
            layer = new GameObject("Units").transform;
            layer.SetParent(transform, false);
            units.Clear();
            corpses.Clear();
            foreach (var u in battle.Units) AddUnit(u);
            foreach (var c in battle.Corpses) AddCorpse(c.Id, c.Type, c.Pos);
        }

        public void Clear()
        {
            StopAllCoroutines();
            pending.Clear();
            playing = false;
            Battle = null;
            if (layer != null) Destroy(layer.gameObject);
            layer = null;
            units.Clear();
            corpses.Clear();
            board.Clear();
        }

        public CommandResult Run(Func<Battle, CommandResult> command)
        {
            var result = command(Battle);
            if (!result.Ok) return result;
            pending.Enqueue(result.Events);
            if (Battle.Phase == Phase.Deploy) SyncDeploy();
            if (!playing) StartCoroutine(Drain());
            return result;
        }

        public UnitView ViewOf(int id) => units.TryGetValue(id, out var v) ? v : null;

        public void ShowDone()
        {
            foreach (var kv in units)
            {
                var u = Battle.Find(kv.Key);
                kv.Value.SetDone(u != null && u.Side == Side.Ally && Battle.Phase == Phase.Player && u.Acted);
            }
        }

        public void SyncDeploy()
        {
            foreach (var id in new List<int>(units.Keys))
                if (Battle.Find(id) == null) Remove(units, id, 0f);
            foreach (var u in Battle.Units)
            {
                if (!units.ContainsKey(u.Id)) AddUnit(u);
                units[u.Id].transform.position = Ground(u.Pos);
            }
        }

        IEnumerator Drain()
        {
            playing = true;
            while (pending.Count > 0)
                foreach (var e in pending.Dequeue())
                    yield return Play(e);
            playing = false;
        }

        float Wait(float seconds) => speed > 0f ? seconds / speed : 0f;

        IEnumerator Play(BattleEvent e)
        {
            Played?.Invoke(e);
            switch (e.Kind)
            {
                case BattleEventKind.Walk:
                    if (!units.TryGetValue(e.UnitId, out var walker)) break;
                    walker.SetWalking(true);
                    foreach (var h in e.Path)
                    {
                        var from = walker.transform.position;
                        var to = Ground(h);
                        walker.Face(to);
                        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.0001f, Wait(StepSeconds)))
                        {
                            walker.transform.position = Vector3.Lerp(from, to, t);
                            yield return null;
                        }
                        walker.transform.position = to;
                    }
                    walker.SetWalking(false);
                    yield return Delay(StepSeconds);
                    break;
                case BattleEventKind.Hit:
                    units.TryGetValue(e.UnitId, out var target);
                    if (units.TryGetValue(e.OtherId, out var attacker) && target != null && attacker != target)
                    {
                        attacker.Face(target.transform.position);
                        attacker.Trigger("Attack");
                    }
                    if (target != null) target.Trigger("Hit");
                    yield return Delay(StrikeSeconds);
                    break;
                case BattleEventKind.HpChanged:
                    if (units.TryGetValue(e.UnitId, out var hurt)) hurt.SetHp(e.Value, Battle.Find(e.UnitId)?.MaxHp ?? MaxHpOf(hurt, e.Value));
                    break;
                case BattleEventKind.Popup:
                    if (units.TryGetValue(e.UnitId, out var shown)) Popup.Spawn(shown.transform.position + Vector3.up * (Art.unitHeight * Art.hexSize + 0.6f), Loc.Text(e.Text), e.Style, speed);
                    break;
                case BattleEventKind.Died:
                    if (units.TryGetValue(e.UnitId, out var dead)) { dead.Trigger("Death"); Remove(units, e.UnitId, Wait(1.2f)); }
                    break;
                case BattleEventKind.CorpseLeft:
                    var corpse = CorpseType(e.UnitId);
                    if (corpse.HasValue) AddCorpse(e.UnitId, corpse.Value, e.At);
                    break;
                case BattleEventKind.Rose:
                    Remove(corpses, e.OtherId, 0f);
                    var risen = Battle.Find(e.UnitId);
                    if (risen != null) AddUnit(risen, e.At);
                    yield return Delay(RiseSeconds);
                    break;
                case BattleEventKind.Woke:
                    if (units.TryGetValue(e.UnitId, out var woke)) woke.StandUp();
                    yield return Delay(WakeSeconds);
                    break;
                case BattleEventKind.Banner:
                    yield return Delay(BannerSeconds);
                    break;
            }
        }

        static int MaxHpOf(UnitView v, int hp) => v.HpFraction > 0f ? Mathf.RoundToInt(hp / v.HpFraction) : hp;

        // A corpse that rises in the same command is gone from Battle.Corpses by the time playback reaches it; its origin is on the spawn.
        UnitKind? CorpseType(int corpseId)
        {
            foreach (var c in Battle.Corpses) if (c.Id == corpseId) return c.Type;
            return null;
        }

        IEnumerator Delay(float seconds)
        {
            var w = Wait(seconds);
            if (w > 0f) yield return new WaitForSeconds(w);
        }

        Vector3 Ground(Hex h) => board.HexToWorld(h) + Vector3.up * board.TileTop;

        void AddUnit(Unit u, Hex? at = null)
        {
            var look = u.Role switch
            {
                Role.Skeleton => Art.skeletonLook,
                Role.Vampire => Art.vampireLook,
                _ => Art.allyLooks[(int)u.Type.Value],
            };
            // docs/art/unit-visuals.md 4: allies face the enemy side (north, +Z), enemies face south.
            var yaw = u.Side == Side.Ally ? 0f : 180f;
            var view = UnitView.Create(Art, look, layer, u.Id, Ground(at ?? u.Pos), yaw, RingColor(u), u.Role != Role.Spawn, u.Asleep);
            view.SetHp(u.Hp, u.MaxHp);
            units[u.Id] = view;
        }

        void AddCorpse(int id, UnitKind type, Hex at)
        {
            var view = UnitView.Create(Art, Art.allyLooks[(int)type], layer, -id, Ground(at), 20f, Art.spawnRing * 0.6f, false);
            view.name = "Corpse " + id;
            view.LieDown();
            corpses[id] = view;
        }

        Color RingColor(Unit u)
        {
            switch (u.Role)
            {
                case Role.Skeleton: return Art.skeletonRing;
                case Role.Vampire: return Art.vampireRing;
                case Role.Spawn: return Art.spawnRing;
            }
            return GameData.Type(u.Type.Value).Tier switch
            {
                Tier.Peasant => Art.peasantRing,
                Tier.Knight => Art.knightRing,
                Tier.Hunter => Art.hunterRing,
                _ => Art.regularRing,
            };
        }

        static void Remove(Dictionary<int, UnitView> from, int id, float after)
        {
            if (!from.TryGetValue(id, out var v)) return;
            from.Remove(id);
            v.transform.SetParent(null, true);
            Destroy(v.gameObject, after);
        }

        public List<string> Mismatches()
        {
            var problems = new List<string>();
            if (IsBusy) { problems.Add("still playing"); return problems; }
            foreach (var u in Battle.Units)
            {
                if (!units.TryGetValue(u.Id, out var v)) { problems.Add($"unit {u.Id} {u.Name.Key} has no view"); continue; }
                if (Vector3.Distance(v.transform.position, Ground(u.Pos)) > 0.05f) problems.Add($"unit {u.Id} {u.Name.Key} drawn away from {u.Pos}");
                if (Mathf.Abs(v.HpFraction - Mathf.Clamp01((float)u.Hp / u.MaxHp)) > 0.001f) problems.Add($"unit {u.Id} {u.Name.Key} hp bar {v.HpFraction:F2} vs {u.Hp}/{u.MaxHp}");
                if (v.Lying != u.Asleep) problems.Add($"unit {u.Id} {u.Name.Key} lying={v.Lying} asleep={u.Asleep}");
            }
            foreach (var id in units.Keys) if (Battle.Find(id) == null) problems.Add($"view {id} has no unit");
            foreach (var c in Battle.Corpses)
            {
                if (!corpses.TryGetValue(c.Id, out var v)) { problems.Add($"corpse {c.Id} {c.Name.Key} has no view"); continue; }
                if (Vector3.Distance(v.transform.position, Ground(c.Pos)) > 0.05f) problems.Add($"corpse {c.Id} drawn away from {c.Pos}");
            }
            foreach (var id in corpses.Keys)
            {
                var found = false;
                foreach (var c in Battle.Corpses) if (c.Id == id) found = true;
                if (!found) problems.Add($"corpse view {id} has no corpse");
            }
            return problems;
        }
    }
}
