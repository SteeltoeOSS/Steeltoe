param(
    [Parameter(Mandatory = $true)][string]$CoverageFile,
    [Parameter(Mandatory = $true)][string]$CanonicalPrefix
)

# Each dotnet build/publish subprocess spawned by GitProperties.Build.Test runs against its own temporary copy of the
# repo, so the PDB (and therefore the coverage report) embeds an ephemeral temp path per test run instead of the real
# checkout path, and dotnet-coverage records one separate <class> entry per subprocess rather than recognizing them
# as the same logical file. This script rewrites the paths to the real checkout path, then merges the resulting
# duplicate <class> entries by summing their line hits. Without that merge, tools that correlate coverage by file
# path (SonarCloud) or re-aggregate reports (ReportGenerator's Cobertura export) both pick an arbitrary "last one
# wins" entry instead of the union of what every subprocess actually executed.
Add-Type -AssemblyName System.Xml.Linq

$pattern = '[^"]*[\\/]src[\\/]Management[\\/]src[\\/]GitProperties\.Build[\\/]'
$replacement = "$CanonicalPrefix/"

$content = Get-Content -Path $CoverageFile -Raw
$content = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)

$doc = [System.Xml.Linq.XDocument]::Parse($content)
$invariantCulture = [System.Globalization.CultureInfo]::InvariantCulture

function Get-LineRate($lines) {
    $lines = @($lines)

    if ($lines.Count -eq 0) {
        return "0"
    }

    $covered = @($lines | Where-Object { [int]$_.Attribute("hits").Value -gt 0 }).Count
    return ($covered / $lines.Count).ToString($invariantCulture)
}

foreach ($package in $doc.Descendants("package")) {
    $classesElement = $package.Element("classes")

    if ($null -eq $classesElement) {
        continue
    }

    $classGroups = $classesElement.Elements("class") | Group-Object {
        $_.Attribute("name").Value + "|" + $_.Attribute("filename").Value
    }

    foreach ($group in $classGroups) {
        $keptClass = $group.Group[0]

        if ($group.Count -gt 1) {
            $lineHits = @{}

            foreach ($class in $group.Group) {
                foreach ($line in $class.Element("lines").Elements("line")) {
                    $number = $line.Attribute("number").Value
                    $hits = [int]$line.Attribute("hits").Value

                    if ($lineHits.ContainsKey($number)) {
                        $lineHits[$number] += $hits
                    }
                    else {
                        $lineHits[$number] = $hits
                    }
                }
            }

            foreach ($line in $keptClass.Descendants("line")) {
                $line.Attribute("hits").Value = [string]$lineHits[$line.Attribute("number").Value]
            }

            foreach ($duplicate in $group.Group | Select-Object -Skip 1) {
                $duplicate.Remove()
            }
        }

        $keptClass.Attribute("line-rate").Value = Get-LineRate $keptClass.Element("lines").Elements("line")
    }

    $packageLines = $classesElement.Elements("class").Element("lines").Elements("line")
    $package.Attribute("line-rate").Value = Get-LineRate $packageLines
}

$allLines = @($doc.Descendants("class") | ForEach-Object { $_.Element("lines").Elements("line") })
$doc.Root.Attribute("lines-valid").Value = [string]$allLines.Count
$doc.Root.Attribute("lines-covered").Value = [string]@($allLines | Where-Object { [int]$_.Attribute("hits").Value -gt 0 }).Count
$doc.Root.Attribute("line-rate").Value = Get-LineRate $allLines

$doc.Save($CoverageFile)
