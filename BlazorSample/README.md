# FrankMatch3LocalScan — BlazorSample

The same minimal match-3 board as the Unity sample, running as a **Blazor WebAssembly standalone app**.
Everything runs in the browser. There is no server-side code, so the published output can be hosted on
any static file host (GitHub Pages, Cloudflare Pages, Azure Static Web Apps, a plain folder behind nginx).

## Run it

Requires the .NET 10 SDK.

```bash
cd BlazorSample
dotnet run
```

Open the URL it prints, click a tile, then click a neighbour to swap.
The side panel shows the latest scan's 8 bits, the hash value and the pattern it maps to, plus a log of every scan
during the swap and the cascades that follow. The small dots on the board are the latest scan's sampling points.

## Publish as static files

```bash
dotnet publish -c Release -o publish
```

Serve `publish/wwwroot` from any static host. To host under a sub-path (for example
`https://<user>.github.io/FrankMatch3LocalScan/`), change `<base href="/" />` in `wwwroot/index.html` to that path.

## How it references the algorithm

Nothing is copied. The project file compiles the repository's files in place:

| Source | How |
|---|---|
| `../Algorithms/**/*.cs` | linked unchanged, exactly as used in the original game |
| `../UnitySample/.../Sample/SampleBoard.cs`, `SamplingLayout.cs` | linked; the engine-free board logic is shared with the Unity sample |
| `Compat/UnityShims.cs` | three tiny stand-ins (`UnityEngine.Debug`, an empty `Unity.VisualScripting` namespace, the game's `LegacySpecieIds.Empty`) so the original files compile outside Unity |

The page itself is `Pages/Home.razor`; styles are at the end of `wwwroot/css/app.css` and follow the system light/dark setting.

MIT licence, same as the repository.
