# Critter Turret v1.2.1 — Visible again on older game builds

A compatibility fix. **No gameplay change** — the turret targets, charges and
fires exactly as it did in 1.2.0, and your saves are untouched.

## The fix

The mod declared a minimum game build of **744825**, which was the very newest
build at the time. Oxygen Not Included treats that number as a hard gate, not as
a hint: if your game is even one build older, the mod is filtered out before it
loads and the Mods menu shows it greyed out as **"[no compatible mod found]"**
with "No content was found to load."

The minimum is now **737790**, so the mod is available again to anyone who hasn't
updated yet or who runs the `previous_update` branch.

If Critter Turret was working fine for you, nothing about this release changes
anything — update at your leisure.

## Why it was safe to lower

The number was raised as routine housekeeping, not because the mod started
needing anything new. Checking the shipped assembly, it makes **no** Sim, DLC or
save-system calls at all — only building, entity, animation and plan-screen API,
none of which changed across the update. So there is nothing in it that a
slightly older build could fail to provide.

## Notes

- **No save changes.** Existing turrets keep their threshold, their species
  checklist and their wiring.
- The automation output port and the one-second wind-up from 1.2.0 are unchanged.
