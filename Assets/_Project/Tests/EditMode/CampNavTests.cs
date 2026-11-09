using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    // docs/ui/stage-select.md 5, docs/ui/upgrade.md 4·6.
    public class CampNavTests
    {
        static Stage[] Stages => Campaign.Stages;
        static int Last => Stages.Length - 1;

        static Campaign ClearedUpTo(int n)
        {
            var c = new Campaign();
            for (var i = 0; i < n; i++) c.Finish(Stages[i], true, false);
            return c;
        }

        static CampNav Press(CampNav s, Campaign c, CampKey key, float now = 0f) => s.Step(c, CampInput.Of(key, now)).next;

        [Test]
        public void FromMenuFocusesTheFirstOpenUnclearedStageOnTheMap()
        {
            var c = ClearedUpTo(2);
            var s = CampNav.FromMenu(c, null);
            Assert.AreEqual(CampLayer.Map, s.Layer);
            Assert.AreEqual(2, s.Stage);
            Assert.AreEqual(Last, CampNav.FromMenu(ClearedUpTo(Stages.Length), null).Stage, "all cleared: last stage");
        }

        [Test]
        public void FromMenuKeepsTheStagePickedEarlierThisRun()
        {
            var c = ClearedUpTo(6);
            var picked = CampNav.FromMenu(c, null).Step(c, CampInput.Pick(5, 0f)).next;
            var back = CampNav.FromMenu(c, picked);
            Assert.AreEqual(5, back.Stage);
            Assert.AreEqual(CampLayer.Map, back.Layer);
        }

        [Test]
        public void UpWalksNorthToTheNextStageAndStopsAtTheEnds()
        {
            var c = ClearedUpTo(0);
            var s = CampNav.FromMenu(c, null);
            Assert.AreEqual(0, s.Stage);
            Assert.AreEqual(CampDo.None, s.Step(c, CampInput.Of(CampKey.Down, 0f)).act, "stops at the first stage");
            for (var i = 0; i < Stages.Length + 3; i++) s = Press(s, c, CampKey.Up);
            Assert.AreEqual(Last, s.Stage, "stops at the last stage");
            Assert.AreEqual(Last - 1, Press(s, c, CampKey.Down).Stage);
        }

        [Test]
        public void EnterOpensInfoThenMarches()
        {
            var c = ClearedUpTo(1);
            var s = Press(CampNav.FromMenu(c, null), c, CampKey.Enter);
            Assert.AreEqual(CampLayer.Info, s.Layer);
            Assert.AreEqual(CampDo.March, s.Step(c, CampInput.Of(CampKey.Enter, 0f)).act);
        }

        // #48: a stage that cannot be played never opens Info, whichever way the player tries.
        [Test]
        public void ALockedStageNeverOpensInfo()
        {
            var c = ClearedUpTo(1);
            var map = CampNav.FromMenu(c, null);
            Assert.AreEqual(CampDo.None, map.Step(c, CampInput.Pick(5, 0f)).act, "clicking a locked marker");
            var onLocked = Press(map, c, CampKey.Up);
            Assert.AreEqual(2, onLocked.Stage, "the map focus may rest on a locked stage");
            Assert.AreEqual(CampLayer.Map, Press(onLocked, c, CampKey.Enter).Layer, "Enter on a locked focus");
            var info = map.Step(c, CampInput.Pick(1, 0f)).next;
            Assert.AreEqual(CampLayer.Info, info.Layer);
            Assert.AreEqual(1, Press(info, c, CampKey.Up).Stage, "up in Info stops before a locked stage");
        }

        [Test]
        public void EscStepsBackOneLayerAndLeavesFromTheMap()
        {
            var c = ClearedUpTo(3);
            var info = CampNav.FromMenu(c, null).Step(c, CampInput.Pick(3, 0f)).next;
            var barracks = Press(info, c, CampKey.Barracks);
            Assert.AreEqual(CampLayer.Barracks, barracks.Layer);
            var back = Press(barracks, c, CampKey.Esc);
            Assert.AreEqual(CampLayer.Info, back.Layer, "barracks returns to the layer it covered");
            Assert.AreEqual(3, back.Stage);
            var map = Press(back, c, CampKey.Esc);
            Assert.AreEqual(CampLayer.Map, map.Layer);
            Assert.AreEqual(3, map.Stage, "the map keeps focus on that stage");
            Assert.AreEqual(CampDo.Menu, map.Step(c, CampInput.Of(CampKey.Esc, 0f)).act);
        }

        [Test]
        public void EmptyMapClosesInfoButNotBarracks()
        {
            var c = ClearedUpTo(2);
            var info = CampNav.FromMenu(c, null).Step(c, CampInput.Pick(2, 0f)).next;
            Assert.AreEqual(CampLayer.Map, Press(info, c, CampKey.EmptyMap).Layer);
            var barracks = Press(info, c, CampKey.Barracks);
            Assert.AreEqual(CampLayer.Barracks, Press(barracks, c, CampKey.EmptyMap).Layer);
            Assert.AreEqual(CampDo.None, barracks.Step(c, CampInput.Pick(4, 0f)).act, "markers are hidden in the barracks");
        }

        [Test]
        public void AFirstClearShowsTheNewlyOpenedStageOnTheMap()
        {
            var c = ClearedUpTo(3);
            var s = CampNav.FromBattle(c, 2, firstClear: true, tab: -1);
            Assert.AreEqual(CampLayer.Map, s.Layer);
            Assert.AreEqual(3, s.Stage);
        }

        [Test]
        public void ALossOrReplayReopensThatStagesInfo()
        {
            var c = ClearedUpTo(3);
            var lost = CampNav.FromBattle(c, 3, firstClear: false, tab: -1);
            Assert.AreEqual(CampLayer.Info, lost.Layer);
            Assert.AreEqual(3, lost.Stage);
            var last = CampNav.FromBattle(ClearedUpTo(Stages.Length), Last, firstClear: true, tab: -1);
            Assert.AreEqual(CampLayer.Info, last.Layer, "clearing the last stage opens nothing new");
        }

        [Test]
        public void ResetNeedsASecondPressWithinTheWindow()
        {
            var c = ClearedUpTo(2);
            var s = CampNav.FromMenu(c, null);
            var (armed, a1) = s.Step(c, CampInput.Of(CampKey.Reset, 10f));
            Assert.IsTrue(armed.ResetArmed);
            Assert.AreEqual(CampDo.Redraw, a1);
            Assert.AreEqual(CampDo.Reset, armed.Step(c, CampInput.Of(CampKey.Reset, 11f)).act);

            var (expired, a2) = armed.Step(c, CampInput.Of(CampKey.Tick, 10f + CampNav.ConfirmSeconds + 0.1f));
            Assert.IsFalse(expired.ResetArmed, "2.5 s passing disarms");
            Assert.AreEqual(CampDo.Redraw, a2);
            Assert.AreEqual(CampDo.None, armed.Step(c, CampInput.Of(CampKey.Tick, 11f)).act, "still armed inside the window");

            var moved = Press(armed, c, CampKey.Up, 11f);
            Assert.IsFalse(moved.ResetArmed, "any other input disarms");
            Assert.AreNotEqual(CampDo.Reset, moved.Step(c, CampInput.Of(CampKey.Reset, 11.5f)).act);

            var info = Press(s, c, CampKey.Enter);
            Assert.AreEqual(CampDo.None, info.Step(c, CampInput.Of(CampKey.Reset, 0f)).act, "reset lives on the map layer only");
        }

        [Test]
        public void BuyingAsksInADialogThatWaitsForAnAnswer()
        {
            var c = ClearedUpTo(Stages.Length);
            var s = Press(CampNav.FromMenu(c, null), c, CampKey.Barracks);
            var id = GameData.Upgrades[0].Id;
            var asking = s.Step(c, CampInput.Buy(id, 0f)).next;
            Assert.AreEqual(id, asking.BuyArmed);
            Assert.AreEqual(CampDo.None, asking.Step(c, CampInput.Of(CampKey.Tick, 60f)).act, "the dialog does not time out");
            Assert.AreEqual(CampDo.Buy, asking.Step(c, CampInput.Of(CampKey.Enter, 1f)).act, "Enter buys");
            Assert.AreEqual(CampDo.Buy, asking.Step(c, CampInput.Buy(id, 1f)).act, "the dialog's Buy button buys");
            var cancelled = asking.Step(c, CampInput.Of(CampKey.Esc, 1f));
            Assert.AreEqual(CampDo.Redraw, cancelled.act);
            Assert.IsNull(cancelled.next.BuyArmed);
            Assert.AreEqual(CampLayer.Barracks, cancelled.next.Layer, "Esc closes only the dialog, not the barracks");
        }

        [Test]
        public void TabsSkipStagesNotYetUnlockedAndStopAtTheEnds()
        {
            var c = ClearedUpTo(0);
            var kinds = GameData.Types.Select(t => t.Key).ToArray();
            var hidden = Enumerable.Range(0, kinds.Length).Where(i => CampNav.TabOf(c, kinds[i]) == TabState.Hidden).ToArray();
            Assume.That(hidden, Is.Not.Empty, "a fresh campaign has units not yet unlocked");
            var s = Press(CampNav.FromMenu(c, null), c, CampKey.Barracks);
            Assert.AreNotEqual(TabState.Hidden, CampNav.TabOf(c, kinds[s.Tab]), "opens on a tab that can be seen");
            Assert.AreEqual(CampDo.None, s.Step(c, CampInput.Tab(hidden[0], 0f)).act, "a hidden tab cannot be picked");
            for (var i = 0; i < kinds.Length + 2; i++) s = Press(s, c, CampKey.Right);
            Assert.AreNotEqual(TabState.Hidden, CampNav.TabOf(c, kinds[s.Tab]), "right never lands on a hidden tab");
            Assert.AreEqual(CampDo.None, s.Step(c, CampInput.Of(CampKey.Right, 0f)).act, "stops at the end");
        }

        [Test]
        public void TheBarracksOpensOnTheFirstTabWithSomethingToBuy()
        {
            var c = ClearedUpTo(Stages.Length);
            var kinds = GameData.Types.Select(t => t.Key).ToArray();
            var first = System.Array.FindIndex(kinds, k => CampNav.Affordable(c, k) > 0);
            Assume.That(first, Is.GreaterThanOrEqualTo(0), "the full campaign has gold for something");
            Assert.AreEqual(first, CampNav.DefaultTab(c, -1));
            Assert.AreEqual(kinds.Length - 1, CampNav.DefaultTab(c, kinds.Length - 1), "the tab picked last this run wins");
        }
    }
}
