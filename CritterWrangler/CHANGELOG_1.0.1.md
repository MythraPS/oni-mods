# Critter Wrangler v1.0.1 — Visible again on older game builds

A compatibility fix. **No gameplay change** — wrangling behaves exactly as it did
in 1.0.0, and your saves are untouched.

## The fix

The mod declared a minimum game build of **744825**, which was the very newest
build at the time. Oxygen Not Included treats that number as a hard gate, not as
a hint: if your game is even one build older, the mod is filtered out before it
loads and the Mods menu shows it greyed out as **"[no compatible mod found]"**
with "No content was found to load."

The minimum is now **737790**, so the mod is available again to anyone who hasn't
updated yet or who runs the `previous_update` branch.

Since this was the case from the mod's first release, anyone not on the newest
build would have seen it as unusable from day one. If that was you, this fixes
it — nothing was wrong on your end.

## Why it was safe to lower

The mod never needed a newer build. Checking the shipped assembly, it makes **no**
Sim, DLC or save-system calls at all — only building, entity, animation and
plan-screen API, none of which changed across the update.

## Notes

- **No save changes.** Placed wranglers keep their settings.
