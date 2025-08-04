[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = 'Stop'

function HasUnshipped([string] $dir) {
    $filePath = Join-Path $dir 'PublicAPI.Unshipped.txt'
    $content = Get-Content $filePath | Where-Object {$_ -NOTLIKE "#*"}
    return [bool]$content
}

try {
    Push-Location ./../..
    $unshipped = @()

    foreach ($file in Get-ChildItem -re -in 'PublicApi.Unshipped.txt') {
        $dir = Split-Path -parent $file
        $hasUnshipped = HasUnshipped $dir

        if ($hasUnshipped) {
            $unshipped += $dir
        }
    }

    if ($unshipped.Count -ne 0) {
        $list = $unshipped -join "`n"
        throw "Unshipped APIs found in the following directories:`n$list"
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
