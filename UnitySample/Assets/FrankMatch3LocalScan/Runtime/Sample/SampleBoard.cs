using System;
using System.Collections.Generic;
using Vector2Int = imady.FrankShenMatch3Hashing.Vector2Int;

namespace imady.FrankShenMatch3Hashing.Sample
{
    /// <summary>
    /// One local scan: which cell was scanned, from which direction, the 8-bit hash and the result.
    /// </summary>
    public class ScanRecord
    {
        public Vector2Int Landing;
        public Vector2Int Direction;
        public byte Hash;
        public MatchInfo Match; // null when the hash maps to MT_MatchPattern.None

        public MT_MatchPattern Pattern => Match?.Pattern ?? MT_MatchPattern.None;
    }

    /// <summary>
    /// A tile that moved: swapped, fell, or was spawned above the board (From.y &lt; 0).
    /// </summary>
    public struct TileMove
    {
        public Vector2Int From;
        public Vector2Int To;
        public TileMove(Vector2Int from, Vector2Int to) { From = from; To = to; }
    }

    /// <summary>
    /// Minimal match-3 board driven by the FrankMatch3LocalScan algorithm.
    /// Board layout follows the algorithm: cells[y, x], x = column, y = row, y grows downwards,
    /// so Vector2Int.down == (0, 1). 0 means an empty cell.
    ///
    /// The board never scans the whole grid for matches. Every scan is local:
    /// it runs at the cell a tile has just landed on, using the direction it came from.
    /// </summary>
    public class SampleBoard
    {
        public const ushort Empty = 0;

        public readonly int Width;
        public readonly int Height;
        public readonly int ColorCount;
        public readonly ushort[,] Cells;

        private readonly Random _rng;

        public SampleBoard(int width, int height, int colorCount, int seed)
        {
            if (colorCount < 3) throw new ArgumentOutOfRangeException(nameof(colorCount), "Use at least 3 colours.");
            Width = width;
            Height = height;
            ColorCount = colorCount;
            Cells = new ushort[height, width];
            _rng = new Random(seed);
            Match3Hashing.GlobalSetup();
        }

        public ushort Get(Vector2Int p) => Cells[p.y, p.x];
        public void Set(Vector2Int p, ushort v) => Cells[p.y, p.x] = v;
        public bool InBoard(Vector2Int p) => p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height;
        public static bool AreAdjacent(Vector2Int a, Vector2Int b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) == 1;

        private ushort RandomColor() => (ushort)_rng.Next(1, ColorCount + 1);

        /// <summary>Fills the board with random colours, avoiding ready-made lines of 3.</summary>
        public void FillWithoutLines()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    ushort c;
                    do { c = RandomColor(); }
                    while ((x >= 2 && Cells[y, x - 1] == c && Cells[y, x - 2] == c) ||
                           (y >= 2 && Cells[y - 1, x] == c && Cells[y - 2, x] == c));
                    Cells[y, x] = c;
                }
        }

        public void Swap(Vector2Int a, Vector2Int b)
        {
            var t = Get(a);
            Set(a, Get(b));
            Set(b, t);
        }

        /// <summary>
        /// The core call: one local scan at the landing cell.
        /// Direction = landing - initial (normalised), exactly as the original game computes it.
        /// </summary>
        public ScanRecord Scan(Vector2Int landing, Vector2Int initial)
        {
            if (Get(landing) == Empty) return null; // an empty cell would "match" its empty neighbours

            var direction = initial.GetMovementDirection(landing);
            var hash = Match3Hashing.ComputeFrankShenHash(Cells, landing, direction);
            var match = Match3Hashing.FindMatchAtDirection(Cells, landing, direction);
            return new ScanRecord { Landing = landing, Direction = direction, Hash = hash, Match = match };
        }

        /// <summary>Scans every moved tile. Lowest tiles first, so the log reads bottom-up like the fall.</summary>
        public List<ScanRecord> ScanMoves(IEnumerable<TileMove> moves)
        {
            var list = new List<TileMove>(moves);
            list.Sort((a, b) => b.To.y.CompareTo(a.To.y));
            var records = new List<ScanRecord>();
            foreach (var m in list)
            {
                var r = Scan(m.To, m.From);
                if (r != null) records.Add(r);
            }
            return records;
        }

        /// <summary>Union of every matched cell in the records (a cell found by two scans counts once).</summary>
        public static HashSet<(int x, int y)> MatchedCells(IEnumerable<ScanRecord> records)
        {
            var set = new HashSet<(int x, int y)>();
            foreach (var r in records)
                if (r.Match != null)
                    foreach (var c in r.Match.Coords) set.Add(c);
            return set;
        }

        public void Clear(IEnumerable<(int x, int y)> cells)
        {
            foreach (var (x, y) in cells)
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                    Cells[y, x] = Empty;
        }

        /// <summary>
        /// Drops tiles into the holes and spawns new ones above the board.
        /// Returns every tile that moved; spawned tiles report a From above the board (y &lt; 0),
        /// so their movement direction is still Vector2Int.down.
        /// </summary>
        public List<TileMove> CollapseAndRefill()
        {
            var moves = new List<TileMove>();
            for (int x = 0; x < Width; x++)
            {
                int write = Height - 1;
                for (int y = Height - 1; y >= 0; y--)
                {
                    if (Cells[y, x] == Empty) continue;
                    if (y != write)
                    {
                        Cells[write, x] = Cells[y, x];
                        Cells[y, x] = Empty;
                        moves.Add(new TileMove(new Vector2Int(x, y), new Vector2Int(x, write)));
                    }
                    write--;
                }
                // write is now the lowest empty row; fill it and everything above with new tiles
                int spawned = 0;
                for (int y = write; y >= 0; y--)
                {
                    spawned++;
                    Cells[y, x] = RandomColor();
                    moves.Add(new TileMove(new Vector2Int(x, -spawned), new Vector2Int(x, y)));
                }
            }
            return moves;
        }
    }
}
