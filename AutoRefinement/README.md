# Auto Refinement

Buildings that normally need a Duplicant standing at them run by themselves.

## How it works

This is not a new mechanic. `ComplexFabricator` carries a public
`duplicantOperated` flag, and when it is false `Sim200ms` advances the craft
itself, `UpdateChore` returns before creating an operate errand, and `HasWorker`
reports true so the state machine never stalls waiting for someone. Seven vanilla
buildings already ship that way — Chlorinator, Data Miner, Food Dehydrator, Kiln,
Rubber Maker, Smoker and Uranium Centrifuge. The mod extends the set.

Buildings are matched by **prefab ID**, patched on the prefab rather than per
instance. `duplicantOperated` has no `[Serialize]` attribute, so every instance
takes its value from the prefab and nothing is written to the save — existing
colonies work, and uninstalling leaves no trace.

The pass runs from a postfix on `GeneratedBuildings.LoadGeneratedBuildings`, by
which point `Assets.BuildingDefs` holds every def from the game and from every
other mod. That is why modded refineries are covered without referencing those
mods.

## Two modes

**Assisted** (default). `duplicantOperated` stays true, so the errand still
exists and Duplicants may work the building — it just isn't required. Unattended
progress comes from a postfix on `Sim200ms` that advances `OrderProgress` at a
configurable fraction of `dt / recipe.time`.

Both work paths share one value: `CreateChore` seeds the work chore with
`OrderProgress`, and `ComplexFabricatorWorkable.UpdateOrderProgress` writes it
back as the Duplicant works. The seed happens **once, at chore creation**, so the
unattended tick re-syncs `workable.WorkTimeRemaining` each time — without that a
Duplicant arriving mid-craft would resume from a stale seed and drag the progress
backwards.

A postfix on `get_HasWorker` reports true for assisted buildings so they play
their working animation and don't claim to be waiting for someone. That is safe
because `HasWorker` is read in exactly two places — `Sim200ms`, where it drives
`Operational.SetActive`, and `WaitingForWorker`, which drives the status text.
Neither creates the errand; `UpdateChore` keys off `IsOperational` and
`HasWorkingOrder`.

**Fully automatic.** `duplicantOperated` is set false. No errand at all, and the
building shows a progress bar in place of the Duplicant animation, exactly as the
vanilla Kiln does.

> Do **not** flip `duplicantOperated` temporarily inside a `Sim200ms` patch to
> borrow the vanilla automatic path. `Sim200ms` calls `Operational.SetActive`, an
> active-state change fires `OnOperationalChanged`, and that calls `UpdateChore`,
> which returns early while the flag is false. The errand is then never created
> and no Duplicant ever comes. This mod did that in 1.5.0 and it was wrong.

## The Desalinator

Not a `ComplexFabricator` at all — it is an `ElementConverter` plus its own state
machine, and its salt is removed by a `WorkChore<DesalinatorWorkableEmpty>` under
`ChoreTypes.EmptyDesalinator`. There is no flag to switch.

Instead, a prefix on `Desalinator.StatesInstance.CreateEmptyChore` invokes the
private `OnEmptyComplete(Chore)` and skips the original. That method ignores its
`chore` argument entirely: it nulls `emptyChore` and drops every Salt item in
storage. Calling it directly *is* "a Duplicant just emptied this". The salt lands
on the floor where a Duplicant would have left it, so an Auto-Sweeper still
collects it. Falls back to the vanilla errand if the reflection ever fails.

## Configuration

In-game **Mod Options** (the gear icon in the Mods menu), via PLib. Buildings are
grouped into seven categories — refinement, cooking, medicine, clothing,
crafting, research, and a catch-all for anything unrecognised, which is where
other mods' buildings land unless listed.

PLib renders scalar properties only, so a list of prefab IDs cannot be a control.
Per-building exceptions therefore live in `ManualBuildings` in
`AutoRefinement.json` beside the mod, and always win over the category toggles.
The Player log prints every affected ID, quoted and comma-terminated, ready to
paste.

Categories render in alphabetical order — PLib keeps them in a `SortedList` — so
the names are chosen to read top to bottom: Automatic buildings, Duplicant help,
Other settings.

## Building

```sh
./build.sh
```

Compiles with `mcs`, packages `dist/`, deploys to `mods/Local/AutoRefinement`,
and stages a flat upload folder at `~/Documents/ONI-Uploads/AutoRefinement`.

`-noconfig -nostdlib` are required, or `mcs` also loads
`/usr/lib/mono/4.5/mscorlib.dll` and every corlib type collides with the game's.

## Credits

Bundles [PLib](https://github.com/peterhaneve/ONIMods) by Peter Han, MIT licence,
for the options screen. PLib is designed to be shipped per-mod; the newest loaded
copy wins.
