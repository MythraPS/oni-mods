#!/usr/bin/env bash
# Build script for the Auto Refinement ONI mod (Mono mcs compiler).
#
# Two deliberate departures from the other mods in this repo:
#
#   1. This mod bundles PLib (PeterHan, MIT) for its in-game Mod Options screen.
#      PLib is designed to be shipped per-mod -- the newest copy loaded wins -- so
#      lib/PLib.dll is committed (see the !*/lib/*.dll rule in .gitignore) and
#      copied into dist/ beside our own assembly.
#
#   2. It deploys to mods/Local/, not mods/Dev/. The tested install lives there and
#      is already enabled in mods.json; a second copy under Dev/ would load the same
#      staticID twice. If you move it, delete the other folder first.
set -e

ONI_MANAGED="$HOME/.steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY="$HOME/.config/unity3d/Klei/Oxygen Not Included/mods/Local/AutoRefinement"
UPLOAD="$HOME/Documents/ONI-Uploads/AutoRefinement"

echo "==> Compiling C#..."
rm -rf "$SCRIPT_DIR/dist"
mkdir -p "$SCRIPT_DIR/dist"

# -noconfig -nostdlib are REQUIRED: without them mcs also loads
# /usr/lib/mono/4.5/mscorlib.dll and every corlib type collides with the game's
# (CS0433 on List<T>).
mcs \
  -noconfig \
  -nostdlib \
  -target:library \
  -out:"$SCRIPT_DIR/dist/AutoRefinement.dll" \
  -optimize+ \
  -langversion:7.2 \
  -r:"$ONI_MANAGED/mscorlib.dll" \
  -r:"$ONI_MANAGED/System.dll" \
  -r:"$ONI_MANAGED/System.Core.dll" \
  -r:"$ONI_MANAGED/netstandard.dll" \
  -r:"$ONI_MANAGED/Assembly-CSharp.dll" \
  -r:"$ONI_MANAGED/Assembly-CSharp-firstpass.dll" \
  -r:"$ONI_MANAGED/UnityEngine.dll" \
  -r:"$ONI_MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$ONI_MANAGED/0Harmony.dll" \
  -r:"$ONI_MANAGED/Newtonsoft.Json.dll" \
  -r:"$SCRIPT_DIR/lib/PLib.dll" \
  "$SCRIPT_DIR/src/"*.cs

echo "==> Packaging mod..."
cp "$SCRIPT_DIR/lib/PLib.dll"  "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/mod.yaml"      "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/mod_info.yaml" "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/preview.png"   "$SCRIPT_DIR/dist/"

echo "==> Deploying to $DEPLOY ..."
mkdir -p "$DEPLOY"
cp "$SCRIPT_DIR/dist/"* "$DEPLOY/"

# The uploader's content folder must be FLAT -- assembly beside mod_info.yaml, no
# src/, dist/ or build.sh -- and must sit somewhere the GTK file dialog can reach,
# which ~/.config is not.
echo "==> Staging flat upload folder at $UPLOAD ..."
rm -rf "$UPLOAD"
mkdir -p "$UPLOAD"
cp "$SCRIPT_DIR/dist/"* "$UPLOAD/"

echo ""
echo "=== Build complete ==="
echo "Deployed:  $DEPLOY"
echo "Upload me: $UPLOAD"
ls -1 "$UPLOAD"
