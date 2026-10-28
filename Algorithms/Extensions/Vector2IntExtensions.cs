using imady.MatchingTowers;
using System;
using System.Collections.Generic; 
using Vector2Int = imady.FrankShenMatch3Hashing.Vector2Int;


namespace imady.FrankShenMatch3Hashing
{
    public static class Vector2IntExtensions
    {
        // --- Directional Shortcuts ---

        /// <summary>
        /// Returns a new Vector2Int one unit above this one.
        /// </summary>
        public static Vector2Int Up(this Vector2Int v) => new Vector2Int(v.x, v.y + 1);

        /// <summary>
        /// Returns a new Vector2Int one unit below this one.
        /// </summary>
        public static Vector2Int Down(this Vector2Int v) => new Vector2Int(v.x, v.y - 1);

        /// <summary>
        /// Returns a new Vector2Int one unit to the left of this one.
        /// </summary>
        public static Vector2Int Left(this Vector2Int v) => new Vector2Int(v.x - 1, v.y);

        /// <summary>
        /// Returns a new Vector2Int one unit to the right of this one.
        /// </summary>
        public static Vector2Int Right(this Vector2Int v) => new Vector2Int(v.x + 1, v.y);

        // --- Diagonal Shortcuts ---
        public static Vector2Int UpLeft(this Vector2Int v) => new Vector2Int(v.x - 1, v.y + 1);

        public static Vector2Int UpRight(this Vector2Int v) => new Vector2Int(v.x + 1, v.y + 1);

        public static Vector2Int DownLeft(this Vector2Int v) => new Vector2Int(v.x - 1, v.y - 1);
        public static Vector2Int DownRight(this Vector2Int v) => new Vector2Int(v.x + 1, v.y - 1);


        // --- Distance ---
        /// <summary>
        /// !!! This function calculates the movement direction from the initial position to the landing position. This is irregular calculation method.
        /// </summary>
        /// <param name="landindPos"></param>
        /// <param name="initialPos"></param>
        /// <returns></returns>
        public static Vector2Int GetMovementDirection(this Vector2Int landindPos, Vector2Int initialPos)
        {
            return new Vector2Int(initialPos.x - landindPos.x, initialPos.y - landindPos.y).Normalize();
        }
        public static Vector2Int Normalize(this Vector2Int v)
        {
            return new Vector2Int(v.x == 0 ? 0 : (v.x > 0 ? 1 : -1), v.y == 0 ? 0 : (v.y > 0 ? 1 : -1));
        }

        public static bool IsAdjacent(this Vector2Int a, Vector2Int b)
        {
            return a.ManhattanDistance(b) == 1;
        }

        /// <summary>
        /// Calculates the Manhattan distance to another Vector2Int.
        /// (Cost of moving horizontally and vertically only)
        /// </summary>
        public static int ManhattanDistance(this Vector2Int a, Vector2Int b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        /// <summary>
        /// Calculates the Chebyshev distance (or chessboard distance) to another Vector2Int.
        /// (Max of horizontal or vertical distance - cost of moving diagonally)
        /// </summary>
        public static int ChebyshevDistance(this Vector2Int a, Vector2Int b) => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));

        /// <summary>
        /// Calculates the Euclidean distance (magnitude) between two Vector2Int points.
        /// </summary>
        public static float EuclideanDistance(this Vector2Int a, Vector2Int b)
        {
            int deltaX = a.x - b.x;
            int deltaY = a.y - b.y;
            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        // --- Neighbors ---

        /// <summary>
        /// Gets the four cardinal neighbors (Up, Down, Left, Right).
        /// </summary>
        public static IEnumerable<Vector2Int> GetCardinalNeighbors(this Vector2Int v)
        {
            yield return v.Up();
            yield return v.Down();
            yield return v.Left();
            yield return v.Right();
        }

        /// <summary>
        /// Gets all eight neighbors (cardinal and diagonal).
        /// </summary>
        public static IEnumerable<Vector2Int> GetAllNeighbors(this Vector2Int v)
        {
            yield return v.Up();
            yield return v.Down();
            yield return v.Left();
            yield return v.Right();
            yield return v.UpLeft();
            yield return v.UpRight();
            yield return v.DownLeft();
            yield return v.DownRight();
        }


        // --- Other Utilities ---
        /// <summary>
        /// Checks if this Vector2Int is within the given bounds (inclusive min, exclusive max).
        /// </summary>
        public static bool IsWithinBounds(this Vector2Int v, int minX, int minY, int maxX, int maxY)
        {
            return v.x >= minX && v.x < maxX && v.y >= minY && v.y < maxY;
        }

        /// <summary>
        /// Checks if this Vector2Int is within the given bounds (inclusive min, exclusive max),
        /// where bounds are defined by another Vector2Int (e.g., grid dimensions).
        /// Assumes boundsMin is (0,0).
        /// </summary>
        public static bool IsWithinBounds(this Vector2Int v, Vector2Int dimensions)
        {
            return v.x >= 0 && v.x < dimensions.x && v.y >= 0 && v.y < dimensions.y;
        }

    }
}