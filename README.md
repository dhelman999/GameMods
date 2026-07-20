# GameMods

Generic workspace for **game mod / script development** across multiple titles.

Each game is a self-contained subproject under this repo. Shared tooling or conventions can live at the
root later; today the only game subproject is Across the Obelisk.

## Layout

| Path | Purpose |
|------|---------|
| `AcrossTheObelisk/` | Across the Obelisk BepInEx mods (Quick_Save rebuild, scripts, docs). |
| *(future)* `<Game>/` | Same pattern for the next title. |

## Across the Obelisk

See [`AcrossTheObelisk/README.md`](AcrossTheObelisk/README.md) for build/deploy and
[`AcrossTheObelisk/docs/REBUILD.md`](AcrossTheObelisk/docs/REBUILD.md) for recovery after a game or
Thunderstore update breaks a mod.
