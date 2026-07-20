# AcrossTheObelisk-Mods

Workspace for developing and maintaining **Across the Obelisk** BepInEx mods.

Currently contains a rebuilt, API-compatible version of **Quick_Save** (originally by Binbin /
`binbinmods`), fixed to work with the game's July 2026 update after the Obeliskial framework fell
out of date and the stock mod started silently blocking all UI clicks.

### Changes vs stock Quick_Save

- Remapped save/load calls that the 2026-07-09 game update moved from `AtOManager` to `SaveManager`.
- Hardened the shared `BotonRollover.OnMouseUp` patch (null-guards + try/catch + only-our-buttons)
  so a mod error can never freeze all game input again.
- Re-added a **Reset Seed** button (opt-in, on by default) that appears in the starting town before
  your first battle. Clicking it rolls a new game seed and regenerates the map, letting you reroll
  the path/quests until you find one you like, then continue. Hidden once combat has started.

## Layout

| Path | Purpose |
|------|---------|
| `src/QuickSave/` | Source for the rebuilt Quick_Save mod (`.csproj`, patches, functions). |
| `scripts/` | PowerShell helpers: build+deploy, log capture, decompile, game-info, backups. |
| `tools/AtoDecompile/` | Small C# tool (ICSharpCode.Decompiler) to dump game/mod types to C#. |
| `docs/AGENTS.md` | Persistent modding context (paths, mod stack, build steps, debugging). |
| `docs/DIAGNOSIS.md` | Root-cause write-up of the Quick_Save click-blocking bug and the fix. |
| `docs/REBUILD.md` | Rebuild & recovery runbook — how to get back to a working state after an update. |
| `refs/` | Decompiled reference code (git-ignored; regenerate via `scripts/decompile-*.ps1`). |
| `backups/`, `diagnostics/` | Local backups and captured logs (git-ignored). |

## Build & deploy

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-mod.ps1
```

Builds `src/QuickSave`, backs up the stock DLL in the TMM profile, and deploys the workspace build to
`%APPDATA%\Thunderstore Mod Manager\DataFolder\AcrossTheObelisk\profiles\Default\BepInEx\plugins\Binbin-Quick_Save\`.

Then launch via TMM, reproduce, and capture the log:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\grab-log.ps1
```

## Requirements

- Across the Obelisk (Steam) installed at the path in `docs/AGENTS.md`.
- .NET SDK 9.x, BepInEx pack + Quick_Save installed via Thunderstore Mod Manager.
- Build targets the **installed** game DLLs — always rebuild after a game update.
