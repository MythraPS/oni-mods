# Critter Turret v1.2.3 — Fixes the turret stopping after a single kill, plus longer range and a slower trigger

**If you are on 1.2.2, update.** On 1.2.2 the turret fires once and then goes
quiet for the rest of that species. Your saves and settings are untouched.

## The fix

1.2.2 stopped the turret overshooting its threshold by having it remember which
critters it had already shot, so a corpse waiting to be cleaned up wouldn't be
counted as living population.

That memory was a set of critters, and it wasn't storing what it looked like it
was storing. The game's `KPrefabID` — the component the set was keyed on — treats
two objects as equal when they are the same *kind* of critter, not the same
critter. So it was really a set of species. The first Puft Prince shot marked
every Puft Prince in the room as already dealt with: they dropped out of the
population count and the turret refused to aim at any of them.

The result was a turret that killed exactly one critter of a species and then sat
idle — the opposite of the bug 1.2.2 set out to fix. The set now compares
critters by identity, so a shot critter marks only itself.

The 1.2.2 threshold behaviour is otherwise unchanged: the turret stops the moment
a species reaches its threshold, counting neither corpses nor critters it has
already shot.

## Also in this release

- **Longer range.** The firing area goes from 13×9 tiles to 19×11 (nine tiles to
  either side, eleven up). One turret now reaches across a full-width stable from
  anywhere along it instead of having to sit in the middle. The on-screen range
  box updates with it — it reads from the same numbers the targeting does.
- **Slower rate of fire.** The wind-up, which is also the gap between kills, goes
  from 1 second to 3. A cull is now something you can watch happen and interrupt,
  rather than a herd vanishing between glances.
- **Corpses clear faster.** A critter shot by the turret skips the death
  animation and goes straight to being butchered. Vanilla holds a fresh corpse
  for up to 4 seconds first; that wait is gone for turret kills. **The meat still
  drops** — it is the same butcher-then-destroy path, just without the pause in
  front of it. Deaths from any other cause are untouched.

## Notes

- **No save changes**, no new settings, no change to the automation output port.
- The range change lives in shared code, so the **Critter Wrangler gets the same
  larger firing area** — see Critter Wrangler 1.0.3.
- The Critter Wrangler never had the single-kill bug: it tracks caught critters
  through the game's own `Capturable` state rather than a set of its own.
