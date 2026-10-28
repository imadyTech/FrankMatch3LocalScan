using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;

namespace imady.FrankShenMatch3Hashing
{
    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        // Add useful methods/operators as needed
        public static Vector2Int left => new Vector2Int(-1, 0);
        public static Vector2Int right => new Vector2Int(1, 0);
        public static Vector2Int up => new Vector2Int(0, -1);
        public static Vector2Int down => new Vector2Int(0, 1);
        public static Vector2Int leftUp => new Vector2Int(-1, -1);
        public static Vector2Int leftDown => new Vector2Int(-1, 1);
        public static Vector2Int rightUp => new Vector2Int(1, -1);
        public static Vector2Int rightDown => new Vector2Int(1, 1);
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int one => new Vector2Int(1, 1);
        public static (int x, int y)[] CardinalNeibours =>
            new (int dx, int dy)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        public static (int x, int y)[] AllNeibours =>
            new (int dx, int dy)[] {
                (-1, 0), (1, 0), (0, -1), (0, 1), 
                // left, right, up, down
                (-1, -1), (-1, 1), (1, -1), (1, 1)
                // ↖, ↙, ↗, ↘
            };

        public override string ToString()
        {
            if (this == Vector2Int.left) return "Vector2Int.left";
            if (this == Vector2Int.right) return "Vector2Int.right";
            if (this == Vector2Int.up) return "Vector2Int.up";
            if (this == Vector2Int.down) return "Vector2Int.down";
            if (this == Vector2Int.zero) return "Vector2Int.zero";
            if (this == Vector2Int.one) return "Vector2Int.one";
            return $"{x},{y}";
        }


        public static bool operator ==(Vector2Int a, Vector2Int b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Vector2Int a, Vector2Int b)
        {
            return !(a == b);
        }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
        {
            a.x += b.x;
            a.y += b.y;
            return a;
        }

        public static Vector2Int operator -(Vector2Int a, Vector2Int b)
        {
            a.x -= b.x;
            a.y -= b.y;
            return a;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2Int other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

    }
}
