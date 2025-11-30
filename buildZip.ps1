param(
    [Parameter(Mandatory)][string]$Path = $(Read-Host "Enter the project path")
)

$Path = $Path.Trim('"')  # Remove accidental quotes

$releaseDir = Join-Path $Path "bin\Release"

if (!(Test-Path $releaseDir)) {
    Write-Error "Release folder not found: $releaseDir"
    exit
}

$out = Join-Path $Path "Finalized"
New-Item -Type Directory -Path $out -Force | Out-Null

$stamp = (Get-Date -Format "yyyyMMdd_HHmmss")
$zip = Join-Path $out "GPhysStore_$stamp.zip"

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($releaseDir, $zip)

Write-Host "Release folder zipped to $zip"
