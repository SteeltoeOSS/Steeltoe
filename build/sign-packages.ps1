
$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if (-not $env:SignClientSecret) {
	Write-Host "Client Secret not found, not signing packages."
	return;
}

dotnet tool install --tool-path . SignClient

# Setup Variables we need to pass into the sign client tool

$appSettings = Join-Path $currentDirectory 'appsettings.json'

$nupkgs = Get-ChildItem $env:ArtifactDirectory/Steeltoe*.*nupkg -recurse | Select-Object -ExpandProperty FullName

foreach ($nupkg in $nupkgs) {
	./SignClient 'sign' -c $appSettings -i $nupkg -r $env:SignClientUser -s $env:SignClientSecret -n 'Steeltoe' -d 'Steeltoe' -u 'https://github.com/SteeltoeOSS' 
}

Write-Host "Sign-packages completed."
