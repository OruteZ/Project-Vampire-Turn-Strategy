# Builds StageReview.exe next to this file with the C# compiler that ships with Windows (.NET Framework 4).
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:exe /codepage:65001 /out:"$PSScriptRoot\StageReview.exe" /r:System.Web.Extensions.dll "$PSScriptRoot\server.cs"
