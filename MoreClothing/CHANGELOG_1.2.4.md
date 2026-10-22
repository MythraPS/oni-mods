# More Clothing v1.2.4 — Outfit designer fixed

Reported on the Workshop: with More Clothing on, the outfit designer showed
nothing. The preview was empty, the outfits and blueprints were gone, and the
log showed an error in `OutfitDesignerScreen.OnPrefabInit`.

## The cause

The Mannequin building's animation was named `mannequin_kanim`. The game has
its own animation with that exact name: the faceless figure the outfit
designer and blueprint screens use to preview clothes. A mod animation with the
same name replaces the game's, so those screens were loading the Mannequin
building's art instead. It doesn't have the body parts the preview needs
(`mannequin_arm`, `mannequin_body`, `mannequin_headshape`, `mannequin_leg`).

## The fix

The Mannequin's animation is now `moreclothing_mannequin_kanim`, so the game's
own preview figure is left alone. The Mannequin building uses the same art as
before.

---

*More Clothing is standalone. The Soft Suit and Snazzy upgrades use Bionic
Booster Pack garments as ingredients; the Winter Coat, Shoes, and Mannequin are
DLC-free.*
