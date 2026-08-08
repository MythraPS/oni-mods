<#
.SYNOPSIS
    Build the "Longer Bridges" release on Windows. PowerShell peer of build.sh.

.DESCRIPTION
    Longer Bridges ships TWO of this repo's own DLLs together (ONI runs every
    UserMod2 it finds in a mod folder):
      - MagpieExtension.dll          all the standalone gap bridges
      - MagpieExtensionRonivans.dll  Ronivans Legacy bridges, soft-detected

    It is fully STANDALONE: it bundles no third-party files and requires no
    other mod. Ronivans Legacy is optional; its bridges appear if present.

    Unlike build.sh this does not regenerate the kanim assets. Those are
    committed under each mod's anim/, and the generators in tools/ need a
    Python venv that only exists on the author's Linux box. Re-run them there
    when the art itself changes.

.PARAMETER GameLibsDir
    The game's OxygenNotIncluded_Data\Managed folder. Defaults to the standard
    Steam location, or $env:ONI_INSTALL_DIR if set.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -GameLibsDir "D:\SteamLibrary\...\OxygenNotIncluded_Data\Managed"
#>
[CmdletBinding()]
param(
    [string]$GameLibsDir
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root      = Split-Path -Parent $ScriptDir
$Dist      = Join-Path $ScriptDir 'dist'

# Forwarded to dotnet build only when explicitly supplied; otherwise each
# csproj falls back to ONI_INSTALL_DIR and then the default Steam path.
$buildArgs = @()
if ($GameLibsDir) { $buildArgs += "-p:GameLibsDir=$GameLibsDir" }

$mods = @('MagpieExtension', 'MagpieExtensionRonivans')

Write-Host '==> Building sibling source mods...'
foreach ($mod in $mods) {
    $proj = Join-Path $Root "$mod\src\$mod.csproj"
    if (-not (Test-Path $proj)) { throw "Project not found: $proj" }

    & dotnet build $proj -v minimal --nologo @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $mod" }
}

Write-Host '==> Assembling dist/ ...'
if (Test-Path $Dist) { Remove-Item -Recurse -Force $Dist }
New-Item -ItemType Directory -Force -Path (Join-Path $Dist 'anim') | Out-Null

foreach ($mod in $mods) {
    $dll = Join-Path $Root "$mod\dist\$mod.dll"
    if (-not (Test-Path $dll)) { throw "Expected DLL missing: $dll" }
    Copy-Item $dll -Destination $Dist
}

# Merge anim assets from BOTH extensions. Both are required: dropping the
# Ronivans anims makes ONI crash with "First anim file needs to be non-null".
foreach ($mod in $mods) {
    $animSrc = Join-Path $Root "$mod\anim"
    if (Test-Path $animSrc) {
        Copy-Item (Join-Path $animSrc '*') -Destination (Join-Path $Dist 'anim') -Recurse -Force
    }
}

Copy-Item (Join-Path $ScriptDir 'mod.yaml')      -Destination $Dist
Copy-Item (Join-Path $ScriptDir 'mod_info.yaml') -Destination $Dist

# Workshop preview image, when present.
$preview = Join-Path $ScriptDir 'preview.png'
if (Test-Path $preview) { Copy-Item $preview -Destination $Dist }

Write-Host ''
Write-Host "=== Build complete. Contents of $Dist ==="
Get-ChildItem $Dist | Select-Object -ExpandProperty Name
Write-Host ''
Write-Host 'To install as a local mod:'
Write-Host '  $local = "$env:USERPROFILE\Documents\Klei\OxygenNotIncluded\mods\Local\LongerBridges"'
Write-Host '  Remove-Item -Recurse -Force $local -ErrorAction SilentlyContinue'
Write-Host "  Copy-Item -Recurse `"$Dist`" `$local"
Write-Host ''
Write-Host 'Note: if Documents is redirected to OneDrive, use the real path the'
Write-Host 'game reads. Use Dev/ instead of Local/ when uploading to the Workshop.'
