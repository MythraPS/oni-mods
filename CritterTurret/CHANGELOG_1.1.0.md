# Critter Turret v1.1.0 — per-species culling and a charged kill shot

Two changes to how the turret decides what to shoot and how it shoots it.

## Each species is now counted separately

The population threshold used to apply to everything you'd ticked, added
together. Six Hatches and two Pufts in one room came to eight, so a threshold of
four meant the turret culled the Pufts too — even though there were only two of
them.

Every ticked species is now counted on its own, and the turret only fires at the
ones over the threshold. With that same room and a threshold of four, the Hatches
get thinned and the Pufts are left alone.

- Babies and morphs count as their own populations, matching how they're listed
  separately in the species checklist. A room of eight Hatchlings and two Hatches
  is eight and two, not ten.
- The side screen shows the population that's driving the turret: the largest
  herd when firing above a threshold, the smallest when firing below one.
- The turret stops the moment a species drops back to its threshold, so a cull no
  longer overshoots by one.

## One charged shot instead of a three-shot burst

The turret used to fire three quick shots into a critter. It now spends two
seconds charging, then kills it outright with a single shot. The beam grows out
of the muzzle as it winds up and snaps out to the target when it fires.

**Time to kill is unchanged** — two seconds either way, whether it's a Shine Bug
or a Smooth Hatch. This is a change in how a kill looks and sounds, not in how
fast the turret clears a room.

Losing the target mid-charge discards the charge, so a critter that wanders out
of the firing arc doesn't hand a free instant kill to the next one that wanders
in.

## Notes

- No save changes; existing turrets keep their settings and pick up the new
  behaviour on load.
- Your threshold setting carries over, but it now means "per species" rather than
  "everything added up" — if you'd set it high to compensate for several species
  sharing a room, it's worth lowering.
