[CmdletBinding()]
param(
    [string]$GameDir,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Text)
    Write-Host ''
    Write-Host $Text -ForegroundColor Cyan
}

function Find-GameDir {
    $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    if (-not $steam) { return $null }
    $libs = @($steam)
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    if (Test-Path $vdf) {
        Select-String -Path $vdf -Pattern '"path"\s+"(.+?)"' -AllMatches |
            ForEach-Object { $_.Matches } |
            ForEach-Object { $libs += $_.Groups[1].Value.Replace('\\','\') }
    }
    foreach ($lib in $libs) {
        $candidate = Join-Path $lib 'steamapps\common\Sons Of The Forest'
        if (Test-Path (Join-Path $candidate 'SonsOfTheForest.exe')) { return $candidate }
    }
    return $null
}

function Get-ReleaseZip {
    param([string]$Repo, [string]$Pattern, [string]$OutFile)
    $release = Invoke-RestMethod "https://api.github.com/repos/$Repo/releases/latest" -Headers @{ 'User-Agent' = 'ps' }
    $asset = $release.assets | Where-Object { $_.name -match $Pattern } | Select-Object -First 1
    if (-not $asset) { throw "Could not find a download matching $Pattern in the latest $Repo release" }
    Write-Host "  version $($release.tag_name)"
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $OutFile -UseBasicParsing
    return $asset.name
}

try {
    Write-Host ''
    Write-Host '====================================' -ForegroundColor Green
    Write-Host ' PlayerList installer' -ForegroundColor Green
    Write-Host '====================================' -ForegroundColor Green

    if (Get-Process -Name 'SonsOfTheForest' -ErrorAction SilentlyContinue) {
        throw 'Sons of the Forest is running. Close the game and run the installer again.'
    }

    Write-Step 'Looking for Sons of the Forest'
    if (-not $GameDir) { $GameDir = Find-GameDir }
    if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir 'SonsOfTheForest.exe'))) {
        Write-Host '  Could not find the game automatically.' -ForegroundColor Yellow
        Write-Host '  In Steam, right-click Sons Of The Forest, then Manage, then Browse local files.'
        Write-Host '  Copy the folder path from the address bar and paste it below.'
        $GameDir = (Read-Host '  Game folder').Trim('"')
        if (-not (Test-Path (Join-Path $GameDir 'SonsOfTheForest.exe'))) {
            throw "SonsOfTheForest.exe was not found in $GameDir"
        }
    }
    Write-Host "  found: $GameDir" -ForegroundColor Green

    if ($Remove) {
        Write-Step 'Removing PlayerList'
        $modsDir = Join-Path $GameDir 'Mods'
        $dll = Join-Path