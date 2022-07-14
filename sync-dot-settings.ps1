#Requires -Version 7.0

# This script copies the contents of Steeltoe.All.sln.DotSettings next to all solution files in the repository.

$masterSolutionDirectory = "src"
$masterSolutionFileName = "Steeltoe.All.sln"
$dotSettingsSuffix = ".DotSettings"
$masterSettingsPath = [IO.Path]::Combine($masterSolutionDirectory, $masterSolutionFileName + $dotSettingsSuffix)

Get-ChildItem $masterSolutionDirectory -Filter *.sln -Recurse | Foreach-Object {
	if ($_.Name -ne $masterSolutionFileName) {
		$destinationFile = "$_$dotSettingsSuffix"
		Write-Host Writing $destinationFile
		Copy-Item -Path $masterSettingsPath -Destination $destinationFile
	}
}
