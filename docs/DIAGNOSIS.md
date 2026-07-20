# Quick_Save (Binbin) — TMM Debug Diagnosis

> ## RESOLVED (2026-07-20) ✅
> Quick_Save works again on game build `24123647` (2026-07-09), with the Obeliskial framework
> **disabled**, using our own rebuilt DLL (`com.binbin.quicksave` v1.2.1.1) from `src/QuickSave`.
> Vanilla buttons (Tome, settings, etc.) AND Quick_Save save/load all confirmed working in-game.
>
> **Two root causes, both fixed:**
> 1. *Fatal load crash* = Obeliskial Essentials 1.6.4 / Content 1.7.5 incompatible with the
>    2026-07-09 build (`CardData` TypeLoadException). Fixed by disabling the framework (Quick_Save
>    doesn't need it).
> 2. *All buttons dead* = **Theory T2 confirmed.** The July update moved `GetSaveSlot`, `LoadGame`,
>    `SaveGame` from `AtOManager` → `SaveManager`. The stock mod threw `MissingMethodException` on the
>    first click inside its `BotonRollover.OnMouseUp` prefix (the shared handler for ALL buttons);
>    AtO's input dispatcher swallowed it → every button silently died. Fixed by remapping those calls
>    to `SaveManager.Instance` and hardening the prefix (null-guards + try/catch + only-our-buttons).
>
> **Rebuild/deploy:** `scripts\build-mod.ps1` (stock DLL backed up as `com.binbin.quicksave.dll.stock.bak`).
> **Caveat:** updating Quick_Save in TMM overwrites our DLL — just re-run `build-mod.ps1` afterward.
> Future game updates may drift the API again → re-run `decompile-game-types.ps1`, remap, rebuild.



> **Status 2026-07-20:** Game directory cleaned to vanilla. Removed a stale **manual** BepInEx
> install + old **Quick_Save 0.9.4** that had been extracted directly into the game folder, the
> active doorstop injector (`winhttp.dll`, `doorstop_libs`, `BepInEx\`, `run_bepinex.sh`,
> `doorstop_config.ini`), the `.thunderstoremm` marker, and Quick_Save's `gamedata_*.ato` turn
> saves. All moved (not deleted) to `backups\gamedir-modfiles-*`. Next: clean reinstall via TMM,
> then verify versions before launching.


**Profile:** `Default` — `%APPDATA%\Thunderstore Mod Manager\DataFolder\AcrossTheObelisk\profiles\Default`
**Log analyzed:** `diagnostics/LogOutput.log` (copied from the profile).

## Installed set (from `mods.yml` / runtime log)

| Mod | `mods.yml` version | Runtime-loaded | Quick_Save `manifest.json` requires |
|---|---|---|---|
| BepInExPack_AcrossTheObelisk | 5.4.23 | 5.4.21 | — |
| meds-Obeliskial_Essentials | **1.6.4** | 1.6.4 | **1.5.1** |
| meds-Obeliskial_Content | **1.7.5** | 1.7.5 | **1.5.6** |
| Binbin-Quick_Save | 1.2.2 | **1.2.1** (dll) | — |

Note: the installed `com.binbin.quicksave.dll` reports **1.2.1** in the log even though TMM lists
1.2.2 — worth reinstalling/updating Quick_Save so the dll matches.

## What the log shows (root cause)

1. `Loading [QuickSave 1.2.1]` → loads OK, but immediately:
   > `com.binbin.quicksave Essentials is not installed. Mod will load normally, but Essentials
   > features are unavailable.`
   This is because **QuickSave loads *before* Essentials** in the chainloader, so its Essentials
   presence-check at `Awake` fails. (Symptom, not the fatal error.)

2. `Loading [Obeliskial_Essentials 1.6.4]` → then **FATAL**:
   ```
   TypeLoadException: Could not resolve type with token 01000028 from typeref
   (expected class 'CardData' in assembly 'Assembly-CSharp, Version=0.0.0.0, ...')
       at HarmonyLib ... PatchAll
       at Obeliskial_Essentials.Essentials.Awake ()
   ```
   Essentials' Harmony `PatchAll()` references the game type **`CardData`**, and that type
   reference **cannot be resolved in the installed `Assembly-CSharp.dll`**.

3. Cascade caused by the broken Essentials type
   (`Obeliskial_Essentials.Essentials+<>c:<>9__101_2`, a compiler-generated lambda cache whose
   type involves `CardData`):
   - `The script 'Obeliskial_Content.Content' could not be instantiated!`
   - `UnityEngine.InputSystem.InputSystem` static ctor throws (its `RegisterCustomTypes` enumerates
     loaded types, trips over the broken type) → `GameManager.Awake` input setup fails.
   - `Obeliskial_Essentials.ProfileEditor` UI fails to construct (same `CardData` typeref).

4. QuickSave's *own* Harmony patches still fire afterward (`AwakePostfix`, `ShowPostfix`,
   `LoadGameTurnPrefix` debug spam), so QuickSave's core patching is intact — but its
   Essentials-dependent integration is disabled and the Essentials/Content framework is broken.

## Conclusion

**This is not primarily a Quick_Save bug.** The fatal failure is that
**Obeliskial Essentials 1.6.4 / Content 1.7.5 are incompatible with the currently installed
Across the Obelisk build** — they were compiled against a different `Assembly-CSharp.dll`, so the
`CardData` type reference no longer resolves. Because Content and QuickSave's Essentials features
sit on top of Essentials, the whole modded stack degrades.

Secondary issue: Quick_Save's `manifest.json` pins Essentials **1.5.1** / Content **1.5.6**, but the
profile has **1.6.4 / 1.7.5** — a version drift that should be reconciled.

## CONFIRMED ROOT CAUSE (2026-07-20)

Version timeline proves a **game-update-vs-framework** incompatibility:

| Component | Version | Date |
|---|---|---|
| Across the Obelisk game | build `24123647` (Unity 2022.3.62f2) | **2026-07-09** (Assembly-CSharp.dll modified 7/9/2026; Steam LastUpdated 1783604185) |
| Obeliskial Essentials (latest on TS) | **1.6.4** | 2026-03-23 |
| Obeliskial Content (latest on TS) | **1.7.5** | 2026-03-23 |
| Quick_Save (latest on TS) | **1.2.2** | 2026-03-23 |

The game received a content patch on **2026-07-09**, ~3.5 months **after** the last framework
update (2026-03-23). That patch changed the `CardData` type (or a type it references) so Essentials
1.6.4's Harmony `PatchAll` can no longer resolve it → `TypeLoadException`.

**1.6.4 / 1.7.5 are the NEWEST versions available on Thunderstore.** A clean reinstall installs the
exact versions that crash. There is currently **no published framework fix** for the 2026-07-09 build.

Note from the original log: Quick_Save's *own* Harmony patches attach fine to the current build, and
it explicitly logs that it "will load normally" when Essentials is absent. So Quick_Save may work
**without** the framework.

## Path A result (2026-07-20): framework disabled, Quick_Save only

With Essentials + Content **disabled** (BepInEx + Quick_Save enabled), the fatal error is GONE:
clean log, no `TypeLoadException`, no InputSystem crash. Quick_Save loads and patches fine on the
2026-07-09 build (`Created DropDownTest`, `AwakePostfix`, `ShowPostfix`, `LoadGameTurnPrefix`).

**New isolated symptom:** in-game, ALL buttons are unclickable — Quick_Save's *and* vanilla ones
(settings, tome of knowledge, etc.). Only log oddity: `DontDestroyOnLoad only works for root
GameObjects`. This is the signature of a **full-screen UI raycast blocker**: Quick_Save creates its
own persistent canvas (normally parented into Essentials' UI) that, without Essentials, ends up
covering the screen with a raycast-target/`CanvasGroup.blocksRaycasts` that eats every click.
Candidate culprit: its "janky loading UI" overlay (see CHANGELOG) not being hidden, or its button
canvas being full-screen with `raycastTarget=true`.

Next step: decompile `com.binbin.quicksave.dll` to locate the canvas/overlay setup and fix it
(or reimplement). See `scripts\decompile-quicksave.ps1` → output in `refs\QuickSave-src`.

### Source analysis of the "all buttons locked" bug (decompiled `com.binbin.quicksave.cs`)

- Quick_Save Harmony-patches **`BotonRollover.OnMouseUp`** — the click handler shared by ALL AtO
  buttons (vanilla + mod). A misbehaving prefix there disables every button at once (matches symptom).
- The prefix dereferences several singletons **unguarded** before the useful work:
  `AlertManager.Instance.IsActive()`, `GameManager.Instance.IsTutorialActive()`,
  `SettingsManager.Instance.IsActive()`, `DamageMeterManager.Instance.IsActive()`. If any is null on
  the current screen/build → NRE in the prefix → original `OnMouseUp` never runs → button does nothing.
- It also uses `[HarmonyReversePatch]` copies of `BotonRollover.CloseWindows` and `fRollOut`, and calls
  `Functions.ClickedThisTransform(...)`. Reverse patches are fragile across game versions; if the
  2026-07-09 update changed those bodies/signatures, calling them can throw for every click.
- All patches applied cleanly (no PatchAll error), so target methods still exist → the break is
  runtime/behavioral, not a missing type/method.
- Secondary suspect: cloned `languageDropdown` / `CreateIcon` UI as a raycast blocker (less likely to
  kill vanilla buttons than the shared-OnMouseUp patch).

**Decisive test needed:** fresh log while clicking a vanilla button (e.g. Tome) on the map screen.
- If we see `BotonRolloverOnMouseUp ...` debug lines → clicks reach the handler (not a raycast block).
- If we see an `[Error: Unity Log] NullReferenceException` from the prefix → confirmed unguarded-null.
- If we see nothing on click → raycast blocker eating input upstream.

### CONFIRMED (2026-07-20, 2nd click-test log)

Test: new game, first map screen (no combat), clicked vanilla Tome ×3 + a Quick_Save button ×1.
Log (`diagnostics/LogOutput-20260720-154941.log`): mod loads, `AwakePostfix` + `ShowPostfix ×4`,
then NOTHING — **no `BotonRolloverOnMouseUp` line and no exception** despite the clicks.

Conclusion: clicks never reach any button handler. Two remaining theories, both mod-caused and both
requiring OUR build to fix/diagnose:
- **T1 – input/raycast blocker:** mod-created full-screen UI/collider eats clicks before buttons.
- **T2 – silent throw:** prefix on shared `BotonRollover.OnMouseUp` throws on an unguarded null
  (e.g. `DamageMeterManager.Instance`/`SettingsManager.Instance`) and AtO's input dispatcher swallows
  the exception → every button dead, no log.

Root context: the mod is only broken **without Essentials on the 2026-07-09 build**. Since the
framework is dead, the fix must be our own rebuilt DLL. Further stock-DLL logs won't help.

### Sustainable fix approach
Framework is effectively abandoned relative to game updates. Best path = **rebuild Quick_Save from the
decompiled source into a proper mod project** referencing the *current* game DLLs, fix the unguarded
prefix (null-guards + safer reverse-patch handling), build, and deploy our own DLL. This doubles as
the "reimplement" fallback and the workspace's coding context.

## Paths forward (game is now vanilla; TMM profile is empty)

- **(A) Run Quick_Save WITHOUT the framework** — install BepInEx + Quick_Save, disable/omit
  Obeliskial Essentials + Content. Cheap to test; may fully work for this QoL mod.
- **(B) Reimplement Quick_Save** as a standalone BepInEx+Harmony plugin built against the current
  (2026-07-09) `Assembly-CSharp.dll`, no framework dependency. (User's stated fallback.)
- **(C) Roll the game back** to the last pre-2026-07-09 build and disable auto-updates, to use the
  published mods as-is.

## Candidate fixes (in priority order)

1. **Align the framework to the game build.** Reinstall/update Obeliskial Essentials + Content in TMM
   to the versions built for the *currently installed* game build. Then update Quick_Save to 1.2.2 so
   the dll matches, and confirm load order (Essentials before QuickSave).
2. **If the game auto-updated past the framework:** roll the game back to the last framework-supported
   build via Steam (beta branch or `download_depot`), OR wait for a framework update. Optionally set
   AtO to "Only update this game when I launch it" and launch via TMM to avoid surprise updates.
3. **Fix the QuickSave "Essentials not installed" message** by ensuring load order / that a compatible
   Essentials is present (secondary; only matters once #1 is resolved).
4. **Fallback (Plan B): reimplement Quick_Save ourselves** as a standalone BepInEx+Harmony plugin
   built against the installed `Assembly-CSharp.dll`, with **no hard dependency on the broken
   Essentials framework**, replicating: restart-turn, multi-save-per-run, quick-load, skip-corruptor.
