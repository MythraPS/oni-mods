#!/usr/bin/env python3
"""
Builds the mod's recoloured kanims from the vanilla Auto-Miner art, replacing the
runtime TintColour hack — which only coloured the placed building and left the
build-menu icon / drag preview orange.

Two variants are produced, one per mod:
  critterturret_kanim   red  — CritterTurret/anim/critter_turret_anims/
  critterwrangler_kanim teal — CritterWrangler/anim/critter_wrangler_anims/

Extracts auto_miner_{anim,build}.bytes + atlas from the game's Unity assets
(UnityPy; run with ~/.venvs/oni-kanim/bin/python), renames the build, and
multiplies the atlas RGB by the variant's tint. Output goes to
each mod's own anim/ tree, which that mod's build.sh copies into dist/.

Re-run after game updates if Klei changes the Auto-Miner art.
"""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
from gen_extended_kanims import parse_build, write_build, parse_anim, write_anim

GAME_DATA = Path.home() / ".local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data"
CACHE = REPO / "tools" / "vanilla_kanim_cache" / "auto_miner"
MODS_ROOT = REPO.parent
# name -> the anim folder of the mod that ships it. The two buildings are shipped
# as separate mods, so their kanims live in separate trees.
ANIM_ROOTS = {
    "critterturret":   MODS_ROOT / "CritterTurret"   / "anim" / "critter_turret_anims",
    "critterwrangler": MODS_ROOT / "CritterWrangler" / "anim" / "critter_wrangler_anims",
}

# name -> RGB multiplier applied to the vanilla grey atlas.
VARIANTS = {
    # the brain's old TURRET_RED tint (it overrode the config's weaker one)
    "critterturret":  (1.00, 0.22, 0.18),
    # teal: reads as clearly "not the red one" at build-menu icon size, and
    # non-lethal by convention.
    "critterwrangler": (0.20, 0.80, 0.85),
}


def extract():
    import UnityPy
    CACHE.mkdir(parents=True, exist_ok=True)
    wanted_text = {"auto_miner_anim", "auto_miner_build"}
    found = {}
    for assets in sorted(GAME_DATA.glob("*.assets")):
        env = UnityPy.load(str(assets))
        for obj in env.objects:
            if obj.type.name not in ("TextAsset", "Texture2D"):
                continue
            data = obj.read()
            name = data.m_Name
            if obj.type.name == "TextAsset" and name in wanted_text:
                raw = data.m_Script
                if isinstance(raw, str):
                    raw = raw.encode("utf-8", "surrogateescape")
                (CACHE / f"{name}.bytes").write_bytes(raw)
                found[name] = assets.name
            elif obj.type.name == "Texture2D" and name.startswith("auto_miner_") \
                    and name.removeprefix("auto_miner_").isdigit():
                data.image.save(CACHE / f"{name}.png")
                found[name] = assets.name
    print("extracted:", found)
    missing = wanted_text | {"auto_miner_0"}
    missing -= found.keys()
    if missing:
        sys.exit(f"MISSING from game assets: {missing}")


def generate():
    from PIL import Image
    bd = (CACHE / "auto_miner_build.bytes").read_bytes()
    ad = (CACHE / "auto_miner_anim.bytes").read_bytes()
    assert write_build(parse_build(bd)) == bd, "build roundtrip failed"
    assert write_anim(parse_anim(ad)) == ad, "anim roundtrip failed"

    for name, tint in VARIANTS.items():
        # Re-parse per variant: write_build mutates nothing, but the dict is
        # reused and renaming it in place would leak between variants.
        build, anim = parse_build(bd), parse_anim(ad)
        build["name"] = name
        out = ANIM_ROOTS[name] / name
        out.mkdir(parents=True, exist_ok=True)
        (out / f"{name}_build.bytes").write_bytes(write_build(build))
        (out / f"{name}_anim.bytes").write_bytes(write_anim(anim))

        for src in sorted(CACHE.glob("auto_miner_*.png")):
            img = Image.open(src).convert("RGBA")
            r, g, b, a = img.split()
            r = r.point(lambda v: round(v * tint[0]))
            g = g.point(lambda v: round(v * tint[1]))
            b = b.point(lambda v: round(v * tint[2]))
            idx = src.stem.removeprefix("auto_miner_")
            Image.merge("RGBA", (r, g, b, a)).save(out / f"{name}_{idx}.png")
        print("wrote", out)


if __name__ == "__main__":
    if not (CACHE / "auto_miner_build.bytes").exists():
        extract()
    generate()
