# Patreon post draft — ONI mods (the three without their own post)

**Title:** Three more Oxygen Not Included mods, all free on the Workshop

> Critter Turret and Longer Bridges are deliberately NOT here — they already have
> their own posts. This covers the three that never got one. Their individual
> drafts remain in each mod folder.
>
> Suggested images, in order: `AutoRefinement/preview.png`,
> `CritterWrangler/preview.png`, `MoreClothing/preview2.png`.

---

A few more Oxygen Not Included mods that never got a proper introduction, so here
they are together. **Every one is free and standalone** — none of them require
each other or anything else, and none of them write to your save, so you can add
or drop any of them mid-colony.

---

## ⚙️ Auto Refinement

Refining coal into refined carbon, crushing rock, forging glass — all of it needs
a Duplicant standing there doing nothing interesting while the machine does the
work. An Auto-Sweeper can already feed the building and take the product away.
The one thing it can't do is press the button.

Here's what I found while digging into it: **the game already supports this.**
`ComplexFabricator` carries a "no operator needed" switch, and seven vanilla
buildings already use it — the Kiln, Rubber Maker, Smoker, Chlorinator, Data
Miner, Food Dehydrator and Uranium Centrifuge all run on their own. Auto
Refinement extends that to everything else.

- ✅ **Metal Refinery, Rock Crusher, Glass Forge, Supermaterial Refinery**,
  presses, kilns, cooking stations, apothecaries — and buildings added by **other
  mods**, picked up automatically
- ✅ **Duplicants can still help.** By default the errand stays, so someone may
  wander over and work it — it just isn't required. Normal speed on its own,
  **half again as fast** with a Duplicant, Machinery skill on top. Both rates
  configurable from 1% to 500%
- ✅ Or turn the errand off entirely for **silent automation** — Duplicants
  ignore the building completely
- ✅ **The Desalinator empties itself**, dropping its salt where a Duplicant
  would have left it so an Auto-Sweeper still collects it
- ✅ **Mod Options** groups everything by category — refinement, cooking,
  medicine, clothing, crafting, research — so you automate some and leave others
  manual

👉 https://steamcommunity.com/sharedfiles/filedetails/?id=3782032937

---

## 🩵 Critter Wrangler

A **teal robo-miner fitted with a Rancher's net gun**. It catches surplus
critters at range and leaves them bagged on the ground for a Duplicant to carry
to a Critter Drop-Off — hands-off population control without killing anything.

The reason it exists is a genuine gap in the vanilla game. The Critter Drop-Off's
"auto-wrangle surplus" option **only ever fires for adults** — the check it runs
reads a flag that the Drop-Off never sets, so it sits permanently off for babies.
If you want to thin a nursery, you're marking critters by hand. The Wrangler
lists babies as their own rows and handles exactly the case vanilla can't.

- ✅ **It does the catching itself.** It doesn't flag a critter and wait for a
  Rancher to walk over — it fires the net and the critter ends up bagged. **No
  Ranching skill needed.** Duplicants still do the hauling
- ✅ **Per-species thresholds**, with **babies counted as their own rows**, so one
  overcrowded species gets thinned without touching the rest
- ✅ Pick exactly what it's allowed to catch from a **species checklist**
- ✅ **Mounts on floor, wall or ceiling** with a directional arc, like the
  Robo-Miner — line of sight required
- ✅ Critters that can never be wrangled — Morbs, adult Beetas, Shockworms — are
  skipped and left out of the count, so ticking one is a harmless no-op instead
  of jamming the building on a target it can't reach
- ✅ Green Automation signal when every selected species is at or below threshold
- ✅ Works submerged; not industrial machinery
- ✅ Deliberately unhurried — catching something alive should read as work, and at
  full speed a ranch emptied faster than you could watch

Shipping menu. Refined metal, and it never targets Duplicants.

👉 https://steamcommunity.com/sharedfiles/filedetails/?id=3775520629

---

## 🧥 More Clothing

Your dupes deserve a wardrobe. Five new garments, tailored at machines you
already own.

**At the Clothing Refashionator:**

- 🧥 **Winter Coat** — a Warm Sweater rebuilt into a thick insulated coat with
  markedly better cold protection
- 🟡 **Soft Suit** — the centrepiece: a lightweight, **checkpoint-free**
  alternative to the Atmo Suit. Cold, heat and radiation protection, sealed
  against airborne germs and wetness, a big air reserve dupes actually breathe
  from, fewer bathroom trips — and a helmet that **assembles piece by piece over
  the dupe's head** when their surroundings turn dangerous, then retracts when
  they're safe. There's no air tank, so it delays suffocation rather than
  preventing it: mid-game "good enough" gear by design
- ✨ **Snazzy Swimwear** (+25 decor) and **Snazzy Rubber Boots** (+15 decor) —
  gold-sequin upgrades of the Bionic Booster Pack garments

**At the Textile Loom:**

- 👞 **Shoes** (+15 decor) — black-and-gold dress shoes, pure flourish

The Soft Suit and Snazzy recipes use Bionic Booster Pack garments as
ingredients; Winter Coat and Shoes are DLC-free.

👉 https://steamcommunity.com/sharedfiles/filedetails/?id=3765092134

---

All three are MIT-licensed and the source for every one is here:
👉 **https://github.com/MythraPS/oni-mods**

Free for everyone, and staying that way — if you'd like to support more mods like
these, a pledge keeps the lights on. 💛 Bug reports, ideas and "could it also
do X" welcome in the comments.

*Auto Refinement bundles PLib by Peter Han (MIT) for its options screen.*
