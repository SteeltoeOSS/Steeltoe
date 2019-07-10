# Require a PivNet API Token
Param(
    [Parameter(Mandatory=$true)]
    [string]$PivNetAPIToken
 )

 Set-Location $PSScriptRoot
 
if (-Not (Get-Command "pivnet" -ErrorAction SilentlyContinue))
{
    Write-Host "Downloading PivNet client..."
    if ($IsWindows -Or $PSVersionTable.PSVersion.Major -lt 6)
    {
        Write-Host "Running on Windows, use WebClient"
        # Download pivnet cli ... TODO: get latest instead of hardcoded version
        (New-Object System.Net.WebClient).DownloadFile("https://github.com/pivotal-cf/pivnet-cli/releases/download/v0.0.60/pivnet-windows-amd64-0.0.60", "$PSScriptRoot\pivnet.exe")  
        Write-Host "Adding alias 'pivnet' for .\pivnet.exe"
        Set-Alias -Name pivnet -Value ".\pivnet.exe"
    }
    elseif ($IsMacOS)
    {
        Write-Host "Running on MacOS, use brew"
        brew install pivotal/tap/pivnet-cli
    }
    elseif ($IsLinux)
    {
        Write-Host "Running on Linux"
        wget -O pivnet https://github.com/pivotal-cf/pivnet-cli/releases/download/v0.0.60/pivnet-linux-amd64-0.0.60
        chmod +x ./pivnet
        Write-Host "Adding alias 'pivnet' for ./pivnet"
        Set-Alias -Name pivnet -Value "./pivnet"
    }
    else
    {
        Write-Host "Unknown Host! Can't continue without pivnet cli and don't know how to get it"
        return 1
    }
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

# Download archive if it isn't already present
if (-Not (Test-Path $fileName))
{
    Write-Host "Downloading $fileName"
    pivnet download-product-files -p pivotal-gemfire -r $gemfireReleaseVersion -i $fileId
}

# 7zip isn't currently installed on MSFT hosted MacOS agents...
if ($IsMacOS -And -Not (Get-Command "7z" -ErrorAction SilentlyContinue))
{
    brew install p7zip
}

# just in case 7zip still isn't installed check first
if (Get-Command "7z" -ErrorAction SilentlyContinue)
{
    # Unzip file
    $dllLocation = Join-Path (Join-Path "pivotal-gemfire-native" "bin") "Pivotal.GemFire.dll"
    Write-Host "Use 7zip to extract $dllLocation from $fileName"
    Write-Host "7z e $fileName $dllLocation -r"
    7z e $fileName $dllLocation -r
}
else
{    
    Write-Host "7zip not found, manually extract Pivotal.GemFire.dll from $fileName to continue!"
	return 1
}