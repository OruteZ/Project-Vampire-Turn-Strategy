using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    public class HexParityTests
    {
        [Serializable] class CellRow { public int c, r, dist00; public bool walkable, lit; public string[] nbrs; }
        [Serializable] class PathRow { public string to; public string[] path; }
        [Serializable] class UnitRow { public string side, kind; public int x, y, mov; public string[] reach; public PathRow[] paths; }
        [Serializable] class Scenario { public UnitRow[] units; public string[] corpses, fieldEnemy, fieldAlly; }
        [Serializable] class StageHex { public string id; public int cols, rows; public CellRow[] cells; public Scenario @base, corpse; }
        [Serializable] class Golden { public StageHex[] stages; }

        static Golden golden;

        static Golden Load()
        {
            if (golden != null) return golden;
            var file = Path.Combine(Directory.GetCurrentDirectory(), "tools", "golden", "out", "hex.json");
            golden = JsonUtility.FromJson<Golden>("{\"stages\":" + File.ReadAllText(file) + "}");
            return golden;
        }

        static IEnumerable<int> StageIndexes() => Enumerable.Range(0, GameData.Stages.Length);

        static Hex ParseHex(string s)
        {
            var p = s.Split(',');
            return new Hex(int.Parse(p[0]), int.Parse(p[1]));
        }

        static string Step(Hex h, int d) => h + ":" + d;

        [Test]
        public void GoldenCoversEveryStage()
        {
            Assert.AreEqual(GameData.Stages.Select(s => s.Id).ToArray(), Load().stages.Select(s => s.id).ToArray());
        }

        [TestCaseSource(nameof(StageIndexes))]
        public void CellsMatch(int i)
        {
            var g = Load().stages[i];
            var board = new Board(GameData.Stages[i].Map);
            Assert.AreEqual(new[] { g.cols, g.rows }, new[] { board.Cols, board.Rows }, g.id + " size");
            foreach (var cell in g.cells)
            {
                var h = new Hex(cell.c, cell.r);
                var at = g.id + " cell " + h + " ";
                Assert.AreEqual(cell.walkable, board.Walkable(h), at + "walkable");
                Assert.AreEqual(cell.lit, board.Lit(h), at + "lit");
                Assert.AreEqual(cell.nbrs, board.Neighbors(h).Select(n => n.ToString()).ToArray(), at + "neighbors");
                Assert.AreEqual(cell.dist00, Hex.Distance(h, new Hex(0, 0)), at + "distance to 0,0");
            }
        }

        [TestCaseSource(nameof(StageIndexes))]
        public void BaseScenarioMatches(int i) => ScenarioMatches(i, Load().stages[i].@base, "base");

        [TestCaseSource(nameof(StageIndexes))]
        public void CorpseScenarioMatches(int i) => ScenarioMatches(i, Load().stages[i].corpse, "corpse");

        static void ScenarioMatches(int i, Scenario g, string name)
        {
            var stageId = Load().stages[i].id;
            var board = new Board(GameData.Stages[i].Map);
            var occupied = new Dictionary<Hex, Occupant>();
            foreach (var u in g.units) occupied[new Hex(u.x, u.y)] = u.side == "ally" ? Occupant.Ally : Occupant.Enemy;
            foreach (var c in g.corpses) occupied[ParseHex(c)] = Occupant.Corpse;
            Occupant At(Hex h) => occupied.TryGetValue(h, out var o) ? o : Occupant.Empty;

            foreach (var u in g.units)
            {
                var from = new Hex(u.x, u.y);
                var side = u.side == "ally" ? Side.Ally : Side.Enemy;
                var at = stageId + " " + name + " " + u.kind + "@" + from + " ";
                Assert.AreEqual(u.reach, board.Reachable(from, u.mov, side, At).Select(r => Step(r.hex, r.steps)).ToArray(), at + "reachable (order matters)");
                foreach (var p in u.paths)
                    Assert.AreEqual(p.path, board.PathTo(from, ParseHex(p.to), side, At).Select(h => h.ToString()).ToArray(), at + "path to " + p.to);
            }

            Assert.AreEqual(g.fieldEnemy, Field(board, g, Side.Enemy, "ally", At), stageId + " " + name + " enemy distance field");
            Assert.AreEqual(g.fieldAlly, Field(board, g, Side.Ally, "enemy", At), stageId + " " + name + " ally distance field");
        }

        static string[] Field(Board board, Scenario g, Side side, string around, Func<Hex, Occupant> at)
        {
            var goals = new List<Hex>();
            foreach (var u in g.units.Where(u => u.side == around))
                foreach (var n in board.Neighbors(new Hex(u.x, u.y)))
                    if (!board.Blocked(n, side, at) && at(n) == Occupant.Empty) goals.Add(n);
            return board.DistField(goals, side, at)
                .Select(kv => (key: kv.Key.ToString(), d: kv.Value))
                .OrderBy(kv => kv.key, StringComparer.Ordinal)
                .Select(kv => kv.key + ":" + kv.d)
                .ToArray();
        }
    }
}
