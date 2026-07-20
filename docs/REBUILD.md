# Rebuild & Recovery Runbook — Quick_Save (workspace build)

This is the step-by-step guide for **recreating the working modded Quick_Save from scratch** and for
**getting back to a working state** after a Thunderstore mod update or an Across the Obelisk game
update breaks things. Read `DIAGNOSIS.md` for *why* it broke; this file is the *how to fix it*.

---

## 0. TL;DR — "Thunderstore/game updated and Quick_Save is broken again"

Most breakages are one of two things: (a) TMM re-downloaded the stock Quick_Save DLL and overwrote
our build, or (b) a game update moved/renamed an API our code calls. Recovery:

```powershell
cd "C:\Users\dhelm\source\repos\AcrossTheObelisk-Mods"

# 1. Rebuild our DLL against the CURRENTLY INSTALLED game DLLs and redeploy it.
powershell -ExecutionPolicy Bypass -File .\scripts\build-mod.ps1

# 2. Launch the game through Thunderstore Mod Manager, reproduce, then grab the log.
powershell -ExecutionPolicy Bypass -File .\scripts\grab-log.ps1
```

- If `build-mod.ps1` **succeeds**, you're done — just re-run it any time TMM overwrites our DLL.
- If `build-mod.ps1` **fails with `CS1061` / `CS0117` / `CS0246` (missing member/type)**, the game
  update drifted an API. Go to **Section 4 (API drift)**.
- If it builds but buttons/behavior misbehave in-game, go to **Section 5 (runtime debugging)**.

> Why rebuilding fixes it: our `.csproj` references the DLLs in the *installed* game folder, so a
> fresh build is always compiled against whatever game version is currently on disk.

---

## 1. Prerequisites (one-time)

| Tool | Notes |
|------|-------|
| Across the Obelisk (Steam) | `C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk` |
| Thunderstore Mod Manager (TMM) | Profile `Default`; installs BepInEx + Quick_Save. The game must be launched **through TMM** so the profile is injected. |
| .NET SDK 9.x | `dotnet --version` (was `9.0.304`). Used to build the mod and the decompiler. |
| Git + GitHub CLI (`gh`) | For source control / pushing. `gh` installed via `winget install --id GitHub.cli`. |

Install the mod stack once via TMM: install **Quick_Save** (pulls in the BepInEx pack + Obeliskial
framework as dependencies). Then in TMM **disable Obeliskial Essentials and Obeliskial Content** —
they are incompatible with the 2026-07-09+ game build and Quick_Save does not need them (see
`DIAGNOSIS.md`). Leave the BepInEx pack enabled.

Key paths (all referenced by the scripts / `.csproj`):

- Managed game DLLs: `...\Across the Obelisk\AcrossTheObelisk_Data\Managed`
- TMM profile: `%APPDATA%\Thunderstore Mod Manager\DataFolder\AcrossTheObelisk\profiles\Default`
- BepInEx core: `<profile>\BepInEx\core`
- Quick_Save plugin folder (deploy target): `<profile>\BepInEx\plugins\Binbin-Quick_Save\`
- Runtime log: `<profile>\BepInEx\LogOutput.log`

---

## 2. How the mod project was set up (recreate from scratch)

1. **Project shape.** `src/QuickSave/QuickSave.csproj` is a `netstandard2.1` class library whose
   output assembly name is `com.binbin.quicksave` (must match the original so TMM/BepInEx treats it
   as the same plugin). It defines `<GameDir>`, `<Managed>`, `<Profile>`, `<BepInExCore>` properties
   so all references resolve off the installed game + profile.

2. **References (all `Private=false`, i.e. copy-local off — never ship game/BepInEx DLLs):**
   - `Assembly-CSharp.dll` (the game — **the installed build**),
   - `0Harmony.dll`, `BepInEx.dll` (from BepInEx `core`),
   - `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`,
     `UnityEngine.InputLegacyModule.dll`, `Unity.InputSystem.dll`, `Unity.TextMeshPro.dll`,
   - **Photon**: `PhotonUnityNetworking.dll`, `PhotonRealtime.dll`, `Photon3Unity3D.dll`
     (required because `AtOManager : MonoBehaviourPunCallbacks`; without these you get
     `CS0012: MonoBehaviourPunCallbacks ... not referenced`).

3. **Plugin entry point.** `Plugin.cs` is a `BaseUnityPlugin` with
   `[BepInPlugin("com.binbin.quicksave", "QuickSave", version)]`, binds config entries, and calls
   `harmony.PatchAll()` inside a try/catch in `Awake()`.

4. **Source** was reconstructed from the decompiled stock DLL (see Section 3), then fixed for the
   current API (Section 4) and hardened (Section 5).

---

## 3. Decompiling for reference (when you need to see game or mod source)

We use a tiny in-repo decompiler (`tools/AtoDecompile`, built on `ICSharpCode.Decompiler`) instead
of the `ilspycmd` global tool (which failed to install with a NuGet `DotnetToolSettings.xml` error).

```powershell
# Dump specific game manager types into refs/game-types.cs
powershell -ExecutionPolicy Bypass -File .\scripts\decompile-game-types.ps1

# Dump the currently-installed stock Quick_Save DLL into refs/QuickSave-src/
powershell -ExecutionPolicy Bypass -File .\scripts\decompile-quicksave.ps1
```

`refs/` is git-ignored (it's decompiled proprietary code and is fully regenerable). Regenerate it
after any game update so you're reading the current API. To dump different types, edit the type list
in `scripts/decompile-game-types.ps1` (it calls `AtoDecompile <dll> <outfile> <Type1> <Type2> ...`).

---

## 4. Fixing API drift (game update moved/renamed something)

Symptom: `build-mod.ps1` fails with `CS1061 '<Type>' does not contain a definition for '<Member>'`
(or `CS0117` / `CS0246`). This means a game update relocated an API our code calls. This is exactly
what the 2026-07-09 update did.

Procedure:

1. **Regenerate references** so you're looking at the new API:
   `powershell -ExecutionPolicy Bypass -File .\scripts\decompile-game-types.ps1`
   (add whatever type the error mentions to the type list in that script first, if needed).
2. **Find where the member moved.** Search `refs/game-types.cs` for the missing method name, e.g.
   `GetSaveSlot`, and note which class now owns it.
3. **Remap the call site(s)** in `src/QuickSave/*.cs`.

Known remaps from the 2026-07-09 update (already applied — use as the pattern):

| Old call (pre-update) | New call (2026-07-09+) |
|---|---|
| `AtOManager.Instance.GetSaveSlot()` | `SaveManager.Instance.GetSaveSlot()` |
| `AtOManager.Instance.SaveGame(slot, backup)` | `SaveManager.Instance.SaveGame(slot, backup)` |
| `AtOManager.Instance.LoadGame(slot, coming)` | `SaveManager.Instance.LoadGame(slot, coming)` |
| `AtOManager.Instance.SetGameId()` / `GetGameId()` | **unchanged** (still on `AtOManager`) |

If a member was *removed* entirely (no replacement), decide whether the feature is still needed. The
team/economy-reset helpers (`GetTeam`, `SetTeamFromArray`, `SetPlayerGold/Dust/Perks`) were removed;
we dropped the dead code that used them rather than reimplementing.

4. Rebuild: `powershell -ExecutionPolicy Bypass -File .\scripts\build-mod.ps1`.

---

## 5. Runtime debugging (builds fine, misbehaves in-game)

1. Reproduce in-game, then `powershell -ExecutionPolicy Bypass -File .\scripts\grab-log.ps1`
   (copies `LogOutput.log` into `diagnostics/` and prints errors/warnings/load lines).
2. Our code logs `[Debug : QuickSave] ... OnMouseUp ENTER: <button>` on every click and wraps the
   shared `BotonRollover.OnMouseUp` prefix in try/catch. Use those lines to tell apart:
   - **No `OnMouseUp ENTER` on click** → input never reached the handler (raycast/overlay blocker).
   - **`ENTER` then an `Exception in BotonRolloverOnMouseUp` line** → a null/removed-API throw; the
     try/catch keeps vanilla buttons alive, and the message names the culprit.
3. The prefix only acts on the mod's own buttons (see `ourButtons` in `QuickSavePatches.cs`) and
   null-guards every game singleton — keep that invariant when editing so a mod bug can never freeze
   all game input again.
4. Set `Enable Debugging = true` in `<profile>\BepInEx\config\com.binbin.quicksave.cfg` (or via the
   config) to get the verbose logs; turn it off for normal play.

---

## 6. Redeploy loop & reverting

- **Redeploy after every code change or after TMM overwrites us:**
  `powershell -ExecutionPolicy Bypass -File .\scripts\build-mod.ps1`
  (builds, backs up the stock DLL as `com.binbin.quicksave.dll.stock.bak` the first time, deploys).
- **Bump the version** in `src/QuickSave/Plugin.cs` (`PLUGIN_VERSION`) and `QuickSave.csproj`
  (`AssemblyVersion`/`FileVersion`) so the log clearly shows which build is loaded.
- **Revert to stock:** copy `com.binbin.quicksave.dll.stock.bak` over
  `com.binbin.quicksave.dll` in the plugin folder (or reinstall Quick_Save in TMM).

---

## 7. Version pin reference (last known-good)

| Component | Version / build | Date |
|---|---|---|
| Across the Obelisk | build `24123647`, Unity `2022.3.62f2` (`Assembly-CSharp.dll` dated 7/9/2026) | 2026-07-09 |
| BepInEx pack | 5.4.23 (loads as 5.4.21) | — |
| Our Quick_Save build | `com.binbin.quicksave` **1.2.1.2** | 2026-07-20 |
| Obeliskial Essentials / Content | 1.6.4 / 1.7.5 — **kept disabled** (incompatible) | 2026-03-23 |

If a future game update changes these, update this table after you get back to a working build.
