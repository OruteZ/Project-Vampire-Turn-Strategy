using System;
using System.Collections.Generic;
using System.Linq;

namespace VampireSpawn.Core
{
    public enum Terrain { Plain, Rock, Torch, Water, Sun, Deploy, Rough, Void }
    public enum Side { Ally, Enemy }
    public enum Occupant { Empty, Ally, Enemy, Corpse }

    public sealed class Board
    {
        public readonly int Cols, Rows;
        readonly Terrain[,] terrain;

        public Board(string[] map)
        {
            Rows = map.Length;
            Cols = map[0].Length;
            terrain = new Terrain[Cols, Rows];
            for (var r = 0; r < Rows; r++)
                for (var c = 0; c < Cols; c++)
                    terrain[c, r] = Parse(map[r][c]);
        }

        static Terrain Parse(char ch) => ch switch
        {
            '.' => Terrain.Plain,
            '#' => Terrain.Rock,
            'T' => Terrain.Torch,
            '~' => Terrain.Water,
            'S' => Terrain.Sun,
            'd' => Terrain.Deploy,
            ',' => Terrain.Rough,
            '_' => Terrain.Void,
            _ => throw new ArgumentException("unknown map tile '" + ch + "'"),
        };

        // docs/systems/triggers.md 1-4: a door becomes plain ground when its trigger takes effect.
        public void Open(Hex h) => terrain[h.Col, h.Row] = Terrain.Plain;

        public bool InBounds(Hex h) => h.Col >= 0 && h.Col < Cols && h.Row >= 0 && h.Row < Rows;
        public Terrain At(Hex h) => terrain[h.Col, h.Row];
        public bool Walkable(Hex h) => At(h) != Terrain.Rock && At(h) != Terrain.Water && At(h) != Terrain.Void;

        public List<Hex> Neighbors(Hex h)
        {
            var result = new List<Hex>(6);
            foreach (var n in h.AllNeighbors())
                if (InBounds(n)) result.Add(n);
            return result;
        }

        public bool Lit(Hex h)
        {
            if (At(h) == Terrain.Sun || At(h) == Terrain.Torch) return true;
            foreach (var n in Neighbors(h))
                if (At(n) == Terrain.Torch) return true;
            return false;
        }

        public bool Blocked(Hex h, Side side, Func<Hex, Occupant> occupant)
        {
            if (!Walkable(h)) return true;
            var o = occupant(h);
            return o == Occupant.Corpse
                || (o == Occupant.Ally && side != Side.Ally)
                || (o == Occupant.Enemy && side != Side.Enemy);
        }

        public int Cost(Hex h) => At(h) == Terrain.Rough ? 2 : 1;

        public List<(Hex hex, int steps)> Reachable(Hex from, int mov, Side side, Func<Hex, Occupant> occupant) =>
            Flood(new[] { from }, mov, side, occupant, false, null)
                .Where(r => r.hex == from || occupant(r.hex) == Occupant.Empty).ToList();

        // An unreachable goal returns just the goal.
        public List<Hex> PathTo(Hex from, Hex to, Side side, Func<Hex, Occupant> occupant)
        {
            var prev = new Dictionary<Hex, Hex>();
            foreach (var (h, _) in Flood(new[] { from }, int.MaxValue, side, occupant, false, prev))
                if (h == to) break;
            if (!prev.ContainsKey(to)) return new List<Hex> { to };
            var path = new List<Hex>();
            for (var k = to; k != from; k = prev[k]) path.Insert(0, k);
            return path;
        }

        // Cost of walking from each hex to the nearest source.
        public Dictionary<Hex, int> DistField(IEnumerable<Hex> sources, Side side, Func<Hex, Occupant> occupant) =>
            Flood(sources, int.MaxValue, side, occupant, true, null).ToDictionary(r => r.hex, r => r.steps);

        // Costs are 1 or 2, so FIFO buckets by cost (Dial) keep the old BFS tie order when there is no rough ground.
        IEnumerable<(Hex hex, int steps)> Flood(IEnumerable<Hex> starts, int limit, Side side, Func<Hex, Occupant> occupant, bool toStarts, Dictionary<Hex, Hex> prev)
        {
            var dist = new Dictionary<Hex, int>();
            var buckets = new List<Queue<Hex>>();
            void Push(Hex h, int d)
            {
                while (buckets.Count <= d) buckets.Add(new Queue<Hex>());
                buckets[d].Enqueue(h);
            }
            foreach (var s in starts)
            {
                if (dist.ContainsKey(s)) continue;
                dist[s] = 0;
                if (prev != null) prev[s] = s;
                Push(s, 0);
            }
            for (var d = 0; d < buckets.Count; d++)
                while (buckets[d].Count > 0)
                {
                    var h = buckets[d].Dequeue();
                    if (dist[h] != d) continue;
                    yield return (h, d);
                    foreach (var n in Neighbors(h))
                    {
                        if (Blocked(n, side, occupant)) continue;
                        var nd = d + Cost(toStarts ? h : n);
                        if (nd > limit || (dist.TryGetValue(n, out var old) && old <= nd)) continue;
                        dist[n] = nd;
                        if (prev != null) prev[n] = h;
                        Push(n, nd);
                    }
                }
        }
    }
}
