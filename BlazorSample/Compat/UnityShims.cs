// The files in ../Algorithms were written inside a Unity game and mention a few
// names from Unity and from that game. These stand-ins let the files compile
// unchanged in a plain .NET / Blazor WebAssembly project.

namespace UnityEngine
{
    internal static class Debug
    {
        public static void Log(object message) => System.Console.WriteLine(message);
    }
}

namespace Unity.VisualScripting
{
    // Imported by Vector2Int.cs but not used.
    internal static class VisualScriptingPlaceholder { }
}

namespace imady.MatchingTowers
{
    // The game's "empty cell" id, used as the reward sentinel in Match3Hashing.FindMatchAtDirection.
    internal static class LegacySpecieIds
    {
        public const ushort Empty = 0;
    }
}
