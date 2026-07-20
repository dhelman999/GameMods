# Across the Obelisk (AtO) — Modding Coding Context

This file is the persistent context for developing and debugging **Across the Obelisk** mods
in this workspace. Read it before doing modding work here.

## Game

- **Title:** Across the Obelisk (Unity deck-building roguelike).
- **Engine:** Unity `2022.3.62` (Mono backend, .NET Framework-style `mscorlib` 4.x).
- **Steam AppID:** `1385380`.
- **Install path:** `C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk`
- **Managed assemblies:** `...\Across the Obelisk\AcrossTheObelisk_Data\Managed`
  - Game code lives in **`Assembly-CSharp.dll`** (all gameplay types: `CardData`, `GameManager`,
    `Card`, `Hero`, `Combat`, etc.). JSON via bundled `Newtonsoft.Json.dll`.
  - There is **no** `Assembly-CSharp-firstpass.dll` in this build.
  - Networking is Photon (`PhotonUnityNetworking.dll`) + Steamworks (`Facepunch.Steamworks.Win64.dll`).

## Mod stack

Mods are **C# .NET class libraries (`.dll`)** loaded at runtime by:

- **BepInEx 5.4.21** (mono) — plugin loader. Installed via the Thunderstore
  `BepInEx-BepInExPack_AcrossTheObelisk` pack (profile pins `5.4.23` in `mods.yml`,
  runtime log reports the actual loaded `5.4.21`).
- **HarmonyX** (`0Harmony.dll` / `0Harmony20.dll`, `HarmonyXInterop.dll`) — runtime method patching
  (Prefix/Postfix/Transpiler). This is how mods change game behavior without editing game files.
- **Obeliskial Essentials** — shared framework: mod registry, config manager (sinai-dev
  `BepInExConfigManager`, F5), UI helpers, dev tools (F1/F2), profile editor. Originally by
  `stiffmeds`, now maintained by `binbinmods`.
- **Obeliskial Content** — custom-content loader (cards/items/heroes/traits via JSON in
  `Obeliskial_importing`). Depends on Essentials.

### Canonical reference repos (author of Quick_Save = Binbin = `binbinmods`)

- `binbinmods/Obeliskial-Essentials` — framework source (reference for game API + patch patterns).
- `binbinmods/SampleCSharpWorkspace` — starter C# workspace for a mod.
- `binbinmods/Damali`, `binbinmods/Rosalinde`, `stiffmeds/TraitMod` — example "effect trait" mods.
- `binbinmods/Obeliskial_exported` — exported game/content data.
- Quick_Save source: `github.com/binbinmods/QualityUpdates` (per its `manifest.json` website_url).
- Community how-to: `code.secretsisters.gay/AtO_How_To`.

## Building a mod (VS / dotnet)

1. `.NET class library` targeting a framework compatible with Unity Mono (typically `netstandard2.0`
   or `net472`).
2. Add **assembly references** (copy-local = false) to:
   - `Assembly-CSharp.dll` (from the game's `Managed` folder — the installed build!),
   - `0Harmony.dll`, `BepInEx.dll` (from the BepInEx `core` folder),
   - `Obeliskial Essentials.dll`, `Obeliskial Content.dll` (from the installed plugins),
   - relevant `UnityEngine.*Module.dll` (e.g. `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`).
   - **Always reference the DLLs from the game version you are targeting** — mismatches cause
     `TypeLoadException` at load (see `DIAGNOSIS.md`).
3. A BepInEx plugin: `[BepInPlugin(guid, name, version)]` on a `BasePlugin`/`BaseUnityPlugin`
   subclass; in `Awake()` create a `Harmony` instance and `PatchAll()`.
4. Build → drop the `.dll` into the profile's `BepInEx\plugins\<Author>-<Mod>\` folder.

Tooling available on this machine: **Visual Studio**, **dotnet SDK 9.0.304**.
Decompiler for inspecting game types: install `ilspycmd` (`dotnet tool install -g ilspycmd`) or use dnSpy/ILSpy GUI.

## Thunderstore Mod Manager (TMM)

- TMM data root: `%APPDATA%\Thunderstore Mod Manager\DataFolder`
- AtO profiles: `...\DataFolder\AcrossTheObelisk\profiles\<Profile>` (active profile: **`Default`**).
- Inside a profile:
  - `BepInEx\plugins\<Author>-<Mod>\` — installed mods (each with `manifest.json`).
  - `BepInEx\core\` — BepInEx + Harmony DLLs.
  - `BepInEx\config\` — per-mod config.
  - **`BepInEx\LogOutput.log`** — the primary runtime log (read this first when debugging).
  - `mods.yml` — TMM's record of installed mods, versions, enabled state, load order.
  - `doorstop_config.ini`, `winhttp.dll` — the injector; the game is launched *through* TMM so
    doorstop points at this profile.
- TMM launches the game with the profile injected. Manual installs instead extract BepInEx into the
  game folder directly.

## Distribution

- Thunderstore community: `https://thunderstore.io/c/across-the-obelisk/`
- A package is a zip with `manifest.json` (namespace, name, version_number, dependencies[], website_url),
  `README.md`, `CHANGELOG.md`, `icon.png` (256x256), and the plugin `.dll`.
- Thunderstore API docs: `https://thunderstore.io/api/docs/` (Swagger; some endpoints need login).
  Public package listing: `https://thunderstore.io/c/across-the-obelisk/api/v1/package/`.

## Debugging checklist

1. Read `BepInEx\LogOutput.log`. Look for `[Error` / `[Warning` and `Loading [<Plugin> <ver>]` lines.
2. Confirm plugin **load order** (Essentials/Content should load before dependents; QuickSave checks
   for Essentials at its own `Awake`).
3. `TypeLoadException: Could not resolve type ... 'CardData' in 'Assembly-CSharp'` ⇒ a mod was built
   against a **different game build** than installed → align mod & game versions.
4. Verify dependency versions in each mod's `manifest.json` vs what's installed in `mods.yml`.
5. Reproduce with a minimal profile (only BepInEx + Essentials + Content) to isolate.
