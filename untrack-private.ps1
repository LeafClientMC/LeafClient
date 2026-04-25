$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

$patterns = @(
    "src/LeafClient/**/*.axaml",
    "src/LeafClient/Themes",
    "src/LeafClient/DarkTheme.axaml",
    "src/LeafClient/LightTheme.axaml",
    "src/LeafClient/Fonts",
    "src/LeafClient/Assets",
    "launcherassets",
    "src/LeafClient/LeafClient.csproj",
    "src/LeafClient/LeafClient.sln",
    "src/LeafClient/app.manifest",
    "src/LeafClient/rd.xml",
    "src/LeafClient/packages.config",
    "src/LeafClient/Properties",
    "src/LeafClient/build.gradle",
    "src/LeafClient/settings.gradle",
    "src/LeafClient/gradle",
    "src/LeafClient/gradle.properties",
    "src/LeafClient/gradlew",
    "src/LeafClient/gradlew.bat",
    "src/LeafClient/LeafClient-Master-Plan.md",
    "src/LeafClient/LeafClient-Master-Plan.html",
    "src/LeafClient/cosmetic-uv-templates.html",
    "src/LeafClient/angel_texture_preview.png",
    "src/LeafClient/wing_preview_fan.png",
    "src/LeafClient/wing_preview_scaled.png",
    "src/LeafClient/wing_texture_preview.png",
    "latestexe",
    "latestjars",
    "latestversion.txt",
    "Leaf Client Process.ico",
    "Leaf Client.png",
    "RELEASE-CHECKLIST.md",
    "UPTIMEROBOT-SETUP.md",
    "deploy-ssh-key.ps1",
    "NOTES.md",
    "TODO-private.md",
    "SECRETS.md",
    "ROADMAP.md",
    "docs/internal",
    "docs/launch",
    "docs/design",
    "docs/superpowers",
    ".env",
    "appsettings.Production.json",
    "launcher-config.json",
    "signing"
)

$ErrorActionPreference = "SilentlyContinue"

$csprojBackups = git ls-files | Select-String -Pattern "LeafClient\.csproj\.Backup.*\.tmp$"
foreach ($f in $csprojBackups) { $patterns += $f.Line }

$toUntrack = @()
foreach ($p in $patterns) {
    $matches = git ls-files -- $p 2>$null
    if ($matches) { $toUntrack += $matches }
}
$toUntrack = $toUntrack | Sort-Object -Unique

if ($toUntrack.Count -eq 0) {
    Write-Host "No tracked files match the private patterns. Nothing to do." -ForegroundColor Green
    exit 0
}

Write-Host "Dry run: files that would be untracked (still kept on disk):" -ForegroundColor Cyan
foreach ($f in $toUntrack) { Write-Host "  $f" }

$confirm = Read-Host "`nProceed with 'git rm -r --cached' on all matches? (type YES)"
if ($confirm -ne "YES") { Write-Host "Aborted." -ForegroundColor Yellow; exit 0 }

foreach ($p in $patterns) {
    git rm -r --cached --ignore-unmatch -- $p 2>&1 | Out-Null
}

Write-Host "`nDone. Files untracked from index, preserved on disk." -ForegroundColor Green
Write-Host "Review with: git status" -ForegroundColor Yellow
Write-Host "When you're ready, commit with: git commit -m 'chore: privatize design and build files'" -ForegroundColor Yellow
Write-Host "DO NOT PUSH until release day." -ForegroundColor Red
