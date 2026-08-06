# Self-sealing Airlocks (U59 Fix) v1.0.1 — loads on older builds again

A packaging/compatibility fix. No gameplay change — doors that sealed before still seal exactly the same way.

## Fixes

- **"No content was found to load" / greyed-out "[no compatible mod found]".**
  v1.0.0 declared `minimumSupportedBuild: 740622`, so anyone on an earlier game
  build (hasn't updated yet, or on the `previous_update` branch) had the mod
  greyed out and unusable. The minimum is now **737790**, matching a broadly
  compatible build.

- **Safe on both old and new builds.** The reason the build number couldn't
  simply be lowered before: at U59-740622 the Sim calls `SetCellProperties` /
  `ClearCellProperties` gained an optional `callbackIdx` parameter, and a DLL
  compiled against that build bakes in a hard reference to the new 3-argument
  form — so on an *older* build it would load and then crash with
  `MissingMethodException` (the same failure that killed the original mod after
  the U59 update). Those two Sim calls are now resolved by reflection at load
  time and fill any trailing optional parameter with the game's own default
  (`-1`, the same value vanilla passes), so **one binary runs correctly on
  builds before and after the signature change**.

## Unchanged

Doors except the Internal type (i.e. the **Mechanized Airlock** and **Insulated
Airlock**, not the basic Manual Airlock) still seal gas and liquid while
closing/closed, so a single airlock acts as a true airlock without a two-door
vestibule. Doors pinned fully open stay vanilla.

---

*Port of chromiumboy's Self-Sealing Airlocks (via the "Revived" recompile) to
Harmony 2 and the current Sim API.*
