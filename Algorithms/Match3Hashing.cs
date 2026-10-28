using imady.MatchingTowers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RectInt = imady.FrankShenMatch3Hashing.RectInt;
using Vector2Int = imady.FrankShenMatch3Hashing.Vector2Int;

namespace imady.FrankShenMatch3Hashing
{
    public class Match3Hashing
    {
        public static Dictionary<Vector2Int, Vector2Int[]> movementDirectionSamplingPoints = new Dictionary<Vector2Int, Vector2Int[]>()
        {
            [Vector2Int.left]= new Vector2Int[]
            {
                new Vector2Int(1, 2),
                new Vector2Int(3, 2),
                new Vector2Int(2, 1),
                new Vector2Int(0, 2),
                new Vector2Int(4, 2),
                new Vector2Int(2, 0),
                new Vector2Int(1, 1),
                new Vector2Int(3, 1)
            },
            // UP: rotate 90° CCW (unit moves from below to (2,2))
            [Vector2Int.up] = new Vector2Int[]
            {
                new Vector2Int(2, 3), // bit 7
                new Vector2Int(2, 1), // bit 6
                new Vector2Int(1, 2), // bit 5
                new Vector2Int(2, 4), // bit 4
                new Vector2Int(2, 0), // bit 3
                new Vector2Int(0, 2), // bit 2
                new Vector2Int(1, 3), // bit 1
                new Vector2Int(1, 1)  // bit 0
            },
            // RIGHT: rotate 180° (unit moves from left to (2,2))
            [Vector2Int.right] = new Vector2Int[]
            {
                new Vector2Int(3, 1), // bit 7
                new Vector2Int(1, 1), // bit 6
                new Vector2Int(2, 2), // bit 5
                new Vector2Int(4, 1), // bit 4
                new Vector2Int(0, 1), // bit 3
                new Vector2Int(2, 3), // bit 2
                new Vector2Int(3, 2), // bit 1
                new Vector2Int(1, 2)  // bit 0
            },
            // DOWN: rotate 270° CCW (unit moves from above to (2,2))
            [Vector2Int.down] = new Vector2Int[]
            {
                new Vector2Int(1, 1),// bit 7
                new Vector2Int(1, 3),// bit 6
                new Vector2Int(2, 2),// bit 5
                new Vector2Int(1, 0),// bit 4
                new Vector2Int(1, 4),// bit 3
                new Vector2Int(3, 2),// bit 2
                new Vector2Int(2, 1),// bit 1
                new Vector2Int(2, 3) }
        };
        static Dictionary<Vector2Int, RectInt> predefinedDetectRect;
        static Dictionary<Vector2Int, Vector2Int[]> predefinedSamplingCoords;
        static Dictionary<MT_MatchPattern, List<(int,int)>> predefinedPatternCoordsLookup;
        static Dictionary<byte, MT_MatchPattern> predefinedHashkeyPatternsLookup = new Dictionary<byte, MT_MatchPattern>();
        static Dictionary<Vector2Int, Func<ushort[,], int, int, byte>> predefinedHashPatternFuncs;
        static List<ushort[,]> predefinedMatchPatternArrays;

        static int _rows, _cols;
        static bool IsValidGridPosition(int x, int y) => y >= 0 && y < _rows && x >= 0 && x < _cols;

        public static void GlobalSetup()
        {
            predefinedDetectRect?.Clear();
            predefinedHashPatternFuncs?.Clear();
            predefinedMatchPatternArrays?.Clear();
            predefinedHashkeyPatternsLookup?.Clear();
            predefinedPatternCoordsLookup?.Clear();


            #region the pre-defined rotated rects relative to the landing point
            predefinedDetectRect = new Dictionary<Vector2Int, RectInt>()
            {
                { Vector2Int.left, new RectInt(new Vector2Int(-2,-2), new Vector2Int(1,2)) },
                { Vector2Int.up, new RectInt(new Vector2Int(-2,-2), new Vector2Int(2,1)) },
                { Vector2Int.right, new RectInt(new Vector2Int(-1,-2), new Vector2Int(2,2)) },
                { Vector2Int.down, new RectInt(new Vector2Int(-2,-1), new Vector2Int(2,2)) }
            };
            #endregion

            #region the FrankShenHashing functions for each direction
            //update 2025-05-21
            //the declaration and preset value of the predefinedHashPatternFuncs is subject to some change.
            //I now want the functions to be able to process an array of ushort at any size.
            //There will be a passed-in parameter of landing position instea, indicating the base location
            //for the hashing calculation.
            //
            //There will be 8 sampling points, each represent one bit on the returned hashing result in byte
            //format. To deal with potential specific cases that adjacent to the array's boundry, you will
            //also need to do boundry check by using the following method:
            //    Vector2Int.IsValidGridPosition()
            //This has been provided in the Vector2IntGlobalGridExtensions so you don't need do anything extra.
            //The following is the list of relative coords to the landing position to be sampled:
            //Left: (0, -1), (0, 1), (-1, 0), (0, -2), (0, 2), (-2, 0), (-1, -1), (-1, 1)
            //Up: (1, 0), (-1, 0), (0, -1), (2, 0),(-2, 0), (0, -2), (1, -1), (-1, -1)
            //Right: (0,1), (0,-1), (1,0), (0,2), (0,-2), (2,0), (1,1), (1,-1)
            //Down: (-1,0), (1,0), (0,1), (-2,0), (2,0), (0,2), (-1,1), (1,1)
            predefinedHashPatternFuncs = new Dictionary<Vector2Int, Func<ushort[,], int, int, byte>>
            {
                // LEFT
                [Vector2Int.left] = (array, x, y) =>
                {
                    /* Debug print value and array
                    //StringBuilder sb = new StringBuilder();
                    //for (int j = 0; j < 5; j++)
                    //{
                    //    for (int i = 0; i < 5; i++)
                    //    {
                    //        sb.Append(array[j, i]);
                    //        if (x < 5 - 1) sb.Append(" ");
                    //    }
                    //    sb.AppendLine();
                    //}
                    //Debug.Log(sb.ToString()); */
                    var current = array[y, x];
                    _rows = array.GetLength(0);
                    _cols = array.GetLength(1);
                    byte hash = 0;
                    if (IsValidGridPosition(x,     y - 1   ) && array[y - 1, x    ] == current) hash |= (byte)(1 << 7); // (0, -1)
                    if (IsValidGridPosition(x,     y + 1   ) && array[y + 1, x    ] == current) hash |= (byte)(1 << 6); // (0, 1)
                    if (IsValidGridPosition(x - 1, y       ) && array[y,     x-1  ] == current) hash |= (byte)(1 << 5); // (-1, -1)
                    if (IsValidGridPosition(x,     y - 2   ) && array[y - 2, x    ] == current) hash |= (byte)(1 << 4); // (0, -2)
                    if (IsValidGridPosition(x,     y + 2   ) && array[y + 2, x    ] == current) hash |= (byte)(1 << 3); // (0, 2)
                    if (IsValidGridPosition(x - 2, y       ) && array[y    , x - 2] == current) hash |= (byte)(1 << 2); // (-2, 0)
                    if (IsValidGridPosition(x - 1, y - 1   ) && array[y - 1, x - 1] == current) hash |= (byte)(1 << 1); // (-1, -1) (duplicate)
                    if (IsValidGridPosition(x - 1, y + 1   ) && array[y + 1, x - 1] == current) hash |= (byte)(1);// (-1, 1)
                    return hash;
                },
                // UP
                [Vector2Int.up] = (array, x, y) =>
                {
                    var current = array[y, x];
                    _rows = array.GetLength(0);
                    _cols = array.GetLength(1);
                    byte hash = 0;
                    if (IsValidGridPosition(x + 1, y       ) && array[y       , x + 1     ] == current) hash |= (byte)(1 << 7); // (1, 0)
                    if (IsValidGridPosition(x - 1, y       ) && array[y       , x - 1     ] == current) hash |= (byte)(1 << 6); // (-1, 0)
                    if (IsValidGridPosition(x,     y - 1   ) && array[y - 1   , x         ] == current) hash |= (byte)(1 << 5); // (0, -1)
                    if (IsValidGridPosition(x + 2, y       ) && array[y       , x + 2     ] == current) hash |= (byte)(1 << 4); // (2, 0)
                    if (IsValidGridPosition(x - 2, y       ) && array[y       , x - 2     ] == current) hash |= (byte)(1 << 3); // (-2, 0)
                    if (IsValidGridPosition(x,     y - 2   ) && array[y - 2   , x         ] == current) hash |= (byte)(1 << 2); // (0, -2)
                    if (IsValidGridPosition(x + 1, y - 1   ) && array[y - 1   , x + 1     ] == current) hash |= (byte)(1 << 1); // (1, -1)
                    if (IsValidGridPosition(x - 1, y - 1   ) && array[y - 1   , x - 1     ] == current) hash |= (byte)(1);      // (-1, -1)
                    return hash;
                },
                // RIGHT
                [Vector2Int.right] = (array, x, y) =>
                {
                    var current = array[y, x];
                    _rows = array.GetLength(0);
                    _cols = array.GetLength(1);
                    byte hash = 0;
                    if (IsValidGridPosition(x,     y + 1   ) && array[y + 1   ,x     ]  == current) hash |= (byte)(1 << 7); // (0, 1)
                    if (IsValidGridPosition(x,     y - 1   ) && array[y - 1   ,x     ]  == current) hash |= (byte)(1 << 6); // (0, -1)
                    if (IsValidGridPosition(x + 1, y       ) && array[y       ,x + 1 ]  == current) hash |= (byte)(1 << 5); // (1, 0)
                    if (IsValidGridPosition(x,     y + 2   ) && array[y + 2   ,x     ]  == current) hash |= (byte)(1 << 4); // (0, 2)
                    if (IsValidGridPosition(x,     y - 2   ) && array[y - 2   ,x     ]  == current) hash |= (byte)(1 << 3); // (0, -2)
                    if (IsValidGridPosition(x + 2, y       ) && array[y       ,x + 2 ]  == current) hash |= (byte)(1 << 2); // (2, 0)
                    if (IsValidGridPosition(x + 1, y + 1   ) && array[y + 1   ,x + 1 ]  == current) hash |= (byte)(1 << 1); // (1, 1)
                    if (IsValidGridPosition(x + 1, y - 1   ) && array[y - 1   ,x + 1 ]  == current) hash |= (byte)(1);      // (1, -1)
                    return hash;
                },
                // DOWN
                [Vector2Int.down] = (array, x, y) =>
                {
                    var current = array[y, x];
                    _rows = array.GetLength(0);
                    _cols = array.GetLength(1);
                    byte hash = 0;
                    if (IsValidGridPosition(x - 1, y       ) && array[y       , x - 1]  == current) hash |= (byte)(1 << 7); // (-1, 0)
                    if (IsValidGridPosition(x + 1, y       ) && array[y       , x + 1]  == current) hash |= (byte)(1 << 6); // (1, 0)
                    if (IsValidGridPosition(x,     y + 1   ) && array[y + 1   , x    ]  == current) hash |= (byte)(1 << 5); // (0, 1)
                    if (IsValidGridPosition(x - 2, y       ) && array[y       , x - 2]  == current) hash |= (byte)(1 << 4); // (-2, 0)
                    if (IsValidGridPosition(x + 2, y       ) && array[y       , x + 2]  == current) hash |= (byte)(1 << 3); // (2, 0)
                    if (IsValidGridPosition(x,     y + 2   ) && array[y + 2   , x    ]  == current) hash |= (byte)(1 << 2); // (0, 2)
                    if (IsValidGridPosition(x - 1, y + 1   ) && array[y + 1   , x - 1]  == current) hash |= (byte)(1 << 1); // (-1, 1)
                    if (IsValidGridPosition(x + 1, y + 1   ) && array[y + 1   , x + 1]  == current) hash |= (byte)(1);      // (1, 1)
                    return hash;
                }
            };            
            #endregion

            #region Pre-Define 2D ushort arrays for each pattern 
            ushort[,] H3 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 1, 1, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] V3_Situation1 = new ushort[5, 4]
            {
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] V3_Situation2 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 }
            };

            ushort[,] V3_Situation3 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] V3_Situation4 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] S2_Situation1 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 1, 1, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] S2_Situation2 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 1, 1, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] V4_Situation1 = new ushort[5, 4]
            {
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] V4_Situation2 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 }
            };

            ushort[,] L5_Situation1 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 1, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 }
            };

            ushort[,] L5_Situation2 = new ushort[5, 4]
            {
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 },
            { 1, 1, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] T5 = new ushort[5, 4]
            {
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 1, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 }
            };

            ushort[,] V5 = new ushort[5, 4]
            {
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 }
            };

            ushort[,] T7 = new ushort[5, 4]
            {
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 },
            { 1, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 1, 0 }
            };

            predefinedMatchPatternArrays = new List<ushort[,]> { H3, V3_Situation1, V3_Situation2, V3_Situation3, V3_Situation4, S2_Situation1, S2_Situation2, V4_Situation1, V4_Situation2, L5_Situation1, L5_Situation2, T5, V5, T7 };
            #endregion

            #region the predefined <FSHashKey, MatchPattern> looking up dictionary
            // predefinedHashkeyPatternsLookup.Add(0, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(1, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(2, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(3, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(4, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(5, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(6, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(7, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(8, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(9, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(10, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(11, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(12, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(13, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(14, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(15, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(16, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(17, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(18, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(19, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(20, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(21, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(22, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(23, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(24, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(25, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(26, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(27, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(28, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(29, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(30, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(31, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(32, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(33, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(34, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(35, MT_MatchPattern.V3D);
            predefinedHashkeyPatternsLookup.Add(36, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(37, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(38, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(39, MT_MatchPattern.V3D);
            // predefinedHashkeyPatternsLookup.Add(40, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(41, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(42, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(43, MT_MatchPattern.V3D);
            predefinedHashkeyPatternsLookup.Add(44, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(45, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(46, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(47, MT_MatchPattern.V3D);
            // predefinedHashkeyPatternsLookup.Add(48, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(49, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(50, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(51, MT_MatchPattern.V3D);
            predefinedHashkeyPatternsLookup.Add(52, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(53, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(54, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(55, MT_MatchPattern.V3D);
            // predefinedHashkeyPatternsLookup.Add(56, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(57, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(58, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(59, MT_MatchPattern.V3D);
            predefinedHashkeyPatternsLookup.Add(60, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(61, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(62, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(63, MT_MatchPattern.V3D);
            // predefinedHashkeyPatternsLookup.Add(64, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(65, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(66, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(67, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(68, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(69, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(70, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(71, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(72, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(73, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(74, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(75, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(76, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(77, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(78, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(79, MT_MatchPattern.V3B);
            // predefinedHashkeyPatternsLookup.Add(80, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(81, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(82, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(83, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(84, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(85, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(86, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(87, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(88, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(89, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(90, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(91, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(92, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(93, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(94, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(95, MT_MatchPattern.V3B);
            // predefinedHashkeyPatternsLookup.Add(96, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(97, MT_MatchPattern.S2B);
            // predefinedHashkeyPatternsLookup.Add(98, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(99, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(100, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(101, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(102, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(103, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(104, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(105, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(106, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(107, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(108, MT_MatchPattern.L5B);
            predefinedHashkeyPatternsLookup.Add(109, MT_MatchPattern.L5B);
            predefinedHashkeyPatternsLookup.Add(110, MT_MatchPattern.L5B);
            predefinedHashkeyPatternsLookup.Add(111, MT_MatchPattern.L5B);
            // predefinedHashkeyPatternsLookup.Add(112, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(113, MT_MatchPattern.S2B);
            // predefinedHashkeyPatternsLookup.Add(114, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(115, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(116, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(117, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(118, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(119, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(120, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(121, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(122, MT_MatchPattern.V3B);
            predefinedHashkeyPatternsLookup.Add(123, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(124, MT_MatchPattern.L5B);
            predefinedHashkeyPatternsLookup.Add(125, MT_MatchPattern.L5B);
            predefinedHashkeyPatternsLookup.Add(126, MT_MatchPattern.L5B);
            predefinedHashkeyPatternsLookup.Add(127, MT_MatchPattern.L5B);
            // predefinedHashkeyPatternsLookup.Add(128, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(129, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(130, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(131, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(132, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(133, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(134, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(135, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(136, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(137, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(138, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(139, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(140, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(141, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(142, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(143, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(144, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(145, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(146, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(147, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(148, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(149, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(150, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(151, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(152, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(153, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(154, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(155, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(156, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(157, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(158, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(159, MT_MatchPattern.V3A);
            // predefinedHashkeyPatternsLookup.Add(160, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(161, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(162, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(163, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(164, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(165, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(166, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(167, MT_MatchPattern.S2A);
            // predefinedHashkeyPatternsLookup.Add(168, MT_MatchPattern.None);
            // predefinedHashkeyPatternsLookup.Add(169, MT_MatchPattern.None);
            predefinedHashkeyPatternsLookup.Add(170, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(171, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(172, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(173, MT_MatchPattern.H3);
            predefinedHashkeyPatternsLookup.Add(174, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(175, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(176, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(177, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(178, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(179, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(180, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(181, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(182, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(183, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(184, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(185, MT_MatchPattern.V3A);
            predefinedHashkeyPatternsLookup.Add(186, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(187, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(188, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(189, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(190, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(191, MT_MatchPattern.L5A);
            predefinedHashkeyPatternsLookup.Add(192, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(193, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(194, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(195, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(196, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(197, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(198, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(199, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(200, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(201, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(202, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(203, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(204, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(205, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(206, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(207, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(208, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(209, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(210, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(211, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(212, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(213, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(214, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(215, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(216, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(217, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(218, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(219, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(220, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(221, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(222, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(223, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(224, MT_MatchPattern.V3C);
            predefinedHashkeyPatternsLookup.Add(225, MT_MatchPattern.S2B);
            predefinedHashkeyPatternsLookup.Add(226, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(227, MT_MatchPattern.S2A);
            predefinedHashkeyPatternsLookup.Add(228, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(229, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(230, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(231, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(232, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(233, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(234, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(235, MT_MatchPattern.V4A);
            predefinedHashkeyPatternsLookup.Add(236, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(237, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(238, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(239, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(240, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(241, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(242, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(243, MT_MatchPattern.V4B);
            predefinedHashkeyPatternsLookup.Add(244, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(245, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(246, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(247, MT_MatchPattern.T5);
            predefinedHashkeyPatternsLookup.Add(248, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(249, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(250, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(251, MT_MatchPattern.V5);
            predefinedHashkeyPatternsLookup.Add(252, MT_MatchPattern.T7);
            predefinedHashkeyPatternsLookup.Add(253, MT_MatchPattern.T7);
            predefinedHashkeyPatternsLookup.Add(254, MT_MatchPattern.T7);
            predefinedHashkeyPatternsLookup.Add(255, MT_MatchPattern.T7);
            #endregion

            #region the pre-defined matching coords for each MT_MatchPattern type.
            predefinedPatternCoordsLookup = new Dictionary<MT_MatchPattern, List<(int, int)>>()
            {
                [MT_MatchPattern.H3] =  new List<(int,int)>() {(-1,0), (-2,0) },
                [MT_MatchPattern.V3A] = new List<(int,int)>() {(0,-1),(0,-2) },
                [MT_MatchPattern.V3B] = new List<(int,int)>() { (0,1), (0,2) },
                [MT_MatchPattern.V3C] = new List<(int,int)>() { (0,-1), (0,1) },
                [MT_MatchPattern.V3D] = new List<(int,int)>() {(-1,0), (-1,-1), (-1,1) },
                [MT_MatchPattern.S2A] = new List<(int,int)>() { (-1,0), (-1,-1), (0,-1) },
                [MT_MatchPattern.S2B] = new List<(int,int)>() { (-1,0), (-1,1), (0,1) },
                [MT_MatchPattern.V4A] = new List<(int,int)>() {(0,-1), (0, 1), (0, 2) },
                [MT_MatchPattern.V4B] = new List<(int,int)>() {(0,-1), (0, 1), (0,-2) },
                [MT_MatchPattern.L5A] = new List<(int,int)>() {(-1,0),(-2,0), (0,-1), (0,-2) },
                [MT_MatchPattern.L5B] = new List<(int,int)>() {(-1, 0),(-2,0),(0,1),(0,2) },
                [MT_MatchPattern.T5] =  new List<(int,int)>() {(-1,0),(-2,0), (0,-1), (0,1) },
                [MT_MatchPattern.V5] =  new List<(int,int)>() {(0,-1),(0,-2), (0,1), (0,2) },
                [MT_MatchPattern.T7] =  new List<(int,int)>() { (0, -1), (0, -2), (0, 1), (0, 2),(-1,0), (-2,0) },
            };
            #endregion
        }
        public static RectInt GetRotatedRect(Vector2Int direction)
        {
            if (predefinedDetectRect == null) GlobalSetup();
            return predefinedDetectRect[direction];
        }
        public static byte ComputeFrankShenHash(ushort[,] array, Vector2Int landingPos, Vector2Int direction)
        {
            //todo: rotated rects might not be 5x4, need to check
            if (array == null)
                throw new ArgumentNullException();

            try
            {
                return predefinedHashPatternFuncs[direction].Invoke(array, landingPos.x, landingPos.y);
            }
            catch
            {
                return 0;
            }
        }
        public static MT_MatchPattern GetPattern(byte FSHashKey)
        {
            if (predefinedHashkeyPatternsLookup.ContainsKey(FSHashKey))
            {
                return predefinedHashkeyPatternsLookup[FSHashKey];
            }
            else
            {
                return MT_MatchPattern.None;
            }
        }

        /// <summary>
        /// Return un-rotated, original matching points
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<(int, int)> GetMatchingCoords(MT_MatchPattern pattern)
        {
            return predefinedPatternCoordsLookup[pattern];
        }

        /// <summary>
        /// Return ROTATED matching points coords according to movement direction
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="movementDirection"></param>
        /// <returns></returns>
        public static List<(int, int)> GetMatchingCoords(MT_MatchPattern pattern, Vector2Int movementDirection, Vector2Int landingPos)
        {
            if (!predefinedPatternCoordsLookup.ContainsKey(pattern))
                return null;

            var coords = predefinedPatternCoordsLookup[pattern];

            Func<(int, int), (int, int)> rotate =
                movementDirection == Vector2Int.left ? t => t :
                movementDirection == Vector2Int.up ? t => (-t.Item2, t.Item1) :
                movementDirection == Vector2Int.right ? t => (-t.Item1, -t.Item2) :
                                                       t => (t.Item2, -t.Item1);

            // Return absolute positions (including landingPos itself as the first element)
            var result = new List<(int, int)> { (landingPos.x, landingPos.y) };
            result.AddRange(coords.Select(p =>
            {
                var (dx, dy) = rotate(p);
                return (landingPos.x + dx, landingPos.y + dy);
            }));
            return result;
        }

        public static MatchInfo FindMatch(ushort[,] board, Vector2Int landingPos, Vector2Int initialPos)
        {
            Vector2Int movementDirection = initialPos.GetMovementDirection(landingPos);
            Debug.Log($"[Match3Hashing.FindMatch] Movement Direction calculated: {movementDirection.ToString()}");
            //Todo: updated 2025-05-25: For units entering battle field, the initial pos might not adjacent to the landing pos. Subject to check.
            //if (!landingPos.IsAdjacent(initialPos))
            //    return null;
            //0. calculate moving direction

            return FindMatchAtDirection(board, landingPos, movementDirection);
        }
        public static MatchInfo FindMatchAtDirection(ushort[,] board, Vector2Int landingPos, Vector2Int movementDirection)
        {
            //1. prepare initial rectangle(based on the landing point coords)
            //2. rotate the rectangle according to the direction, cache all 4 rectanles
            //RectInt detectRect = FrankShenMatch3Hashing.GetRotatedRect(movementDirection);
            //3. sampling the rotated rectangle from greater board (global board)
            //4. ComputeFrankShenHash the selected points in the samples (points coord rotated or prepared)
            var hashResult = Match3Hashing.ComputeFrankShenHash(board, landingPos, movementDirection);
            //5. FindPattern and get the matching pattern
            var matchingPattern = Match3Hashing.GetPattern(hashResult);
            if (matchingPattern == MT_MatchPattern.None)
                return null;
            // Pass movementDirection to GetMatchingCoords
            List<(int, int)> matchingPoints = Match3Hashing.GetMatchingCoords(matchingPattern, movementDirection, landingPos);
            MatchInfo matchInfo = new MatchInfo(landingPos.x, landingPos.y,
                matchingPattern,
                board[landingPos.y, landingPos.x],
                matchingPoints,
                LegacySpecieIds.Empty // T3 (T3.2): sentinel via LegacySpecieIds
            );
            //6. return pattern and List<(int c, int r)> matching coords (in a MatchInfo object)
            return matchInfo;
        }

    }
}
