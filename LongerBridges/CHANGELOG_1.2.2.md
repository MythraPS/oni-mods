# Longer Bridges v1.2.2 — Automation bridge costs fixed

A cost fix, reported by a player. Nothing else changes.

## The fix

The longer **Automation Wire Bridges** and **Automation Ribbon Bridges** cost a
flat 50 kg of refined metal, while their 1-tile vanilla bridges cost 5 kg and
25 kg. Every other longer bridge in the mod already costs the same as its 1-tile
version; these two now do too:

| Bridge | 1 tile (vanilla) | Longer, before | Longer, now |
|---|---|---|---|
| Automation Wire Bridge | 5 kg | 50 kg | 5 kg |
| Automation Ribbon Bridge | 25 kg | 50 kg | 25 kg |

Both are now read from the vanilla bridge itself, so they follow the game if Klei
ever changes those costs.

## Notes

- **No save changes.** Bridges already built keep working and keep what they were
  built with; only new construction uses the new cost.
