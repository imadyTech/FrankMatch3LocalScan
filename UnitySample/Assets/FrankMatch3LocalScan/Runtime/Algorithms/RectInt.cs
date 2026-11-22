using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imady.FrankShenMatch3Hashing
{
    public struct RectInt
    {
        public Vector2Int position; // The minimum corner (x, y)
        public Vector2Int size;     // The width and height (w, h)

        public RectInt(Vector2Int position, Vector2Int size)
        {
            this.position = position;
            this.size = size;
        }

        public int xMin => position.x;
        public int yMin => position.y;
        public int xMax => position.x + size.x;
        public int yMax => position.y + size.y;

        public override string ToString()
        {
            return $"RectInt(pos: {position}, size: {size})";
        }

        // Optionally, add methods like Contains, Overlaps, etc. as needed.
    }
}
