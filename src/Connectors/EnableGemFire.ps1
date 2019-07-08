# Require a PivNet API Token
Param(
    [Parameter(Mandatory=$true)]
    [string]$PivNetAPIToken
 )

if (-Not (Get-Command "pivnet.exe" -ErrorAction SilentlyContinue))
{
    Write-Host "Downloading PivNet client..."
    # Download pivnet cli ... TODO: get latest instead of hardcoded version
    (New-Object System.Net.WebClient).DownloadFile("https://github.com/pivotal-cf/pivnet-cli/releases/download/v0.0.60/pivnet-windows-amd64-0.0.60", "$PSScriptRoot\pivnet.exe")  
    Write-Host "Adding alias 'pivnet' for $PSScriptRoot\pivnet.exe"
    Set-Alias -Name pivnet -Value "$PSScriptRoot\pivnet.exe"
}

# login with API token (deprecated!)
# New way is to use Refresh token (1 hr life) to get access token -- doesn't appear possible in CI Process ???
pivnet login --api-token=$PivNetAPIToken

# Find latest (file) version - requires jq
$gemfireReleaseVersion = pivnet releases -p pivotal-gemfire --format=json | jq '[.[]|select(.version | startswith(\"Native Client 10\"))][0].version'
Write-Host "Looking for Native Client version $gemfireReleaseVersion"
$productFile = pivnet product-files -p pivotal-gemfire -r $gemfireReleaseVersion --format=json | jq '[.[]|select(.name|contains(\"Windows\"))][0]'
$fileId = $productFile | jq '.id'
Write-Host "File Id to download: $fileId"

# get file name from source like "aws_object_key": "product-files/pivotal-gemfire/pivotal-gemfire-native-10.0.2-build.9-Windows-64bit.zip"
$fileName = (($productFile | jq '.aws_object_key') -split "/")[2] -replace '"', ""
Write-Host "File Name to download: $fileName"

# Download archive
pivnet download-product-files -p pivotal-gemfire -r $gemfireReleaseVersion -i $fileId -d $PSScriptRoot

if (Get-Command "7z.exe" -ErrorAction SilentlyContinue)
{
    # Unzip file
    Write-Host "Use 7zip to extract Pivotal.GemFire.dll from $fileName to $PSScriptRoot"
    7z e "$PSScriptRoot\$fileName" pivotal-gemfire-native\bin\Pivotal.GemFire.dll -r -o"$PSScriptRoot"
}
else
{
    Write-Host "7zip not found, manually extract Pivotal.GemFire.dll from $fileName to $PSScriptRoot to continue!"
}