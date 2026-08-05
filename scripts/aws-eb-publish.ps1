# Publishes NomoAI.API and creates an Elastic Beanstalk Linux zip
# (zip root = publish output contents, not the parent folder).

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\publish-eb",
    [string]$ZipPath = ".\nomoai-api-eb-v5.zip"
)

$ErrorActionPreference = "Stop"
$apiProject = Join-Path $PSScriptRoot "..\NomoAI.API\NomoAI.API.csproj"
$apiProject = [System.IO.Path]::GetFullPath($apiProject)
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$outputRelative = $OutputDir -replace '^[.\\/]+', ''
$zipRelative = $ZipPath -replace '^[.\\/]+', ''
$outputFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $outputRelative))
$zipFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $zipRelative))
$apiRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\NomoAI.API"))
$ebExtSource = Join-Path $apiRoot ".ebextensions"
$platformSource = Join-Path $apiRoot ".platform"

Write-Host "Publishing $apiProject ..."
if (Test-Path $outputFull) {
    Remove-Item $outputFull -Recurse -Force
}
dotnet publish $apiProject -c $Configuration -o $outputFull
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

function Copy-DotFolder([string]$source, [string]$destName) {
    if (-not (Test-Path $source)) { return }
    $dest = Join-Path $outputFull $destName
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Copy-Item -Path (Join-Path $source "*") -Destination $dest -Recurse -Force
}

Copy-DotFolder $ebExtSource ".ebextensions"
Copy-DotFolder $platformSource ".platform"

# .NET 8 defaults to port 8080; Elastic Beanstalk nginx proxies to 5000.
$procfile = Join-Path $outputFull "Procfile"
$procfileContent = "web: env ASPNETCORE_URLS=http://127.0.0.1:5000 dotnet ./NomoAI.API.dll`n"
[System.IO.File]::WriteAllText($procfile, $procfileContent, [System.Text.UTF8Encoding]::new($false))

if (Test-Path $zipFull) {
    Remove-Item $zipFull -Force
}

Write-Host "Creating zip: $zipFull"
$items = @(Get-ChildItem -Force -Path $outputFull | ForEach-Object { $_.Name })
Push-Location $outputFull
try {
    & tar -a -cf $zipFull @items
    if ($LASTEXITCODE -ne 0) {
        throw "tar failed while creating zip."
    }
}
finally {
    Pop-Location
}

Write-Host "Done."
Write-Host "Upload and deploy: $zipFull"
