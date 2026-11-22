using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Vector2Int = imady.FrankShenMatch3Hashing.Vector2Int;

namespace imady.FrankShenMatch3Hashing.Sample
{
    /// <summary>
    /// Bare-bones playable view for the sample, drawn with IMGUI so it needs no scene, sprites or input package.
    /// Click a tile, then click a neighbour to swap. Every swap and every landing tile is checked with one
    /// local scan; the panel on the right shows each scan's direction, 8-bit hash and the pattern it maps to.
    /// The 8 sampling points of the latest scan are marked with small dots on the board.
    /// </summary>
    public class Match3SampleView : MonoBehaviour
    {
        [Header("Board")]
        public int width = 8;
        public int height = 8;
        [Range(3, 6)] public int colorCount = 5;
        public int seed = 2026;

        [Header("Timing (seconds)")]
        public float stepDelay = 0.35f;

        static readonly Color[] Palette =
        {
            new Color(0.15f, 0.15f, 0.17f), // empty
            new Color(0.91f, 0.30f, 0.24f),
            new Color(0.20f, 0.60f, 0.86f),
            new Color(0.18f, 0.80f, 0.44f),
            new Color(0.95f, 0.77f, 0.06f),
            new Color(0.61f, 0.35f, 0.71f),
            new Color(0.90f, 0.49f, 0.13f),
        };
        static readonly string[] Glyph = { "", "A", "B", "C", "D", "E", "F" };

        SampleBoard _board;
        Vector2Int? _selected;
        bool _busy;
        HashSet<(int x, int y)> _highlight = new HashSet<(int x, int y)>();
        ScanRecord _lastScan;
        readonly List<string> _log = new List<string>();
        int _moves, _cascadeTotal;
        GUIStyle _cellStyle, _logStyle;
        Texture2D _white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            // Press Play in any empty scene: the sample creates itself.
            if (FindFirstObjectByType<Match3SampleView>() != null) return;
            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            }
            new GameObject("FrankMatch3LocalScan Sample").AddComponent<Match3SampleView>();
        }

        void Start() => NewBoard();

        void NewBoard()
        {
            _board = new SampleBoard(width, height, colorCount, seed + _moves);
            _board.FillWithoutLines();
            _selected = null;
            _highlight.Clear();
            _lastScan = null;
            _log.Clear();
            Log("New board. Click a tile, then a neighbour to swap.");
        }

        void Log(string line)
        {
            _log.Insert(0, line);
            if (_log.Count > 22) _log.RemoveAt(_log.Count - 1);
            Debug.Log("[FrankMatch3LocalScan] " + line);
        }

        static string DirName(Vector2Int d) =>
            d == Vector2Int.left ? "left" : d == Vector2Int.right ? "right" : d == Vector2Int.up ? "up" : "down";

        void LogScan(ScanRecord r)
        {
            string bits = System.Convert.ToString(r.Hash, 2).PadLeft(8, '0');
            Log($"({r.Landing.x},{r.Landing.y}) moved {DirName(r.Direction),-5} hash {bits} ({r.Hash,3}) → {r.Pattern}");
        }

        void OnClick(Vector2Int cell)
        {
            if (_busy) return;
            if (_selected == null) { _selected = cell; return; }
            var a = _selected.Value;
            _selected = null;
            if (a == cell) return;
            if (!SampleBoard.AreAdjacent(a, cell)) { _selected = cell; return; }
            StartCoroutine(SwapRoutine(a, cell));
        }

        IEnumerator SwapRoutine(Vector2Int a, Vector2Int b)
        {
            _busy = true;
            _board.Swap(a, b);
            yield return new WaitForSeconds(stepDelay);

            // Two local scans: the tile now on b came from a, and the tile now on a came from b.
            var moves = new[] { new TileMove(a, b), new TileMove(b, a) };
            var records = _board.ScanMoves(moves);
            foreach (var r in records) { LogScan(r); _lastScan = r; }

            if (SampleBoard.MatchedCells(records).Count == 0)
            {
                Log("No pattern found: swap reverted.");
                yield return new WaitForSeconds(stepDelay);
                _board.Swap(a, b);
                _busy = false;
                yield break;
            }

            _moves++;
            int cascade = 0;
            while (true)
            {
                var matched = SampleBoard.MatchedCells(records);
                if (matched.Count == 0) break;
                if (cascade > 0) _cascadeTotal++;
                _highlight = matched;
                yield return new WaitForSeconds(stepDelay);

                _board.Clear(matched);
                _highlight.Clear();
                yield return new WaitForSeconds(stepDelay * 0.6f);

                var landed = _board.CollapseAndRefill();
                yield return new WaitForSeconds(stepDelay);

                // Cascades: only the tiles that just landed are scanned, each with direction = down.
                cascade++;
                records = _board.ScanMoves(landed);
                var found = records.FindAll(r => r.Match != null);
                Log($"Cascade {cascade}: {landed.Count} tiles landed, {landed.Count} local scans, {found.Count} pattern(s).");
                foreach (var r in found) { LogScan(r); _lastScan = r; }
            }
            _busy = false;
        }

        void EnsureStyles()
        {
            if (_cellStyle != null) return;
            _white = Texture2D.whiteTexture;
            _cellStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _logStyle = new GUIStyle(GUI.skin.label) { font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Menlo", "Courier New" }, 13), fontSize = 13, wordWrap = false };
            _logStyle.normal.textColor = new Color(0.85f, 0.87f, 0.9f);
        }

        void OnGUI()
        {
            if (_board == null) return;
            EnsureStyles();

            float margin = 20f;
            float cell = Mathf.Floor(Mathf.Min((Screen.height - margin * 2 - 40) / height, (Screen.width * 0.5f - margin) / width));
            var origin = new Vector2(margin, margin + 40);

            GUI.Label(new Rect(margin, margin, 600, 30),
                $"FrankMatch3LocalScan sample   moves: {_moves}   cascades: {_cascadeTotal}");
            if (GUI.Button(new Rect(origin.x + cell * width - 110, margin, 110, 26), "New board") && !_busy) NewBoard();

            _cellStyle.fontSize = Mathf.Max(10, (int)(cell * 0.32f));
            var sampled = SampledCells();

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var r = new Rect(origin.x + x * cell, origin.y + y * cell, cell - 3, cell - 3);
                    ushort v = _board.Cells[y, x];
                    var p = new Vector2Int(x, y);
                    var col = Palette[Mathf.Clamp(v, 0, Palette.Length - 1)];
                    if (_highlight.Contains((x, y))) col = Color.Lerp(col, Color.white, 0.65f);
                    DrawRect(r, col);
                    if (_selected.HasValue && _selected.Value == p) DrawFrame(r, Color.white, 3);
                    if (_lastScan != null && _lastScan.Landing == p) DrawFrame(r, Color.black, 2);
                    GUI.Label(r, Glyph[Mathf.Clamp(v, 0, Glyph.Length - 1)], _cellStyle);
                    if (sampled.Contains((x, y)))
                    {
                        float d = Mathf.Max(6, cell * 0.16f);
                        DrawRect(new Rect(r.xMax - d - 3, r.y + 3, d, d), Color.black);
                    }
                    if (!_busy && GUI.Button(r, GUIContent.none, GUIStyle.none)) OnClick(p);
                }

            var logRect = new Rect(origin.x + cell * width + 24, origin.y, Screen.width - (origin.x + cell * width + 24) - margin, cell * height);
            DrawRect(logRect, new Color(0, 0, 0, 0.35f));
            var sb = new StringBuilder();
            sb.AppendLine("Latest first. Black dots = 8 sampling points of the latest scan.");
            sb.AppendLine("Hash bits 7..0; the lookup table maps the byte to a pattern.");
            sb.AppendLine();
            foreach (var l in _log) sb.AppendLine(l);
            GUI.Label(new Rect(logRect.x + 10, logRect.y + 8, logRect.width - 20, logRect.height - 16), sb.ToString(), _logStyle);
        }

        HashSet<(int x, int y)> SampledCells()
        {
            var set = new HashSet<(int x, int y)>();
            if (_lastScan == null) return set;
            for (int bit = 7; bit >= 0; bit--)
            {
                var (dx, dy) = SamplingLayout.OffsetForBit(bit, _lastScan.Direction);
                set.Add((_lastScan.Landing.x + dx, _lastScan.Landing.y + dy));
            }
            return set;
        }

        void DrawRect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = old;
        }

        void DrawFrame(Rect r, Color c, float t)
        {
            DrawRect(new Rect(r.x, r.y, r.width, t), c);
            DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            DrawRect(new Rect(r.x, r.y, t, r.height), c);
            DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }
    }
}
