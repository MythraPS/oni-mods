#!/usr/bin/env bash
# Build script for the Critter Wrangler ONI mod (Mono mcs compiler).
#
# Shares two source files with the Critter Turret mod -- the targeting brain and
# the building template. Rather than keep a second copy in step by hand, they are
# read from CritterTurret/src at build time and rewritten into this mod's own
# namespace. Two separately-shipped mods must not both define
# CritterTurret.CritterTargetingBrain: users can install either or both, and
# identically-named types across two loaded assemblies are asking for trouble in
# anything that resolves types by name (KSerialization does).
set -e

ONI_MANAGED="$HOME/.steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SHARED_SRC="$SCRIPT_DIR/../CritterTurret/src"
DEV="$HOME/.config/unity3d/Klei/Oxygen Not Included/mods/Dev/CritterWrangler"
SHARED_FILES=(CritterTargetingBrain.cs CritterBuildingTemplate.cs)

echo "==> Importing shared sources from CritterTurret..."
rm -rf "$SCRIPT_DIR/obj"
mkdir -p "$SCRIPT_DIR/obj"
for f in "${SHARED_FILES[@]}"; do
  if [ ! -f "$SHARED_SRC/$f" ]; then
    echo "MISSING shared source: $SHARED_SRC/$f" >&2
    exit 1
  fi
  # Only the namespace line changes; anything else would mean the shared file has
  # grown a dependency on turret-specific code and needs looking at.
  sed 's/^namespace CritterTurret$/namespace CritterWrangler/' "$SHARED_SRC/$f" > "$SCRIPT_DIR/obj/$f"
  if grep -q "CritterTurret" "$SCRIPT_DIR/obj/$f"; then
    echo "ERROR: $f still references CritterTurret after the namespace rewrite." >&2
    echo "       A shared file has taken a dependency on the turret mod." >&2
    exit 1
  fi
  echo "    $f"
done

echo "==> Compiling C#..."
mkdir -p "$SCRIPT_DIR/dist"

mcs \
  -target:library \
  -out:"$SCRIPT_DIR/dist/CritterWrangler.dll" \
  -optimize+ \
  -langversion:7.2 \
  -r:"$ONI_MANAGED/Assembly-CSharp.dll" \
  -r:"$ONI_MANAGED/Assembly-CSharp-firstpass.dll" \
  -r:"$ONI_MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$ONI_MANAGED/UnityEngine.dll" \
  -r:"$ONI_MANAGED/0Harmony.dll" \
  -r:"$ONI_MANAGED/netstandard.dll" \
  -r:"$ONI_MANAGED/UnityEngine.InputLegacyModule.dll" \
  -r:"$ONI_MANAGED/UnityEngine.UI.dll" \
  -r:"$ONI_MANAGED/FMODUnity.dll" \
  "$SCRIPT_DIR/src/"*.cs "$SCRIPT_DIR/obj/"*.cs

echo "==> Packaging mod..."
cp "$SCRIPT_DIR/mod.yaml"      "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/mod_info.yaml" "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/preview.png"   "$SCRIPT_DIR/dist/"
rm -rf "$SCRIPT_DIR/dist/anim"
cp -r "$SCRIPT_DIR/anim"       "$SCRIPT_DIR/dist/"

echo "==> Deploying to Dev/CritterWrangler ..."
rm -rf "$DEV"
mkdir -p "$DEV"
cp -r "$SCRIPT_DIR/dist/." "$DEV/"

echo ""
echo "=== Build + deploy complete. Dev mod at: $DEV ==="
ls -1 "$DEV"
