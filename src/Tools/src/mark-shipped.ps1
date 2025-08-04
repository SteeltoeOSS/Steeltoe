# Based on https://github.com/dotnet/roslyn/blob/main/scripts/PublicApi/mark-shipped.ps1

# TIP: To obtain the public API diff between releases, run:
# git diff --unified=0 --text head previous-release-tag-name -- **/PublicAPI.Shipped.txt

[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = 'Stop'

function MarkShipped([string] $dir) {
    $shippedFilePath = Join-Path $dir 'PublicAPI.Shipped.txt'
    $shipped = Get-Content $shippedFilePath
    if ($null -eq $shipped) {
        $shipped = @('#nullable enable')
    }

    $unshippedFilePath = Join-Path $dir 'PublicAPI.Unshipped.txt'
    $unshipped = Get-Content $unshippedFilePath
    $removed = @()
    $removedPrefix = '*REMOVED*';
    Write-Host "Processing $dir"

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0) {
            if ($item.StartsWith($removedPrefix)) {
                $item = $item.Substring($removedPrefix.Length)
                $removed += $item
            }
            elseif (-not $item.StartsWith('#')) {
                $shipped += $item
            }
        }
    }

    $shipped | Sort-Object | ?{ -not $removed.Contains($_) } | Out-File $shippedFilePath -Encoding Ascii
    '#nullable enable' | Out-File $unshippedFilePath -Encoding Ascii
}

try {
    Push-Location ./../..

    foreach ($file in Get-ChildItem -re -in 'PublicApi.Shipped.txt') {
        $dir = Split-Path -parent $file
        MarkShipped $dir
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
finally {
    Pop-Location
}
