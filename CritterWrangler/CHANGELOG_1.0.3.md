# Critter Wrangler v1.0.3 — Longer range and shared targeting fixes

Released alongside Critter Turret 1.2.3. Saves and settings are untouched.

## Changes

- **Longer range.** The firing area goes from 13×9 tiles to 19×11 (nine tiles to
  either side, eleven up), so one wrangler reaches across a full-width stable
  from anywhere along it. The on-screen range box updates with it.
- **A wrangled critter is released as a target immediately.** Previously the
  wrangler could spend another full wind-up aimed at a critter it had already
  caught before noticing and moving on. It now switches to the next target on the
  tick after the shot.
- **Dead critters no longer count toward the room population.** If something in
  the room has died and its corpse hasn't been cleaned up yet, it no longer
  inflates the count the threshold is measured against.

All three come from targeting code shared with the Critter Turret, so the two
buildings move together on range and on target handling.

## Not affected

- The single-kill bug fixed in Critter Turret 1.2.3 never applied here. The
  wrangler tracks caught critters through the game's own `Capturable` state
  rather than a set of its own, which is what went wrong on the turret.
- The rate-of-fire and corpse-cleanup changes in Critter Turret 1.2.3 are
  specific to the lethal turret. The wrangler catches critters rather than
  killing them, so neither applies.
