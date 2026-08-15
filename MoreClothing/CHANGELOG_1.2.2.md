# More Clothing v1.2.2 — Soft Suit: no more false "Low Oxygen", plus Frostbite resistance

A focused pass on the Soft Suit: one long-standing annoyance fixed, one new
protection added, and a couple of numbers nudged up.

## Fixes

- **Soft Suit wearers no longer get flagged "Low Oxygen" while their breath
  meter is full.** The suit only sealed when the surrounding air *wasn't*
  oxygen — thin, low-pressure oxygen still counted as breathable, so a wearer
  standing in it kept breathing the room instead of the suit and picked up the
  Low Oxygen warning despite being in no actual danger. Thin oxygen now counts
  as unbreathable: the suit seals and switches the wearer onto suit air, so the
  warning never appears. The cutoff matches the game's own rule (the same
  threshold the duplicant's own breather uses to decide low oxygen), so it
  follows Klei's tuning instead of a hardcoded value.

- **Soft Suit wearers no longer catch "Yucky Lungs" in Polluted Oxygen.** The
  suit's "suit air" was a breathing-monitor override, not a real air supply, so
  underneath it the duplicant kept drawing gas straight out of the tile they
  were standing in. Polluted Oxygen therefore still reached their lungs, built
  up until it triggered a cough, and the cough is what applies the effect.
  Wearers now ignore the dirty-air intake entirely: no cough animation
  interrupting their work, and no Yucky Lungs. A sealed suit has no business
  inhaling the room.

- **The Soft Suit now has its own tooltip.** Because the suit is built on the
  Warm Vest, it inherited the vest's "Protects from Cold by increasing
  insulation" line, which undersold it badly. It now describes what the suit
  actually does.

## New: Frostbite resistance

The Soft Suit now **resists Frostbite**, tolerating roughly 40 °C colder
surroundings before it sets in. This is **resistance, not immunity** — a truly
extreme freeze will still get through, and the suit is still no substitute for
an Atmo Suit in the coldest biomes.

## Tweaks

- Insulation **+25 → +30** (slows heat and cold exchange both ways).
- Radiation resistance **+0.25 → +0.30** (a Lead Suit is still far better at
  0.66).
- Breath reserve is unchanged at **+200** — the suit still trades an Atmo
  Suit's oxygen tank for a longer lungful, not an endless one.

## Under the hood

- Equipment effect lookups now fail soft: an effect id that's missing or
  renamed by a game update is skipped instead of logging a fatal error and
  breaking prefab load.

---

*More Clothing is standalone. The Soft Suit and Snazzy upgrades use Bionic
Booster Pack garments as ingredients; the Winter Coat, Shoes, and Mannequin are
DLC-free.*
