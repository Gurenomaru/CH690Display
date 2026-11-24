$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$sources = Join-Path $PSScriptRoot 'src'
$references = @(
    (Join-Path $framework 'System.dll')
    (Join-Path $framework 'System.Core.dll')
    (Join-Path $framework 'System.Drawing.dll')
    (Join-Path $framework 'System.Windows.Forms.dll')
    (Join-Path $PSScriptRoot 'LibreHardwareMonitorLib.dll')
)
$compilerArguments = @(
    '/nologo', '/noconfig', '/target:winexe', '/platform:x64', '/optimize+', '/warnaserror+'
    "/out:$(Join-Path $PSScriptRoot 'CH690Display.exe')"
    $references | ForEach-Object { "/reference:$_" }
    Get-ChildItem -LiteralPath $sources -Filter '*.cs' -File | ForEach-Object FullName
)
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE."
}
