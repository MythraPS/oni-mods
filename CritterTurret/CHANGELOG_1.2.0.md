# Critter Turret v1.2.0 — Automation output, faster wind-up

## Automation output

The turret now has a **green output port** alongside its existing on/off input.

It sends a **green signal once every selected species is at or below your
threshold**, and red while any of them is still over it. So "the ranch is back to
target population" is now something you can wire to a door, a Critter Drop-Off or
an Egg Cracker. For the opposite reading — "still culling" — put a NOT gate on it.

The signal reports the room whether or not the turret itself is powered, so you
can use one turret's output to switch another one off. Nothing nags you for a
wire if you never connect it.

## Faster wind-up

The charge before the kill shot is now **one second instead of two**. Same single
shot, same guaranteed kill — it just stops dawdling.

## Housekeeping

- Confirmed **not industrial machinery**, so it won't disqualify the room it
  stands in. (The vanilla Robo-Miner it's built from *is* tagged that way; the
  turret now explicitly clears the tag.)
- Confirmed **submersible** — it keeps working under liquid.

## Notes

- **No save changes.** Existing turrets keep their threshold, their checklist and
  their above/below setting, and pick up the output port on load.
- A non-lethal companion, **Critter Wrangler**, is now available as a separate
  mod: same chassis and controls, but it catches critters alive for a Duplicant
  to carry off instead of killing them. Neither mod requires the other.
