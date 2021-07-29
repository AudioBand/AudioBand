param(
    [Parameter(Mandatory = $true)][string]$major,
    [Parameter(Mandatory = $true)][string]$minor,
    [Parameter(Mandatory = $true)][string]$patch
)

$productVersionToken = '<?define ProductVersion = "0.0.0"?>'
$productIdToken = '<?define ProductId = "{}"?>'
$upgradeCodeToken = '<?define UpgradeCode = "{}">'

$newVersion = "$major.$minor.$patch"
Write-Host "Updating version to $newVersion"

$newGuid = [guid]::NewGuid()
$upgradeCodeGuid = [guid]::NewGuid()
Write-Host "Creating new GUID $newGuid"

$targetFile = $PSScriptRoot + '/../src/AudioBandInstaller/Product.wxs'
$versionReplace = "<?define ProductVersion = '$newVersion'?>"
$guidReplace = "<?define ProductId = '$newGuid'?>"
$upgradeCodeReplace = "<?define UpgradeCode = '$upgradeCodeGuid'?>"

(Get-Content "$targetFile").replace("$productVersionToken", "$versionReplace").replace("$productIdToken", "$guidReplace").replace("$upgradeCodeToken", "$upgradeCodeReplace") | Set-Content -Encoding UTF8 "$targetFile"