param(
    [string]$Iscc
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'blinkNode.App\blinkNode.App.csproj'
$payload = Join-Path $repo 'publish\blinkNode-Light-0.1.0-setup-payload'
$iss = Join-Path $PSScriptRoot 'blinkNodeLight.iss'

if (-not $Iscc) {
    $candidates = @(
        (Join-Path $env:USERPROFILE 'AppData\Local\Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )
    $Iscc = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if (-not $Iscc -or -not (Test-Path -LiteralPath $Iscc)) {
    throw "Inno Setup compiler not found: $Iscc. Install Inno Setup 6 or pass -Iscc with its ISCC.exe path."
}

dotnet restore $project --runtime win-x64 --ignore-failed-sources -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'The app restore failed.' }

dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false --no-restore -o $payload
if ($LASTEXITCODE -ne 0) { throw 'The self-contained Windows app publish failed.' }

Get-ChildItem -LiteralPath $payload -Filter '*.pdb' -File -Recurse | Remove-Item -Force
& $Iscc $iss
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed to build the installer.' }

Get-Item (Join-Path $repo 'publish\blinkNode-Light-Setup-0.1.0-win-x64.exe') | Select-Object FullName, Length
