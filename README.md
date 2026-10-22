# mythraps' Oxygen Not Included mods

Source for my [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included)
mods. Everything here is my own code, MIT-licensed (see [LICENSE](LICENSE)).

## Mods

### Auto Refinement
Steam Workshop: _pending first upload_

Buildings that normally need a Duplicant standing at them run by themselves —
Metal Refinery, Rock Crusher, Glass Forge, Supermaterial Refinery, presses,
kilns, cooking stations, apothecaries, and buildings from other mods.

- Uses the game's own `ComplexFabricator.duplicantOperated` switch, the one the
  vanilla Kiln, Rubber Maker and Uranium Centrifuge already run on.
- **Duplicants can still help**: the errand stays by default, so a building runs
  at normal speed alone and half again as fast with someone on it, Machinery
  skill included. Both rates configurable, or drop the errand entirely.
- **The Desalinator empties itself** onto the floor for an Auto-Sweeper to
  collect — it is not a fabricator, so it needed its own patch.
- **PLib Mod Options** grouped into categories; per-building exceptions by ID in
  `AutoRefinement.json`.
- Patches prefabs, not instances, and `duplicantOperated` is unserialized —
  nothing touches the save.

The only mod here that bundles a third-party assembly (PLib, MIT). See
[its README](AutoRefinement/README.md) for the internals, including two bugs
worth not repeating.

### Critter Turret
[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3756399185)

A red robo-miner that **shoots critters instead of mining rock** — automatic,
hands-off critter population control. Standalone.

- **Population threshold side screen**: only opens fire while the critter count
  in its room is above your target, then stands down.
- **Age targeting**: a button on the turret cycles Adults / Adults & Babies /
  Babies.
- **Mounts on floor, wall, or ceiling** with a directional firing arc (line of
  sight required), like the Robo-Miner.
- Fires the **duplicant multitool's attack laser** (real beam particles and
  sound); kills drop meat and resources normally.
- **Logic port** to disable it from automation. Never targets Duplicants.

Found in the **Shipping** menu next to the Robo-Miner. 120 W, refined metal.

### Gas-Tight Doors
Steam Workshop: link to follow

Doors stay gas- and liquid-tight while Duplicants walk through them.

- **Seal:** Mechanized Airlock, Manual Airlock, Insulated Door, Bunker Door.
- **Unchanged:** Pneumatic Door and Wicker Door (the game never lets these
  seal), and any door set to **Open**, so a room can still be vented on purpose.
- One Harmony prefix on `Door.SetSimState`: an opening door is handed to the
  game as closed, so the game's own closed-door code keeps the cells solid and
  handles insulation and temperature.

Written from scratch; same idea as chromiumboy's classic Self-Sealing Airlocks,
with none of its code.

### More Clothing
[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3765092134)

Five new garments (merger of the former Protective Wear + Snazzy Swimwear
projects into one mod).

- **Winter Coat** — a Warm Sweater rebuilt into a thicker insulated coat
  (+30 Insulation), at the Clothing Refashionator.
- **Soft Suit** — a lightweight, checkpoint-free Atmo Suit alternative:
  cold/heat/radiation protection, sealed against airborne germs and wetness,
  a big breath reserve dupes actually breathe from, fewer bathroom trips, and
  a helmet that assembles piece by piece when surroundings turn dangerous.
  No air tank — it delays suffocation rather than preventing it.
- **Snazzy Swimwear** (+25 decor) and **Snazzy Rubber Boots** (+15 decor) —
  gold-sequin Refashionator upgrades of the Bionic Booster Pack garments.
- **Shoes** (+15 decor) — black-and-gold dress shoes spun at the Textile Loom.

**Required:** none; standalone. The Soft Suit and Snazzy recipes use Bionic
Booster Pack garments as ingredients; Winter Coat and Shoes are DLC-free.

### Longer Bridges
[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3751316059)

A fully **standalone** bridge pack — no other mods required.

- Adds **2-tile and 3-tile gap** bridge variants for **every** utility type:
  **Liquid Pipes**, **Gas Pipes**, **Power Wires**, **Conductive Wires**,
  **Insulated Conductive Wires**, **Automation Wires**, **Automation Ribbons**,
  and **Conveyor Rails**.
- Each appears in its **normal build-menu category**, in **English**.
- If you also run **Ronivans Legacy – Industrial Revolution**, adds gap variants
  for its **Logistic Solid**, **Heavy-Duty**, and **Heavy-Duty Joint Plate**
  bridges (they appear automatically when that mod is present).

**Required:** none. **Optional:** Ronivans Legacy – Industrial Revolution.

Migrating from the old Magpie Bridge (鹊桥) setup is **seamless**: Longer Bridges'
liquid/gas/wire/conductive bridges reuse the same building IDs, so bridges you
already placed survive the switch.

This mod ships only my own DLLs and bundles no third-party files.

### Magpie Bridge Extension / Magpie Bridge Extension — Ronivans Legacy Add-on
The two source projects that Longer Bridges packages together. Kept as separate
build targets; the combined release lives in [`LongerBridges/`](LongerBridges/).

### Pacus Die Out Of Water
Pacus take damage out of water and eventually die. Useful for population control.

## Building

Each mod has a `build.sh` that compiles with Mono `mcs` against the game's
managed assemblies. For the combined release:

```bash
cd LongerBridges
./build.sh        # builds both DLLs and assembles dist/
```

Built `dist/` folders and compiled `*.dll` files are git-ignored; clone and
build to produce them.

## Publishing

ONI mods can't be published with `steamcmd` (it has no workshop depot for the
game — it fails with "no workshop depot found"), and the Steam build of the
game has **no in-game upload button** (that UI exists only on the WeGame/Rail
platform). Use the free **Oxygen Not Included Uploader** Steam tool (AppID
`636750`): point its content folder at the mod's `dist/` (which must contain
`mod_info.yaml` and a 512×512 `preview.png`), set title/description/tags, and
publish. Updates work the same way via its **Edit** button.
`LongerBridges/publish.sh` is non-functional and kept for reference only.

## Credits

- **Ronivans Legacy – Industrial Revolution** — © its author (optional add-on
  support only; no files bundled).
- **Magpie Bridge (鹊桥)** (Steam Workshop `2861126557`) — the original mod that
  inspired this one and whose building IDs Longer Bridges reuses for seamless
  save migration. © its author, 2022.
- **PLib** by Peter Han (<https://github.com/peterhaneve/ONIMods>), MIT licence —
  bundled by Auto Refinement for its options screen. PLib is designed to be
  shipped per-mod; the newest loaded copy wins.
- Klei Entertainment — Oxygen Not Included.
