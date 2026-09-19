param(
    [Parameter(Mandatory = $true)][string]$Target,
    [switch]$InstallBuildTools
)

$ErrorActionPreference = 'Stop'
$launcherRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $launcherRoot
$wahuRoot = Join-Path $repoRoot 'wuwavh_tool\Wahu'
$workRoot = Join-Path $wahuRoot '.pak_editor_build'
$source = Join-Path $wahuRoot 'wahu_editor_cli.py'
$database = Join-Path $wahuRoot 'project.db'
$icon = Join-Path $wahuRoot 'assets\app.ico'

if (-not (Test-Path -LiteralPath $source)) { throw "Thieu backend Pak Editor: $source" }
if (-not (Test-Path -LiteralPath $database)) { throw "Thieu project.db: $database" }

python -c "import PyInstaller" 2>$null
if ($LASTEXITCODE -ne 0) {
    if (-not $InstallBuildTools) {
        throw 'Chua co PyInstaller. Chay lai voi -InstallBuildTools de cai cong cu build.'
    }
    python -m pip install --upgrade pyinstaller
}

$resolvedWahu = [IO.Path]::GetFullPath($wahuRoot).TrimEnd('\') + '\'
$resolvedWork = [IO.Path]::GetFullPath($workRoot)
if (-not $resolvedWork.StartsWith($resolvedWahu, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Thu muc build nam ngoai Wahu: $resolvedWork"
}
if (Test-Path -LiteralPath $workRoot) { Remove-Item -LiteralPath $workRoot -Recurse -Force }
if (Test-Path -LiteralPath $Target) { Remove-Item -LiteralPath $Target -Recurse -Force }
New-Item -ItemType Directory -Force -Path $workRoot, $Target | Out-Null

Write-Host '   + Build WahuPakEditor.exe' -ForegroundColor Cyan
$pyInstallerArgs = @(
    '-m', 'PyInstaller', '--noconfirm', '--clean', '--onefile', '--console',
    '--name', 'WahuPakEditor', '--paths', $wahuRoot,
    '--hidden-import', 'wahu_build', '--hidden-import', 'wahu_pack',
    '--hidden-import', 'wahu_merge', '--hidden-import', 'wahu_hanviet',
    '--hidden-import', 'wahu_qa', '--hidden-import', 'quick_term_customizer',
    '--distpath', $Target,
    '--workpath', (Join-Path $workRoot 'work'),
    '--specpath', (Join-Path $workRoot 'spec')
)
if (Test-Path -LiteralPath $icon) { $pyInstallerArgs += @('--icon', $icon) }
$pyInstallerArgs += $source
& python @pyInstallerArgs
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $Target 'WahuPakEditor.exe'))) {
    throw "PyInstaller Pak Editor loi voi ma $LASTEXITCODE"
}

Write-Host '   + Tao database gon cho Dat ten nhan vat / Thuat ngu' -ForegroundColor Cyan
$requestPath = Join-Path $workRoot 'prepare-db.json'
$request = @{
    command = 'prepare-db'
    source = $database
    target = (Join-Path $Target 'project.db')
} | ConvertTo-Json -Compress
[IO.File]::WriteAllText($requestPath, $request, [Text.UTF8Encoding]::new($false))
& python $source '--request' $requestPath
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $Target 'project.db'))) {
    throw 'Khong tao duoc database Pak Editor.'
}

$avatarSource = Join-Path $wahuRoot 'ui_community\avatars'
if (-not (Test-Path -LiteralPath $avatarSource)) { throw "Thieu icon nhan vat: $avatarSource" }
Copy-Item -LiteralPath $avatarSource -Destination (Join-Path $Target 'avatars') -Recurse -Force

Write-Host '   + Chep du lieu doi chieu va tao PAK' -ForegroundColor Cyan
$supportRoot = Join-Path $Target 'build_support'
New-Item -ItemType Directory -Force -Path $supportRoot | Out-Null
$textRoot = Get-ChildItem -LiteralPath $repoRoot -Directory | Where-Object { $_.Name -like 'Text G*' } | Select-Object -First 1
if (-not $textRoot) { throw 'Khong tim thay thu muc Text Goc.' }

function FindTextDirectory([string]$Pattern) {
    $value = Get-ChildItem -LiteralPath $textRoot.FullName -Directory | Where-Object { $_.Name -like $Pattern } | Select-Object -First 1
    if (-not $value) { throw "Thieu thu muc nguon: $Pattern" }
    return $value.FullName
}

$supportItems = @(
    @{ Source = (Join-Path $wahuRoot 'data\base_vi'); Target = 'Wahu\data\base_vi' },
    @{ Source = (Join-Path $wahuRoot 'data\translations'); Target = 'Wahu\data\translations' },
    @{ Source = (Join-Path $wahuRoot 'data\translations_hv'); Target = 'Wahu\data\translations_hv' },
    @{ Source = (Join-Path $wahuRoot 'data\translations_3_6_story_final'); Target = 'Wahu\data\translations_3_6_story_final' },
    @{ Source = (Join-Path $wahuRoot '_dbcfg\Client\Content\Aki\ConfigDB\en'); Target = 'Wahu\_dbcfg\Client\Content\Aki\ConfigDB\en' },
    @{ Source = (Join-Path $wahuRoot '_dbcfg\Client\Content\Aki\ConfigDB\zh-Hans'); Target = 'Wahu\_dbcfg\Client\Content\Aki\ConfigDB\zh-Hans' },
    @{ Source = (FindTextDirectory '3.4 txt*'); Target = 'TextSource\34_source' },
    @{ Source = (FindTextDirectory '3.4 text d*'); Target = 'TextSource\34_translations' },
    @{ Source = (FindTextDirectory '3.5 txt*'); Target = 'TextSource\35_source' },
    @{ Source = (FindTextDirectory '3.5 text d*'); Target = 'TextSource\35_translations' },
    @{ Source = (FindTextDirectory '3.6 txt*'); Target = 'TextSource\36_source' },
    @{ Source = (FindTextDirectory '3.6 text d*'); Target = 'TextSource\36_translations' }
)

foreach ($item in $supportItems) {
    if (-not (Test-Path -LiteralPath $item.Source)) { throw "Thieu du lieu tao PAK: $($item.Source)" }
    $destination = Join-Path $supportRoot $item.Target
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $item.Source -Destination $destination -Recurse -Force
}

$required = @(
    (Join-Path $Target 'WahuPakEditor.exe'),
    (Join-Path $Target 'project.db'),
    ([IO.Path]::GetFullPath((Join-Path $Target '..\tools\repak.exe'))),
    (Join-Path $supportRoot 'Wahu\data\base_vi')
)
foreach ($item in $required) {
    if (-not (Test-Path -LiteralPath $item)) { throw "Pak Editor dong goi thieu: $item" }
}

if (Test-Path -LiteralPath $workRoot) { Remove-Item -LiteralPath $workRoot -Recurse -Force }
$size = [math]::Round(((Get-ChildItem -LiteralPath $Target -Recurse -File | Measure-Object Length -Sum).Sum) / 1MB, 1)
Write-Host "   + Pak Editor hoan tat ($size MB chua nen)" -ForegroundColor Green
