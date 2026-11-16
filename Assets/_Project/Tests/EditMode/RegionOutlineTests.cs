using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // docs/art/battle-terrain.md 3-2.
    public class RegionOutlineTests
    {
        static List<List<Vector3>> Trace(IEnumerable<Hex> cells)
        {
            var loops = new List<List<Vector3>>();
            RegionOutline.Trace(cells, 1f, loops);
            return loops;
        }

        [Test]
        public void OneCellIsSixEdges() => Assert.AreEqual(6, Trace(new[] { new Hex(3, 3) }).Single().Count);

        [Test]
        public void TorchAndNeighborsIsOneLoopOfEighteen()
        {
            var t = new Hex(3, 3);
            var loops = Trace(t.AllNeighbors().Append(t));
            Assert.AreEqual(1, loops.Count);
            Assert.AreEqual(18, loops[0].Count);
        }

        [Test]
        public void ApartCellsAreSeparateLoops() => Assert.AreEqual(2, Trace(new[] { new Hex(0, 0), new Hex(4, 4) }).Count);

        [Test]
        public void RingKeepsTheHole()
        {
            var loops = Trace(new Hex(3, 3).AllNeighbors());
            Assert.AreEqual(2, loops.Count);
            CollectionAssert.AreEquivalent(new[] { 6, 18 }, loops.Select(l => l.Count));
        }
    }
}
