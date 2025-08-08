# This script verifies that all PublicAPI.Unshipped.txt files are empty.
# Run this as a sanity check before creating a new release.

[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = 'Stop'

function HasUnshipped([string] $dir) {
    $filePath = Join-Path $dir 'PublicAPI.Unshipped.txt'
    $content = Get-Content $filePath | Where-Object { $_ -NOTLIKE "#*" }
    return [bool]$content
}

$dirsUnshipped = @()

foreach ($file in Get-ChildItem -re -in 'PublicApi.Unshipped.txt') {
    $dir = Split-Path -parent $file
    $hasUnshipped = HasUnshipped $dir

    if ($hasUnshipped) {
        $dirsUnshipped += $dir
    }
}

if ($dirsUnshipped.Count -gt 0) {
    Write-Error "Unshipped APIs found in the following directories:`n$($dirsUnshipped -join "`n")"
    return 1
}
