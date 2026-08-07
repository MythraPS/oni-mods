# Critter Turret v1.2.2 — Stops at the threshold instead of wiping the herd

**Bug fix.** The turret could overshoot its population threshold badly — in the
worst case killing every critter of a species in the room when it had been told
to leave four. Your saves and settings are untouched; existing turrets keep their
threshold, species checklist and wiring.

## What was wrong

The turret fires once per second and re-reads the room's population between
shots, so it should have stopped as soon as the count came down. It didn't,
because the count wasn't coming down.

A critter leaves the room's creature list when its object is **destroyed**, not
when its hit points reach zero. Between the shot landing and the corpse being
cleaned up, the game still reports that critter as living population. So every
shot after the first was aimed using a number that still counted the ones already
lying dead on the floor — the threshold was never reached, and the turret kept
going until there was nothing left to shoot.

The faster the critters died, the worse it got. Small herds could be wiped out
entirely.

## The fix

Two changes, either of which would help; together they make the count exact.

- **The turret remembers what it has shot.** A critter stops counting toward the
  threshold the moment the discharge lands, rather than whenever the game gets
  around to removing the corpse. This is the same approach the Critter Wrangler
  already used for critters it had bagged.
- **Corpses no longer count as population.** Anything already dead is skipped
  when tallying the room, so critters killed by anything else — starvation, a
  second turret, a duplicant — don't inflate the count either.

Also fixed: the turret could spend a full second winding up on a critter it had
just shot, because the kill hadn't been registered yet. It now releases a spent
target immediately and moves to the next one.

## Notes

- **No save changes**, no new settings, no change to the automation output port.
- The Critter Wrangler is unaffected by the overshoot bug — it already excluded
  critters it had caught — but it picks up the released-target fix too.
