using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using imady.FrankShenMatch3Hashing.Sample;
using Vector2Int = imady.FrankShenMatch3Hashing.Vector2Int;

namespace imady.FrankShenMatch3Hashing.Tests
{
    public class Match3HashingTests
    {
        static readonly Vector2Int[] Directions = { Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down };

        static readonly MT_MatchPattern[] ActivePatterns =
        {
            MT_MatchPattern.H3, MT_MatchPattern.V3A, MT_MatchPattern.V3B, MT_MatchPattern.V3C, MT_MatchPattern.V3D,
            MT_MatchPattern.V4A, MT_MatchPattern.V4B, MT_MatchPattern.S2A, MT_MatchPattern.S2B,
            MT_MatchPattern.L5A, MT_MatchPattern.L5B, MT_MatchPattern.T5, MT_MatchPattern.V5, MT_MatchPattern.T7
        };

        [OneTimeSetUp]
        public void Setup() => Match3Hashing.GlobalSetup();

        /// <summary>The bit mask a pattern needs, built from its (un-rotated) matching coords.</summary>
        static byte MaskOf(MT_MatchPattern p)
        {
            byte mask = 0;
            foreach (var c in Match3Hashing.GetMatchingCoords(p))
            {
                int index = System.Array.IndexOf(SamplingLayout.Left, c);
                Assert.GreaterOrEqual(index, 0, $"{p} uses a cell {c} that is not a sampling point");
                mask |= (byte)(1 << (7 - index));
            }
            return mask;
        }

        static int Bits(byte b) { int n = 0; while (b != 0) { n += b & 1; b >>= 1; } return n; }

        [Test]
        public void LookupTable_PicksTheLargestPatternContainedInEveryHash()
        {
            var masks = ActivePatterns.ToDictionary(p => p, MaskOf);
            for (int h = 0; h < 256; h++)
            {
                var hash = (byte)h;
                var candidates = masks.Where(kv => (hash & kv.Value) == kv.Value).ToList();
                var chosen = Match3Hashing.GetPattern(hash);
                if (candidates.Count == 0)
                {
                    Assert.AreEqual(MT_MatchPattern.None, chosen, $"hash {h} should map to None");
                    continue;
                }
                Assert.AreNotEqual(MT_MatchPattern.None, chosen, $"hash {h} contains a pattern but maps to None");
                Assert.IsTrue((hash & masks[chosen]) == masks[chosen], $"hash {h} maps to {chosen}, which it does not contain");
                int best = candidates.Max(kv => Bits(kv.Value));
                Assert.AreEqual(best, Bits(masks[chosen]), $"hash {h}: {chosen} is not the largest contained pattern");
            }
        }

        [Test]
        public void Hash_SetsExactlyTheSampledBits_InAllFourDirections()
        {
            foreach (var dir in Directions)
                for (int v = 0; v < 256; v++)
                {
                    var board = new ushort[7, 7];
                    for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++) board[y, x] = 2;
                    var c = new Vector2Int(3, 3);
                    board[3, 3] = 1;
                    for (int bit = 7; bit >= 0; bit--)
                        if (((v >> bit) & 1) == 1)
                        {
                            var (dx, dy) = SamplingLayout.OffsetForBit(bit, dir);
                            board[c.y + dy, c.x + dx] = 1;
                        }
                    Assert.AreEqual((byte)v, Match3Hashing.ComputeFrankShenHash(board, c, dir), $"direction {dir}, value {v}");
                }
        }

        [Test]
        public void Swap_FindsHorizontalThree_AndRotatedCoords()
        {
            // Row 2: 1 1 2 1 -> swapping (2,2) and (3,2) moves the 1 left onto (2,2): H3.
            var b = new SampleBoard(5, 5, 4, 1);
            ushort[,] cells =
            {
                { 3, 4, 3, 4, 3 },
                { 4, 3, 4, 3, 4 },
                { 1, 1, 2, 1, 3 },
                { 4, 3, 4, 3, 4 },
                { 3, 4, 3, 4, 3 },
            };
            System.Array.Copy(cells, b.Cells, cells.Length);
            b.Swap(new Vector2Int(2, 2), new Vector2Int(3, 2));

            var r = b.Scan(new Vector2Int(2, 2), new Vector2Int(3, 2));
            Assert.AreEqual(Vector2Int.left, r.Direction);
            Assert.AreEqual(MT_MatchPattern.H3, r.Pattern);
            CollectionAssert.AreEquivalent(new List<(int, int)> { (2, 2), (1, 2), (0, 2) }, r.Match.Coords);
        }

        [Test]
        public void Cascade_SpawnedTilesMoveDown_AndBoardIsFull()
        {
            var b = new SampleBoard(6, 6, 5, 7);
            b.FillWithoutLines();
            b.Clear(new[] { (1, 2), (2, 2), (3, 2) });
            var moves = b.CollapseAndRefill();
            Assert.IsTrue(moves.All(m => m.From.GetMovementDirection(m.To) == Vector2Int.down));
            for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++) Assert.AreNotEqual(SampleBoard.Empty, b.Cells[y, x]);
        }
    }
}
