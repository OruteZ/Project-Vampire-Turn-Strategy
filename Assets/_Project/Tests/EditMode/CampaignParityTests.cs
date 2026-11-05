using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    public class CampaignParityTests
    {
        [Serializable] class Step { public string op, save, open, upgradesOpen; public int tier, spent; public bool bonusMet; }
        [Serializable] class LoadCase { public string raw, loaded; }
        [Serializable] class BonusCase { public string stage; public int turn, drained; public bool skeletonsLeft, met; }
        [Serializable] class Golden { public Step[] steps; public LoadCase[] loads; public BonusCase[] bonus; }
        [Serializable] class Op { public string op, stage, id; public bool win, skeletonsLeft; public int turn, drained; }

        static Golden golden;

        static Golden Load()
        {
            if (golden != null) return golden;
            golden = JsonUtility.FromJson<Golden>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "tools", "golden", "out", "campaign.json")));
            return golden;
        }

        static Stage StageById(string id) => GameData.Stages.First(s => s.Id == id);

        [Test]
        public void EveryStepMatchesThePrototype()
        {
            var steps = Load().steps;
            Assert.That(steps.Length, Is.GreaterThan(100));
            Campaign c = null;
            for (var i = 0; i < steps.Length; i++)
            {
                var g = steps[i];
                var op = JsonUtility.FromJson<Op>(g.op);
                var at = "step " + i + " " + g.op + " ";
                switch (op.op)
                {
                    case "load": c = Campaign.Parse(null); break;
                    case "buy": c.Buy(op.id); break;
                    case "respec": c.Respec(); break;
                    case "reset": c = Campaign.Parse(null); break;
                    case "finish":
                        var stage = StageById(op.stage);
                        var met = stage.Bonus.IsMet(op.turn, op.drained, op.skeletonsLeft);
                        Assert.AreEqual(g.bonusMet, met, at + "bonus met");
                        c.Finish(stage, op.win, met);
                        break;
                    default: throw new ArgumentException(op.op);
                }
                Assert.AreEqual(g.save, c.ToJson(), at + "save");
                Assert.AreEqual(g.open, string.Concat(Campaign.Stages.Select((_, k) => c.StageOpen(k) ? "1" : "0")), at + "open stages");
                Assert.AreEqual(g.upgradesOpen, string.Join(",", GameData.Types.Where(t => c.UpgradesOpen(t.Kind)).Select(t => t.Key)), at + "upgrades open");
                Assert.AreEqual(g.tier, c.UnlockedTier, at + "unlocked tier");
                Assert.AreEqual(g.spent, c.SpentGold, at + "spent gold");
            }
        }

        [Test]
        public void LoadingMatchesThePrototype()
        {
            foreach (var l in Load().loads)
                Assert.AreEqual(l.loaded, Campaign.Parse(l.raw).ToJson(), "load of '" + l.raw + "'");
        }

        [Test]
        public void BonusConditionsMatchThePrototype()
        {
            foreach (var b in Load().bonus)
                Assert.AreEqual(b.met, StageById(b.stage).Bonus.IsMet(b.turn, b.drained, b.skeletonsLeft),
                    $"{b.stage} turn={b.turn} drained={b.drained} skeletonsLeft={b.skeletonsLeft}");
        }

        [Test]
        public void UpgradesReachBattlesExceptTheDemo()
        {
            var c = Campaign.Parse("{\"gold\":0,\"cleared\":{\"s1\":true,\"s2\":true,\"s3\":true},\"bonus\":{},\"ups\":{\"sword_def\":true}}");
            Assert.AreEqual(2, c.NewBattle(StageById("s3")).Allies.First(u => u.Kind == "sword").Def, "chain mail on in a campaign stage");
            Assert.AreEqual(1, c.NewBattle(StageById("demo")).Allies.First(u => u.Kind == "sword").Def, "demo ignores upgrades");
        }
    }
}
