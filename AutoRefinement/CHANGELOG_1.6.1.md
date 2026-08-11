# Auto Refinement 1.6.1

First public release.

## What it does

Buildings that normally need a Duplicant standing at them run by themselves —
Metal Refinery, Rock Crusher, Glass Forge, Supermaterial Refinery, presses,
kilns, cooking stations, apothecaries, and buildings added by other mods.

By default the operate errand stays, so a Duplicant can still come and help:
**100% speed unattended, 150% with someone working it**, Machinery skill on top.
Both rates are configurable, or switch the errand off for silent automation.

The **Desalinator** dumps its own salt onto the floor instead of queueing an
errand, so an Auto-Sweeper collects it.

**Mod Options** groups everything into categories — refinement, cooking,
medicine, clothing, crafting, research, and anything else — so you can automate
some and leave others manual. Individual buildings can be excluded by ID in
`AutoRefinement.json`.

## Notes

- Nothing is written to your save. Existing colonies work, and uninstalling
  leaves no trace.
- No other mods required.
- The progress bar only appears in fully-automatic mode. It is vanilla's stand-in
  for the missing Duplicant animation, so it is suppressed while a Duplicant
  might still turn up.

## Development history

Versions 1.0.0 through 1.6.0 were never published. Two bugs worth recording,
both caught before release:

- **1.5.0** borrowed the vanilla automatic path by flipping `duplicantOperated`
  false for the duration of each `Sim200ms` call. `Sim200ms` calls
  `Operational.SetActive`; an active-state change fires `OnOperationalChanged`,
  which calls `UpdateChore` — and `UpdateChore` returns early while the flag is
  false. The operate errand was never created, so no Duplicant ever came, at any
  priority. Fixed in 1.6.0 by computing the unattended tick directly from public
  members and leaving vanilla's chore logic alone.
- The work chore is seeded with `OrderProgress` **once**, when it is created, so
  unattended progress had to re-sync `WorkTimeRemaining` or a Duplicant arriving
  mid-craft would drag it backwards. Verified in game: eight Duplicant starts
  across three buildings, all in step, including resumes at 73.3% and 94.4%.
