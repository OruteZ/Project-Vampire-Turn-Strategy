using System;

namespace VampireSpawn.Core
{
    public readonly struct Hex : IEquatable<Hex>
    {
        public readonly int Col, Row;

        public Hex(int col, int row)
        {
            Col = col; Row = row;
        }

        // Flat-top, odd-q offset. Order matches the prototype's NB table because BFS tie-breaks depend on it.
        static readonly (int dc, int dr)[][] Offsets =
        {
            new[] { (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (0, 1) },
            new[] { (1, 1), (1, 0), (0, -1), (-1, 0), (-1, 1), (0, 1) },
        };

        public Hex[] AllNeighbors()
        {
            var o = Offsets[Col & 1];
            var result = new Hex[6];
            for (var i = 0; i < 6; i++) result[i] = new Hex(Col + o[i].dc, Row + o[i].dr);
            return result;
        }

        public static int Distance(Hex a, Hex b)
        {
            int q1 = a.Col, r1 = a.Row - (a.Col - (a.Col & 1)) / 2;
            int q2 = b.Col, r2 = b.Row - (b.Col - (b.Col & 1)) / 2;
            int dq = q1 - q2, dr = r1 - r2;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }

        public bool Equals(Hex other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is Hex h && Equals(h);
        public override int GetHashCode() => Col * 397 ^ Row;
        public static bool operator ==(Hex a, Hex b) => a.Equals(b);
        public static bool operator !=(Hex a, Hex b) => !a.Equals(b);
        public override string ToString() => Col + "," + Row;
    }
}
