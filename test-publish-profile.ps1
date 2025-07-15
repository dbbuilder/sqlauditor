# Test if publish profile is valid
$ErrorActionPreference = "Stop"

$profilePath = "api/publish-profile.xml"
if (Test-Path $profilePath) {
    [xml]$profile = Get-Content $profilePath
    $webDeploy = $profile.publishData.publishProfile | Where-Object { $_.publishMethod -eq "MSDeploy" }
    
    if ($webDeploy) {
        Write-Host "Publish Profile Details:" -ForegroundColor Green
        Write-Host "  Site Name: $($webDeploy.msdeploySite)"
        Write-Host "  Publish URL: $($webDeploy.publishUrl)"
        Write-Host "  Username: $($webDeploy.userName)"
        Write-Host "  Password Length: $($webDeploy.userPWD.Length) chars"
        Write-Host "  Destination URL: $($webDeploy.destinationAppUrl)"
    } else {
        Write-Host "No MSDeploy profile found!" -ForegroundColor Red
    }
} else {
    Write-Host "Publish profile not found at $profilePath" -ForegroundColor Red
}