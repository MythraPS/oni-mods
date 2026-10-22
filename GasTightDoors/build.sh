#!/usr/bin/env bash
# Build Gas-Tight Doors and deploy to the Dev mods folder.
set -e

ONI_MANAGED="$HOME/.local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEV="$HOME/.config/unity3d/Klei/Oxygen Not Included/mods/Dev/GasTightDoors"

echo "==> Compiling C#..."
mkdir -p "$SCRIPT_DIR/dist"
mcs \
  -target:library \
  -out:"$SCRIPT_DIR/dist/GasTightDoors.dll" \
  -optimize+ \
  -langversion:7.2 \
  -r:"$ONI_MANAGED/Assembly-CSharp.dll" \
  -r:"$ONI_MANAGED/Assembly-CSharp-firstpass.dll" \
  -r:"$ONI_MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$ONI_MANAGED/UnityEngine.dll" \
  -r:"$ONI_MANAGED/0Harmony.dll" \
  -r:"$ONI_MANAGED/netstandard.dll" \
  "$SCRIPT_DIR/src/"*.cs

echo "==> Packaging..."
cp "$SCRIPT_DIR/mod.yaml" "$SCRIPT_DIR/mod_info.yaml" "$SCRIPT_DIR/dist/"
# dist/ is the Workshop upload folder: flat, with the preview beside the DLL
[ -f "$SCRIPT_DIR/preview.png" ] && cp "$SCRIPT_DIR/preview.png" "$SCRIPT_DIR/dist/"

echo "==> Deploying to Dev/GasTightDoors ..."
mkdir -p "$DEV"
cp "$SCRIPT_DIR/dist/GasTightDoors.dll" "$SCRIPT_DIR/dist/mod.yaml" "$SCRIPT_DIR/dist/mod_info.yaml" "$DEV/"
[ -f "$SCRIPT_DIR/preview.png" ] && cp "$SCRIPT_DIR/preview.png" "$DEV/"
echo "==> Done: $DEV"
