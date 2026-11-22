# FrankMatch3LocalScan — UnitySample

A minimal Unity project that plays a match-3 board using only the FrankMatch3LocalScan local scan.
No sprites, scenes or extra packages: the board is drawn with IMGUI so the algorithm is the only thing to read.

## Run it

1. Open this folder (`UnitySample`) with **Unity 6000.0** (made with 6000.0.75f1; any 6000.0.x should do).
2. Press **Play** in the empty scene. The sample creates its own camera and board (`Match3SampleView.Bootstrap`).
3. Click a tile, then click a neighbour to swap.

The panel on the right logs every local scan:

```
(3,4) moved left  hash 00100100 ( 36) → H3
```

- `moved left` — the direction the tile travelled to reach the landing cell.
- `hash` — the 8 sampling points, bit 7 to bit 0. A bit is 1 when that cell has the landing tile's colour.
- `→ H3` — the pattern the 256-entry lookup table returns for that byte.

The black dots on the board are the 8 sampling points of the latest scan. A swap that finds no pattern is reverted.
After a match, tiles fall and new ones spawn above the board; each landed tile is scanned once with direction `down`.
The whole board is never rescanned.

## Files

| Path | What it is |
|---|---|
| `Runtime/Algorithms/` | The algorithm, copied from [`../Algorithms`](../Algorithms). `Match3Hashing.cs` only had game-specific usings, one `Debug.Log` and a game constant removed. |
| `Runtime/Sample/SampleBoard.cs` | Pure C# board: swap, local scan, clear, collapse and refill. No `UnityEngine` dependency. |
| `Runtime/Sample/SamplingLayout.cs` | The 8 sampling offsets per direction, for drawing and tests. |
| `Runtime/Sample/Match3SampleView.cs` | The IMGUI view and the swap / cascade loop. |
| `Tests/Editor/Match3HashingTests.cs` | EditMode tests (Window ▸ General ▸ Test Runner). |

## What the tests check

- **The lookup table is consistent.** For all 256 hashes, the table returns the largest pattern whose cells are all set in the hash, or `None` when no pattern fits. This also means the table could be generated from the pattern list instead of written by hand.
- **The hash layout is right in all four directions.** For every byte value and direction, a board built from that byte hashes back to the same byte.
- A horizontal swap finds `H3` at the correct absolute cells, and refilled tiles always move `down`.

## Coordinates

`ushort[,] board` is indexed `[y, x]`. `x` is the column, `y` is the row and grows downwards, so `Vector2Int.down == (0, 1)`.
`0` is an empty cell in this sample. The movement direction is `landing - initial`, normalised.

## Notes for extending it

- The patterns `V3D` (landing tile beside a vertical three) and `S2A` / `S2B` (2×2 squares) are rules of the original game. Edit `predefinedHashkeyPatternsLookup` for your own rules.
- The window is 5 cells long along each axis through the landing cell. A run that continues past it is not included in the returned coordinates.
- `Match3Hashing` keeps board size in static fields while hashing, so do not call it from several threads at once.

MIT licence, same as the repository.
