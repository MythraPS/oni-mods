# More Clothing v1.2.3 — Soft Suit: panting, and germs

Reported on the Workshop by **cowsarenottoys**: a Soft Suit wearer who runs out
of breath goes off to recover and then pants "for hours and hours", long enough
to miss downtime and sleep entirely. Worse with Slimelung, worse again with the
Swimming skill.

Chasing it turned up a second, quieter problem: the suit was never actually
sealed against germs in the first place.

## Panting

Two thresholds decide how long a duplicant pants: the one that sends them off to
breathe, and the one that lets them stop. The suit was moving the wrong one.

It used to trigger the retreat at **half the tank**, and the game only lets a
duplicant stop panting when the bar is **completely full** — with a chore that
outranks everything, sleep included. On a 300-unit tank that is 150 units of
compulsory standing around, against a normal duplicant's 27.

Both ends now match an unsuited duplicant exactly:

- **The retreat fires at the same breath level it always did** — the game's own
  threshold, untouched. The suit buys a bigger tank in front of that line, not a
  different line. A wearer gets ~250 seconds of sealed working air and then the
  same ~80-second walk home everyone else gets.
- **Panting stops once they hold a full lungful for who they are** — what their
  breath bar would hold with the suit off. That is the same ~7 seconds of
  panting a normal duplicant does (~10 with Slimelung).
- **The rest of the tank refills passively** while they get on with their day,
  no chore, no interruption.

A Swimmer still pants longer than a non-Swimmer, because a Swimmer's own lung is
bigger — that is the skill's own behaviour, and the suit no longer adds to it.

## Slimelung could stop the tank refilling at all

The suit backfills its bonus reserve slowly by subtracting from the same
breath-delta Slimelung subtracts from. Together they were larger than breathing,
so an infected wearer **lost breath while standing in pure oxygen** — drain to
the retreat, pant, drain again, for the whole 37-minute infection.

The slow backfill now stands down whenever it would leave the wearer losing
breath. A sick wearer loses breath at Slimelung's rate and nothing more, exactly
like a sick duplicant with no suit on.

## The suit is now actually sealed against airborne germs

It wasn't. The description has claimed it since 1.0, and the game tag the suit
sets for it (`HasAirtightSuit`) turns out to govern mosquito bites and bionic
water damage — not breathing. Real suits avoid inhaled disease by drawing air
from a tank instead of from the tile; the Soft Suit's "suit air" is a
breathing-monitor override, not a tank, so wearers went on inhaling whatever they
stood in. Slimelung included. That is how the suited duplicants in the report
were catching it.

**Wearers no longer inhale germs at all**, for as long as the suit is on. That
covers every disease the game spreads by inhalation: Slimelung, Allergies,
Radiation Sickness and Zombie Spores. The two that don't travel that way are
unaffected — Food Poisoning is still caught by eating, Sunburn by exposure — and
Radiation Sickness and Zombie Spores can still be caught by contact, which the
suit does not claim to stop.

---

*More Clothing is standalone. The Soft Suit and Snazzy upgrades use Bionic
Booster Pack garments as ingredients; the Winter Coat, Shoes, and Mannequin are
DLC-free.*
