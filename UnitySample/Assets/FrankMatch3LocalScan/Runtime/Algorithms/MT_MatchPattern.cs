using System;
using System.Collections.Generic;

namespace imady.FrankShenMatch3Hashing
{
    public enum MT_MatchPattern
    {
        None,
        H3,
        V3A,
        V3B,
        V3C,
        V3D,
        V4A,
        V4B,
        S2A,
        S2B,
        L5A,
        L5B,
        T5,
        V5,
        T7,
        H5, //N/A (For historical compatibility purpose).
        H4, //N/A (For historical compatibility purpose).
        V4, //N/A (For historical compatibility purpose).
        V3, //N/A (For historical compatibility purpose).
        C5, //N/A (For historical compatibility purpose).
        LT5,//N/A (For historical compatibility purpose).
        S2  //N/A (For historical compatibility purpose).
    }
    public class MatchInfo
    {
        public readonly int X; //coords.x
        public readonly int Y; //coords.y
        public readonly MT_MatchPattern Pattern;
        public readonly ushort SpecieType;
        public ushort RewardType;

        //carries the matching cells.
        public readonly List<(int x, int y)> Coords; // Store all coords for marking
        //Carries the neibour cells surrounding the matched units.
        public List<Vector2Int> Neiborings;

        public MatchInfo(int x, int y, MT_MatchPattern pattern, ushort specieTypeId, List<(int x, int y)> coords, ushort reword)
        {
            X = x;
            Y = y;
            Pattern = pattern;
            SpecieType = specieTypeId;
            Coords = coords; // Assign the list directly
            RewardType = reword;
        }
        public void SetReward(ushort reward)
        {
            this.RewardType = reward;
        }

        public override string ToString()
        {
            return $"{Pattern} of type {SpecieType} starting at ({X}, {Y})";
        }
    }
}