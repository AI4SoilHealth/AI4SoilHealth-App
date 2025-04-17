Set-Location ..
$sourceRepo = "ai4soilhealth-app"
$sub1 = "AI4SoilHealthClient\src\common"
$sub2 = "AI4SoilHealthBackend\AI4SoilHealth.PublicAPI\Common"
$flattened = "ai4soilhealth-app-mirror"
$githubRepo = "https://github.com/AI4SoilHealth/AI4SoilHealth-App.git"

if (Test-Path $flattened) {
    Remove-Item -Recurse -Force $flattened
}

New-Item -ItemType Directory -Path $flattened | Out-Null

$excludedDirs = @(".git", "node_modules", "dist", ".vscode", ".idea", "bin", "obj")
$excludedArgs = $excludedDirs | ForEach-Object { "/XD", $_ }

$excludedFiles = @(".gitmodules", ".gitignore")
$excludedFileArgs = $excludedFiles | ForEach-Object { "/XF", $_ }

robocopy $sourceRepo $flattened /E @excludedArgs @excludedFileArgs

robocopy "$sourceRepo\$sub1" "$flattened\$sub1" /E /XD $excludedArgs
robocopy "$sourceRepo\$sub2" "$flattened\$sub2" /E /XD $excludedArgs

$binPath = Join-Path $flattened "AI4SoilHealthBackend\AI4SoilHealth.PublicAPI\bin"
if (Test-Path $binPath) {
    Write-Host "Removing $binPath"
    Remove-Item $binPath -Recurse -Force
}

$objPath = Join-Path $flattened "AI4SoilHealthBackend\AI4SoilHealth.PublicAPI\obj"
if (Test-Path $objPath) {
    Write-Host "Removing $objPath"
    Remove-Item $objPath -Recurse -Force
}

Copy-Item "$flattened\AI4SoilHealthBackend\AI4SoilHealth.PublicAPI\appsettings.json.template" "$flattened\AI4SoilHealthBackend\AI4SoilHealth.PublicAPI\appsettings.json"

Set-Location $flattened
git init
git remote add origin $githubRepo
git add .
git commit -m "Mirror commit from GitLab + submodules"
git branch -M main
git push -f origin main
