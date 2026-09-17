# Builds the plugin and packages it into a release zip under release/.
#
# The game's reference assemblies (libs/) are NOT committed to this repository,
# so this must run on a machine with Drag'n Wash installed and libs/ populated
# (see the .csproj comment). CI cannot build this project.
#
# Since v1.0.0 the mod runs on Drag'n Wash ModFramework, which is built from its
# own repository (a sibling folder by default) and shipped in the same zip.
#
# Usage:
#   pwsh tools/pack.ps1            # version read from Plugin.cs
#   pwsh tools/pack.ps1 -Version 0.2.0
#   pwsh tools/pack.ps1 -FrameworkPath D:\src\dragnwash-modframework
#
# Output:
#   release/DragNWashLocalization-<version>.zip
#     BepInEx/plugins/DragNWash.ModFramework*/<the framework and its libraries>.dll
#     BepInEx/patchers/DragNWash.ModFramework.Preloader.dll
#     BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll, icon.png
#     BepInEx/plugins/DragNWashLocalization/FlagCatalog.csv
#     BepInEx/plugins/DragNWashLocalization/dragnwash-menufont.bundle
#     BepInEx/plugins/DragNWashLocalization/dragnwash-menufont-LICENSE.txt
#     BepInEx/plugins/DragNWashLocalization/data/script_order.csv, level_flow.csv
#     BepInEx/plugins/DragNWashLocalization/Translations/<locale>/strings.csv
#     BepInEx/plugins/DragNWashLocalization/Translations/ignore.txt
#     Install.exe                <- Drag'n Wash ModFramework's shared installer (Windows)
#     install-steamdeck.sh       <- the same for Steam Deck / Linux: bash install-steamdeck.sh
#     mod-install.json           <- what the installers need to know about this mod
#     README.md, README.ja.md
param(
    [string]$Version,
    [string]$FrameworkPath
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root 'src/DragNWashLocalization/DragNWashLocalization.csproj'
$SrcDir = Split-Path -Parent $Project
$Dll = Join-Path $SrcDir 'bin/Release/DragNWashLocalization.dll'
$OutDir = Join-Path $Root 'release'

# 0. The version lives in two places: Plugin.PluginVersion, which the Mods
#    screen shows, and the .csproj's <Version>/<FileVersion>, which become the
#    DLL's file version and are what the installer shows. The .csproj comment
#    says to keep them in step, but nothing checked it: a release that updated
#    only one shipped a mod whose two version numbers disagree, and neither the
#    build nor any test notices. Check before anything is built.
#    <FileVersion> is checked too: it is a separate element, so it can drift on
#    its own, and it is the one Windows shows in the file properties.
#
#    A missing element is not the same as a matching one, so the two are told
#    apart. No <FileVersion> is fine: MSBuild takes it from <Version>. No
#    <Version> is not: the assembly version falls back to 1.0.0.0 - and with
#    both elements gone the file version is stamped 1.0.0.0 too - while
#    Plugin.cs still says something else. That is the very drift this check
#    exists to catch, so it must not read as "nothing to compare, carry on".
#
#    The values are trimmed: <Version> 1.1.2 </Version> is valid MSBuild and
#    builds a 1.1.2 DLL, so it must not be reported as a mismatch.
$PluginCs = Get-Content -LiteralPath (Join-Path $SrcDir 'Plugin.cs') -Raw
$PluginVersion = if ($PluginCs -match 'PluginVersion\s*=\s*"([^"]+)"') { $Matches[1].Trim() } else { $null }
$Csproj = Get-Content -LiteralPath $Project -Raw
$CsprojVersion = if ($Csproj -match '<Version>([^<]*)</Version>') { $Matches[1].Trim() } else { $null }
$CsprojFileVersion = if ($Csproj -match '<FileVersion>([^<]*)</FileVersion>') { $Matches[1].Trim() } else { $null }
if (-not $PluginVersion -and -not $Version) {
    throw 'Could not read PluginVersion from Plugin.cs. Pass -Version explicitly.'
}
if (-not $CsprojVersion) {
    throw "No <Version> in $Project. Without it the assembly version falls back to 1.0.0.0 and nothing matches Plugin.PluginVersion. Add <Version> (and <FileVersion>) and keep both in step with it."
}
$Mismatched = @()
if ($PluginVersion -and $PluginVersion -ne $CsprojVersion) {
    $Mismatched += "<Version> $CsprojVersion"
}
if ($PluginVersion -and $CsprojFileVersion -and $PluginVersion -ne $CsprojFileVersion) {
    $Mismatched += "<FileVersion> $CsprojFileVersion"
}
if ($Mismatched) {
    throw "Version mismatch: Plugin.cs says $PluginVersion but the .csproj says $($Mismatched -join ' and '). Keep <Version> and <FileVersion> in step with Plugin.PluginVersion."
}

# 1. The reference assemblies are copied from the game install and never
#    committed. Fail loudly and early with the list of what is missing.
$Required = @(
    'BepInEx.dll', '0Harmony.dll',
    'UnityEngine.CoreModule.dll', 'UnityEngine.dll',
    'Unity.TextMeshPro.dll', 'UnityEngine.UI.dll',
    'YarnSpinner.dll', 'YarnSpinner.Unity.dll', 'Yarn.Google.Protobuf.dll',
    'UnityEngine.IMGUIModule.dll', 'UnityEngine.TextRenderingModule.dll',
    'Unity.InputSystem.dll', 'UnityEngine.TextCoreFontEngineModule.dll',
    'UnityEngine.AssetBundleModule.dll', 'Naelstrof.UnityScriptableSettings.dll', 'Unity.Localization.dll'
)
$Missing = $Required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $SrcDir "libs/$_")) }
if ($Missing) {
    throw "Missing reference assemblies in src/DragNWashLocalization/libs/: $($Missing -join ', '). Copy them from your game install (see the .csproj comment)."
}

# 2. Build the framework, then the mod against it.
if (-not $FrameworkPath) {
    $FrameworkPath = Join-Path (Split-Path -Parent $Root) 'dragnwash-modframework'
}
if (-not (Test-Path -LiteralPath (Join-Path $FrameworkPath 'src/DragNWash.ModFramework'))) {
    throw "Drag'n Wash ModFramework not found at '$FrameworkPath'. Pass -FrameworkPath."
}
# The framework and the libraries the mod depends on. Every one is shipped, so
# the Mods screen never lists a library as missing. The framework's Inspector
# library is left out on purpose: this mod does not use it, and it is a tool
# for mod makers that a translation release has no reason to carry.
$FrameworkProjects = @(
    'DragNWash.ModFramework',
    'DragNWash.ModFramework.Text',
    'DragNWash.ModFramework.Dialogue',
    'DragNWash.ModFramework.ToolWindow',
    'DragNWash.ModFramework.Assets',
    'DragNWash.ModFramework.Saves'
)
foreach ($name in $FrameworkProjects + 'DragNWash.ModFramework.Preloader') {
    $proj = Join-Path $FrameworkPath "src/$name/$name.csproj"
    Write-Host "Building $proj ..."
    dotnet build $proj -c Release
    if ($LASTEXITCODE -ne 0) { throw "Build of $name failed." }
}
foreach ($name in $FrameworkProjects) {
    Copy-Item -LiteralPath (Join-Path $FrameworkPath "src/$name/bin/Release/$name.dll") -Destination (Join-Path $SrcDir 'libs') -Force
}

Write-Host "Building $Project ..."
dotnet build $Project -c Release
if ($LASTEXITCODE -ne 0) {
    throw 'Build failed.'
}

if (-not (Test-Path -LiteralPath $Dll)) {
    throw "Build succeeded but $Dll was not produced."
}

# 3. Version: explicit argument wins, otherwise the PluginVersion read in step 0,
#    which already refused to go on without one.
if (-not $Version) {
    $Version = $PluginVersion
}

# 4. Stage the files under the same layout the game expects, so extracting the
#    zip into the game root is all the user has to do.
$Stage = Join-Path $OutDir "DragNWashLocalization-$Version"
if (Test-Path -LiteralPath $Stage) {
    Remove-Item -LiteralPath $Stage -Recurse -Force
}

$PluginDir = Join-Path $Stage 'BepInEx/plugins/DragNWashLocalization'
$TranslationsDir = Join-Path $PluginDir 'Translations'
New-Item -ItemType Directory -Force -Path $TranslationsDir | Out-Null

Copy-Item -LiteralPath $Dll -Destination $PluginDir
foreach ($name in $FrameworkProjects) {
    $dir = Join-Path $Stage "BepInEx/plugins/$name"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Copy-Item -LiteralPath (Join-Path $FrameworkPath "src/$name/bin/Release/$name.dll") -Destination $dir
}
$PatcherDir = Join-Path $Stage 'BepInEx/patchers'
New-Item -ItemType Directory -Force -Path $PatcherDir | Out-Null
Copy-Item -LiteralPath (Join-Path $FrameworkPath 'src/DragNWash.ModFramework.Preloader/bin/Release/DragNWash.ModFramework.Preloader.dll') -Destination $PatcherDir
$FrameworkLicense = Join-Path $FrameworkPath 'LICENSE'
if (Test-Path -LiteralPath $FrameworkLicense) {
    Copy-Item -LiteralPath $FrameworkLicense -Destination (Join-Path $Stage 'BepInEx/plugins/DragNWash.ModFramework/LICENSE.txt')
}
# The framework's icon on the Mods screen, and its Mods button artwork.
foreach ($art in 'icon.png', 'ModsButton0.png', 'ModsButton1.png') {
    $FrameworkArt = Join-Path $FrameworkPath "src/DragNWash.ModFramework/$art"
    if (Test-Path -LiteralPath $FrameworkArt) {
        Copy-Item -LiteralPath $FrameworkArt -Destination (Join-Path $Stage 'BepInEx/plugins/DragNWash.ModFramework')
    }
}
# The framework's crash reporter (Windows), next to the core DLL, when the
# framework checkout has it (ModFramework after 1.2.1). Built deterministically.
$ReporterProject = Join-Path $FrameworkPath 'crashreporter/DragNWash.CrashReporter.csproj'
if (Test-Path -LiteralPath $ReporterProject) {
    Write-Host "Building the crash reporter ..."
    dotnet build $ReporterProject -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build of the crash reporter failed.' }
    Copy-Item -LiteralPath (Join-Path $FrameworkPath 'crashreporter/bin/Release/CrashReporter.exe') -Destination (Join-Path $Stage 'BepInEx/plugins/DragNWash.ModFramework')
}
# This mod's icon on the Mods screen (the logo by Mister ERIO).
Copy-Item -LiteralPath (Join-Path $Root 'src/DragNWashLocalization/icon.png') -Destination $PluginDir
Copy-Item -LiteralPath (Join-Path $Root 'FlagCatalog.csv') -Destination $PluginDir
# Menu font for systems whose OS fonts have no CJK glyphs (Steam Deck).
Copy-Item -LiteralPath (Join-Path $Root 'assets/menufont/dragnwash-menufont.bundle') -Destination $PluginDir
Copy-Item -LiteralPath (Join-Path $Root 'assets/menufont/OFL.txt') -Destination (Join-Path $PluginDir 'dragnwash-menufont-LICENSE.txt')
# Play-order data (node names, line ids, hashes; no English).
New-Item -ItemType Directory -Force -Path (Join-Path $PluginDir 'data') | Out-Null
Copy-Item -Path (Join-Path $Root 'data/*.csv') -Destination (Join-Path $PluginDir 'data')

# Locale folders and ignore.txt only; never the runtime _discovered/ output
# (it contains the game's own text).
$SrcTranslations = Join-Path $Root 'Translations'
Copy-Item -LiteralPath (Join-Path $SrcTranslations 'ignore.txt') -Destination $TranslationsDir
Get-ChildItem -LiteralPath $SrcTranslations -Directory |
    Where-Object { $_.Name -notlike '_*' } |
    ForEach-Object {
        $dest = Join-Path $TranslationsDir $_.Name
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        # Only the published file and the display name ship.
        Copy-Item -LiteralPath (Join-Path $_.FullName 'strings.csv') -Destination $dest
        $nameFile = Join-Path $_.FullName 'name.txt'
        if (Test-Path -LiteralPath $nameFile) { Copy-Item -LiteralPath $nameFile -Destination $dest }
        # Translated pictures (docs/TRANSLATED_TEXTURES.md): the PNGs and their credits.
        $textures = Join-Path $_.FullName 'textures'
        if (Test-Path -LiteralPath $textures) {
            $destTextures = Join-Path $dest 'textures'
            New-Item -ItemType Directory -Force -Path $destTextures | Out-Null
            Get-ChildItem -LiteralPath $textures -File |
                Where-Object { $_.Extension -eq '.png' -or $_.Name -eq 'credits.csv' } |
                ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destTextures }
        }
    }

# Both READMEs ship: the user most likely to be stuck is looking at the extracted
# folder offline, and most of them read Japanese.
Copy-Item -LiteralPath (Join-Path $Root 'README.md') -Destination $Stage
Copy-Item -LiteralPath (Join-Path $Root 'README.ja.md') -Destination $Stage

# The installers: Drag'n Wash ModFramework's shared Install.exe and
# install-steamdeck.sh, the same files every mod ships (see the framework's
# docs/INSTALLER.md). Install.exe is built deterministically, so its hash and the
# antivirus reputation that follows it stay the same from release to release.
$InstallerProject = Join-Path $FrameworkPath 'installer/DragNWash.Installer.csproj'
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $FrameworkPath 'installer/bin'), (Join-Path $FrameworkPath 'installer/obj')
dotnet build $InstallerProject -c Release
if ($LASTEXITCODE -ne 0) { throw 'Failed to build Install.exe.' }
Copy-Item -LiteralPath (Join-Path $FrameworkPath 'installer/bin/Release/Install.exe') -Destination $Stage
# Must keep LF line endings (the framework's .gitattributes).
Copy-Item -LiteralPath (Join-Path $FrameworkPath 'installer/install-steamdeck.sh') -Destination $Stage
$InstallerHash = (Get-FileHash -LiteralPath (Join-Path $Stage 'Install.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Install.exe sha256 $InstallerHash (unchanged unless the framework's installer/ or the .NET SDK changed)"

# mod-install.json: this mod's folder, the player's data the installers keep
# (translation working files and old save snapshots), the config file, and the
# language question with every pack that ships.
$Options = @()
Get-ChildItem -LiteralPath $TranslationsDir -Directory | Sort-Object Name | ForEach-Object {
    $display = $_.Name
    $nameFile = Join-Path $_.FullName 'name.txt'
    if (Test-Path -LiteralPath $nameFile) {
        $text = ([IO.File]::ReadAllText($nameFile, [Text.Encoding]::UTF8)).Trim()
        if ($text) { $display = $text }
    }
    $Options += [ordered]@{ value = $_.Name; name = $display }
}
$Options += [ordered]@{ value = 'en'; name = 'English' }
$Manifest = [ordered]@{
    schema      = 1
    name        = "Drag'n Wash Localization"
    version     = $Version
    website     = 'https://github.com/TomXV/dragnwash-localization'
    plugins     = @('DragNWashLocalization')
    keep        = @('DragNWashLocalization/Translations/_discovered', 'DragNWashLocalization/SaveHistory')
    configFiles = @('com.tomxv.dragnwash.localization.cfg')
    choices     = @([ordered]@{
        id      = 'language'
        label   = [ordered]@{ en = 'Language'; ja = '言語'; zh = '语言' }
        config  = [ordered]@{ file = 'com.tomxv.dragnwash.localization.cfg'; section = 'General'; key = 'TargetLocale' }
        options = $Options
        default = 'ui-language'
    })
}
[IO.File]::WriteAllText((Join-Path $Stage 'mod-install.json'), ($Manifest | ConvertTo-Json -Depth 6), (New-Object Text.UTF8Encoding($false)))

# 5. Zip the stage contents (so the zip root holds BepInEx/ and the READMEs).
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$Zip = Join-Path $OutDir "DragNWashLocalization-$Version.zip"
if (Test-Path -LiteralPath $Zip) {
    Remove-Item -LiteralPath $Zip -Force
}

Compress-Archive -Path (Join-Path $Stage '*') -DestinationPath $Zip

Write-Host ''
Write-Host "Created $Zip"
Write-Host 'Install: extract anywhere and double-click Install.exe (or merge BepInEx/ into the game folder by hand).'
