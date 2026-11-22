using Vector2Int = imady.FrankShenMatch3Hashing.Vector2Int;

namespace imady.FrankShenMatch3Hashing.Sample
{
    /// <summary>
    /// The 8 sampling points, relative to the landing cell, in bit order (index 0 = bit 7 ... index 7 = bit 0).
    /// Written for a tile moving LEFT; the other three directions are rotations of the same layout,
    /// using the same rotation as Match3Hashing.GetMatchingCoords. Used for drawing and for tests.
    /// </summary>
    public static class SamplingLayout
    {
        public static readonly (int dx, int dy)[] Left =
        {
            (0, -1), (0, 1), (-1, 0), (0, -2), (0, 2), (-2, 0), (-1, -1), (-1, 1)
        };

        public static (int dx, int dy) Rotate((int dx, int dy) p, Vector2Int direction)
        {
            if (direction == Vector2Int.left) return p;
            if (direction == Vector2Int.up) return (-p.dy, p.dx);
            if (direction == Vector2Int.right) return (-p.dx, -p.dy);
            return (p.dy, -p.dx); // down
        }

        /// <summary>Offset of the sampling point for a given bit (7..0) and movement direction.</summary>
        public static (int dx, int dy) OffsetForBit(int bit, Vector2Int direction) => Rotate(Left[7 - bit], direction);
    }
}
