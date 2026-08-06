# Longer Bridges v1.2.1 — Visible again on older game builds

A compatibility fix. **No gameplay change** — every bridge behaves exactly as it
did in 1.2.0, and your saves are untouched.

## The fix

The mod declared a minimum game build of **744825**, which was the very newest
build at the time. Oxygen Not Included treats that number as a hard gate, not as
a hint: if your game is even one build older, the mod is filtered out before it
loads and the Mods menu shows it greyed out as **"[no compatible mod found]"**
with "No content was found to load."

The minimum is now **737790**, so the mod is available again to anyone who hasn't
updated yet or who runs the `previous_update` branch.

If Longer Bridges was working fine for you, nothing about this release changes
anything — update at your leisure.

## Why it was safe to lower

The number was raised as routine housekeeping, not because the mod started
needing anything new. Checking the shipped assemblies, the only Sim call either
of them makes is `SimMessages.SetInsulation(int, float)` — a two-argument
signature with no optional parameters, so it cannot shift underneath the mod the
way `SetCellProperties` did across the U59 update. Everything else the mod
touches is building, entity and plan-screen API. The mod also shipped at 737790
for over a month before the bump.

## Notes

- **No save changes.** Placed bridges keep working; no rebuild needed.
- Reverse Bridges support, the wide heavy-watt joint plates, and Ronivans Legacy
  HP bridge variants are all unchanged from 1.2.0.
