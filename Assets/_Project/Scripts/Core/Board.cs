using System;
using System.Collections.Generic;

namespace VampireSpawn.Core
{
    public enum Terrain { Plain, Rock, Torch, Water, Sun, Deploy }
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
            _ => throw new ArgumentException("unknown map tile '" + ch + "'"),
        };

        public bool InBounds(Hex h) => h.Col >= 0 && h.Col < Cols && h.Row >= 0 && h.Row < Rows;
        public Terrain At(Hex h) => terrain[h.Col, h.Row];
        public bool Walkable(Hex h) => At(h) != Terrain.Rock && At(h) != Terrain.Water;

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

        public List<(Hex hex, int steps)> Reachable(Hex from, int mov, Side side, Func<Hex, Occupant> occupant)
        {
            var result = new List<(Hex, int)>();
            var seen = new HashSet<Hex> { from };
            var queue = new Queue<(Hex hex, int steps)>();
            queue.Enqueue((from, 0));
            while (queue.Count > 0)
            {
                var (h, d) = queue.Dequeue();
                if (h == from || occupant(h) == Occupant.Empty) result.Add((h, d));
                if (d >= mov) continue;
                foreach (var n in Neighbors(h))
                {
                    if (seen.Contains(n) || Blocked(n, side, occupant)) continue;
                    seen.Add(n);
                    queue.Enqueue((n, d + 1));
                }
            }
            return result;
        }

        // An unreachable goal returns just the goal.
        public List<Hex> PathTo(Hex from, Hex to, Side side, Func<Hex, Occupant> occupant)
        {
            var prev = new Dictionary<Hex, Hex> { [from] = from };
            var queue = new Queue<Hex>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var h = queue.Dequeue();
                if (h == to) break;
                foreach (var n in Neighbors(h))
                {
                    if (prev.ContainsKey(n) || Blocked(n, side, occupant)) continue;
                    prev[n] = h;
                    queue.Enqueue(n);
                }
            }
            if (!prev.ContainsKey(to)) return new List<Hex> { to };
            var path = new List<Hex>();
            for (var k = to; k != from; k = prev[k]) path.Insert(0, k);
            return path;
        }

        public Dictionary<Hex, int> DistField(IEnumerable<Hex> sources, Side side, Func<Hex, Occupant> occupant)
        {
            var field = new Dictionary<Hex, int>();
            var queue = new Queue<Hex>();
            foreach (var s in sources)
            {
                if (field.ContainsKey(s)) continue;
                field[s] = 0;
                queue.Enqueue(s);
            }
            while (queue.Count > 0)
            {
                var h = queue.Dequeue();
                foreach (var n in Neighbors(h))
                {
                    if (field.ContainsKey(n) || Blocked(n, side, occupant)) continue;
                    field[n] = field[h] + 1;
                    queue.Enqueue(n);
                }
            }
            return field;
        }
    }
}
