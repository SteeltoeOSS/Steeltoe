param(
    [Parameter(Mandatory = $true)][string]$CoverageFile,
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [Parameter(Mandatory = $true)][string]$ProjectRelativePath
)

# Some test suites (e.g. GitProperties.Build.Test) run dotnet build/publish subprocesses against their own temporary
# copy of the repo, so the PDB (and therefore the coverage report) embeds an ephemeral temp path per test run instead
# of the real checkout path, and dotnet-coverage records one separate <class> entry per subprocess rather than
# recognizing them as the same logical file. This script rewrites any path ending in $ProjectRelativePath to the real
# checkout path, then merges the resulting duplicate <class> entries: line hits are summed, and each branch
# <condition> (always a binary jump, so its "coverage" is always 0%/50%/100%) takes the maximum observed across
# subprocesses. Without that merge, tools that correlate coverage by file path (SonarCloud) or re-aggregate reports
# (ReportGenerator's Cobertura export) both pick an arbitrary "last one wins" entry instead of the union of what
# every subprocess actually executed.
#
# Branch merging is a best-effort approximation: taking the max per condition cannot distinguish "two subprocesses
# both took the same half of a 50/50 branch" from "they took different halves", so it can still undercount versus a
# true union, but it never overstates coverage and is strictly more accurate than last-one-wins.
Add-Type -AssemblyName System.Xml.Linq

$normalizedRelativePath = $ProjectRelativePath.Replace('\', '/').Trim('/')
$escapedSegments = $normalizedRelativePath -split '/' | ForEach-Object { [Regex]::Escape($_) }
$pattern = '[^"]*[\\/]' + ($escapedSegments -join '[\\/]') + '[\\/]'
$canonicalPrefix = ($RepoRoot.TrimEnd('/', '\').Replace('\', '/')) + '/' + $normalizedRelativePath
$replacement = "$canonicalPrefix/"

$content = Get-Content -Path $CoverageFile -Raw
$content = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)

$doc = [System.Xml.Linq.XDocument]::Parse($content)
$invariantCulture = [System.Globalization.CultureInfo]::InvariantCulture

function Format-Percentage([double]$value) {
    $rounded = [Math]::Round($value, 2)

    if ($rounded -eq [Math]::Floor($rounded)) {
        return "$([int]$rounded)%"
    }

    return "$($rounded.ToString($invariantCulture))%"
}

function Get-LineRate($lines) {
    $lines = @($lines)

    if ($lines.Count -eq 0) {
        return "0"
    }

    $covered = @($lines | Where-Object { [int]$_.Attribute("hits").Value -gt 0 }).Count
    return ($covered / $lines.Count).ToString($invariantCulture)
}

function Get-BranchCounts($lines) {
    $total = 0
    $covered = 0

    foreach ($line in ($lines | Where-Object { $_.Attribute("branch").Value -eq "True" })) {
        $conditionsElement = $line.Element("conditions")

        if ($null -eq $conditionsElement) {
            continue
        }

        foreach ($condition in $conditionsElement.Elements("condition")) {
            $percentage = [double]($condition.Attribute("coverage").Value.TrimEnd('%'))
            $total += 2
            $covered += [Math]::Round($percentage / 100 * 2)
        }
    }

    return [PSCustomObject]@{ Total = $total; Covered = $covered }
}

function Get-BranchRate($lines) {
    $counts = Get-BranchCounts $lines

    if ($counts.Total -eq 0) {
        return "1"
    }

    return ($counts.Covered / $counts.Total).ToString($invariantCulture)
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
            $conditionCoverage = @{}

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

                    $conditionsElement = $line.Element("conditions")

                    if ($null -eq $conditionsElement) {
                        continue
                    }

                    foreach ($condition in $conditionsElement.Elements("condition")) {
                        $key = "$number|$($condition.Attribute("number").Value)"
                        $percentage = [double]($condition.Attribute("coverage").Value.TrimEnd('%'))

                        if (-not $conditionCoverage.ContainsKey($key) -or $percentage -gt $conditionCoverage[$key]) {
                            $conditionCoverage[$key] = $percentage
                        }
                    }
                }
            }

            foreach ($line in $keptClass.Descendants("line")) {
                $line.SetAttributeValue("hits", [string]$lineHits[$line.Attribute("number").Value])

                $conditionsElement = $line.Element("conditions")

                if ($null -eq $conditionsElement) {
                    continue
                }

                $lineTotal = 0
                $lineCovered = 0

                foreach ($condition in $conditionsElement.Elements("condition")) {
                    $key = "$($line.Attribute("number").Value)|$($condition.Attribute("number").Value)"
                    $percentage = $conditionCoverage[$key]
                    $condition.SetAttributeValue("coverage", (Format-Percentage $percentage))
                    $lineTotal += 2
                    $lineCovered += [Math]::Round($percentage / 100 * 2)
                }

                $lineCoverageDescription = "$(Format-Percentage (($lineCovered / $lineTotal) * 100)) ($lineCovered/$lineTotal)"
                $line.SetAttributeValue("condition-coverage", $lineCoverageDescription)
            }

            foreach ($duplicate in $group.Group | Select-Object -Skip 1) {
                $duplicate.Remove()
            }
        }

        $keptClassLines = $keptClass.Element("lines").Elements("line")
        $keptClass.SetAttributeValue("line-rate", (Get-LineRate $keptClassLines))
        $keptClass.SetAttributeValue("branch-rate", (Get-BranchRate $keptClassLines))
    }

    $packageLines = $classesElement.Elements("class").Element("lines").Elements("line")
    $package.SetAttributeValue("line-rate", (Get-LineRate $packageLines))
    $package.SetAttributeValue("branch-rate", (Get-BranchRate $packageLines))
}

# SetAttributeValue (create-or-update) is used instead of Attribute().Value to avoid null-refs: dotnet-coverage omits
# these attributes from the root element entirely (rather than writing zeros) when its profiler never initialized.
$allLines = @($doc.Descendants("class") | ForEach-Object { $_.Element("lines").Elements("line") })
$doc.Root.SetAttributeValue("lines-valid", [string]$allLines.Count)
$doc.Root.SetAttributeValue("lines-covered", [string]@($allLines | Where-Object { [int]$_.Attribute("hits").Value -gt 0 }).Count)
$doc.Root.SetAttributeValue("line-rate", (Get-LineRate $allLines))

$branchCounts = Get-BranchCounts $allLines
$doc.Root.SetAttributeValue("branches-valid", [string]$branchCounts.Total)
$doc.Root.SetAttributeValue("branches-covered", [string]$branchCounts.Covered)
$doc.Root.SetAttributeValue("branch-rate", (Get-BranchRate $allLines))

# Dynamic instrumentation isn't supported on macOS arm64 runners, so a profiler never initializes. And this suite's
# build/publish subprocesses can't be statically instrumented since their assemblies don't exist until mid-run.
# This yields zero packages/classes here. That's a platform limitation, not proof the code is untested.
if ($allLines.Count -eq 0) {
    $message = "Skipping $CoverageFile - no coverage data found"
    Write-Warning $message
    Write-Output "::warning::$message"
}

$doc.Save($CoverageFile)
