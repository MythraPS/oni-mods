# Longer Bridges v1.2.0 — Reverse Bridges support

Longer Bridges now works with **Reverse Bridges** by Knoest, which adds a button
that flips a bridge's input and output. Requested by cowardlyllon in the
comments — thank you.

Nothing changes if you don't have Reverse Bridges installed.

## The Reverse button now appears on the gap bridges

Reverse Bridges hooks the three vanilla bridge types, so it never reached any of
the bridges this mod adds. All of them now offer the button:

- Liquid, Gas and Conveyor gap bridges (2-tile and 3-tile).
- The High Pressure, Logistic and HPA Rail gap bridges, when Ronivans Legacy –
  Industrial Revolution is installed.

Wire and automation bridges are not included — they carry no direction, so there
is nothing to reverse.

## 2-tile-gap bridges no longer jump a tile when reversed

Reversing a bridge rebuilds it turned 180° around its centre tile. A bridge only
lands back where it started if it's the same length on both sides of that tile,
which an even-length bridge can never be — so every 4-tile-long bridge slid one
tile sideways each time it was reversed. Vanilla bridges are 3 tiles long, which
is why this never came up before.

The 2-tile-gap bridges now stay exactly where they were. The 3-tile-gap ones were
never affected.

## Other mods' bridges get the button too

Bridges added by *other* mods have the same gap: Reverse Bridges doesn't know
about them either. Longer Bridges now hands the button to every bridge in the
game that doesn't already have one — Ronivans' own single-tile High Pressure
bridges, Splitters MK II, Underground Conduit, and anything else you install
later. They get the tile-shift fix as well.

This is **on by default**. If a bridge from another mod misbehaves when reversed,
you can turn it off without disabling either mod: open

    mods/LongerBridges.json

(next to `mods.json` in your Oxygen Not Included settings folder — it's created
on first launch) and set `ReverseButtonOnAllBridges` to `false`. The file lives
outside the mod folder, so updating the mod won't overwrite your choice. Longer
Bridges' own bridges keep the button either way.

## Notes

- No save changes. Existing bridges are untouched and you can update mid-colony.
- Still no dependencies and no bundled third-party files. Reverse Bridges and
  Ronivans Legacy are both detected at runtime and entirely optional.
