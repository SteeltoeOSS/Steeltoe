# This script merges the content of PublicAPI.Unshipped.txt files into PublicAPI.Shipped.txt.
# Run this before creating a new release and commit the changes.
# Based on https://github.com/dotnet/roslyn/blob/main/scripts/PublicApi/mark-shipped.ps1
#
# TIP: To obtain the public API diff between two releases, run:
# git diff --unified=0 --text <previous-release-tag-name> head -- **/PublicAPI.Shipped.txt

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

    $shipped | Sort-Object -Unique | Where-Object { -not $removed.Contains($_) } | Out-File $shippedFilePath -Encoding Ascii
    '#nullable enable' | Out-File $unshippedFilePath -Encoding Ascii
}

foreach ($file in Get-ChildItem -re -in 'PublicApi.Shipped.txt') {
    $dir = Split-Path -parent $file
    MarkShipped $dir
}
