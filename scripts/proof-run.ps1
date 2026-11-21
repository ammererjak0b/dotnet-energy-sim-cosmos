<#
EnergySim proof run (Windows, PowerShell 5.1 or 7), headless, no windows:
master, weather, pv-1, pv-2, battery-1, charger-1, wind-1, diagnostic clients
steps: start, speed change, pause, single step, resume, restart, same weather, diagnostic read/write, controlled stop
  .\scripts\proof-run.ps1 [-LogDir proof-logs]     (repo root, after dotnet build EnergySim.sln)
  $env:MASTER_PORT = 1520; .\scripts\proof-run.ps1  when another master already uses 1502
Linux twin: scripts/proof-run.sh (same steps, same marker lines).
Difference: Windows cannot send Ctrl+C to a background process, so the weather service is stopped with Kill (exit code not 0).
All other programs get "quit" on stdin and exit with code 0.
if scripts are blocked: powershell -ExecutionPolicy Bypass -File .\scripts\proof-run.ps1
#>
param(
    [string]$LogDir = 'proof-logs'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$masterPort = 1502
if ($env:MASTER_PORT) { $masterPort = [int]$env:MASTER_PORT }
$config = 'Debug/net10.0'
$masterDll = "src/EnergySim.Time.Cli/bin/$config/EnergySim.Time.Cli.dll"
$weatherDll = "src/EnergySim.WeatherService/bin/$config/EnergySim.WeatherService.dll"
$deviceDll = "src/EnergySim.Cli/bin/$config/EnergySim.Cli.dll"
$diagDll = "src/EnergySim.Diagnostic/bin/$config/EnergySim.Diagnostic.dll"

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$LogDir = (Resolve-Path $LogDir).Path
$stepsLog = Join-Path $LogDir 'steps.log'
Remove-Item (Join-Path $LogDir '*.log') -ErrorAction SilentlyContinue

# thread safe log writer: output of every program goes to its own file, markers go through the same writer
Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
public static class EnergySimLogSink
{
    private static readonly Dictionary<string, StreamWriter> writers = new Dictionary<string, StreamWriter>();

    private static StreamWriter Open(string path)
    {
        lock (writers)
        {
            StreamWriter writer;
            if (!writers.TryGetValue(path, out writer))
            {
                FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                writer = new StreamWriter(stream);
                writer.AutoFlush = true;
                writers[path] = writer;
            }
            return writer;
        }
    }

    public static void Append(string path, string line)
    {
        StreamWriter writer = Open(path);
        lock (writer) { writer.WriteLine(line); }
    }

    public static void Attach(Process process, string path)
    {
        DataReceivedEventHandler handler = delegate (object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) { Append(path, e.Data); }
        };
        process.OutputDataReceived += handler;
        process.ErrorDataReceived += handler;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }
}
"@

function Get-NowMs { return [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() }

function Write-Note([string]$text) {
    $line = "$(Get-NowMs) $text"
    Write-Host $line
    [EnergySimLogSink]::Append($stepsLog, $line)
}

function Wait-For([string]$file, [string]$pattern, [int]$timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path $file) -and (Select-String -Path $file -Pattern $pattern -Quiet)) { return $true }
        Start-Sleep -Milliseconds 100
    }
    Write-Note "TIMEOUT waiting for '$pattern' in $file"
    return $false
}

# a headless program: stdin and output redirected, stdin stays open for commands
function Start-Prog([string]$name, [string[]]$arguments) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = 'dotnet'
    foreach ($argument in $arguments) { $info.ArgumentList.Add($argument) }
    $info.UseShellExecute = $false
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.WorkingDirectory = $root
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $info
    [void]$process.Start()
    $process.StandardInput.AutoFlush = $true
    [EnergySimLogSink]::Attach($process, (Join-Path $LogDir "$name.log"))
    return $process
}

function Send-Line($process, [string]$text) { $process.StandardInput.WriteLine($text) }

$master = Start-Prog 'master' @($masterDll, '--headless', '--port', "$masterPort", '--start', '2026-06-21T10:00:00Z', '--speed', '60')
function Send-Master([string]$text) { Write-Note "master< $text"; Send-Line $master $text }
[void](Wait-For (Join-Path $LogDir 'master.log') 'commands:' 10)

$weather = Start-Prog 'weather' @($weatherDll)
Start-Sleep -Seconds 1

$devices = [ordered]@{}
function Start-Device([string]$name, [string]$type, [int]$modbusPort, [int]$clockPort) {
    $script:devices[$name] = Start-Prog $name @($deviceDll, '--device', $type, '--id', $name, '--clock', 'shared', '--time-port', "$masterPort", '--clock-port', "$clockPort", '--Modbus:Port', "$modbusPort", '--location', 'salzburg', '--weather-url', 'http://localhost:5200')
}
Start-Device 'pv-1' 'pv' 1511 1601
Start-Device 'battery-1' 'battery' 1512 1602
Start-Device 'charger-1' 'charger' 1513 1603
Start-Device 'wind-1' 'wind' 1514 1604
Start-Device 'pv-2' 'pv' 1521 1611
Start-Sleep -Seconds 4

$diags = [ordered]@{}
function Start-Diag([string]$name, [int]$port) {
    $script:diags[$name] = Start-Prog "diag-$name" @($diagDll, '--Port', "$port")
}
Start-Diag 'pv-1' 1511
Start-Diag 'battery-1' 1512
Start-Diag 'pv-2' 1521
Start-Sleep -Seconds 2

# sample: marker with real time in the three diagnostic logs, then header on all three clients back to back
$sampleNo = 0
function Write-Sample([string]$label) {
    $script:sampleNo++
    $marker = "SAMPLE $($script:sampleNo) $label realMs=$(Get-NowMs)"
    foreach ($name in $diags.Keys) { [EnergySimLogSink]::Append((Join-Path $LogDir "diag-$name.log"), $marker) }
    [EnergySimLogSink]::Append($stepsLog, $marker)
    Send-Line $master 'table' # master skew column at the same moment
    foreach ($name in $diags.Keys) { Send-Line $diags[$name] 'header' }
    Start-Sleep -Milliseconds 500
}

Write-Note 'STEP 1 all programs started'
Send-Master 'start'
Start-Sleep -Seconds 3
Write-Note 'STEP 2 skew at speed 60'
1..5 | ForEach-Object { Write-Sample 'speed60'; Start-Sleep -Seconds 1 }
Send-Line $master 'table'; Start-Sleep -Seconds 1

Write-Note 'STEP 3 speed change to 600 while running'
Send-Master 'speed 600'
Start-Sleep -Seconds 3
1..5 | ForEach-Object { Write-Sample 'speed600'; Start-Sleep -Seconds 1 }
Send-Master 'speed 60'
Start-Sleep -Seconds 3
Write-Sample 'back60'

Write-Note 'STEP 4 pause: virtual time frozen, Modbus reachable'
Send-Master 'pause'
Start-Sleep -Seconds 3
Write-Sample 'paused1'
Start-Sleep -Seconds 3
Write-Sample 'paused2'
Send-Line $diags['pv-1'] 'device' # Modbus still answers while paused

Write-Note 'STEP 4b single step while paused: virtual time +12 s per step at speed 60, clock stays paused'
Send-Master 'step'
Start-Sleep -Seconds 1
Write-Sample 'step1'
Send-Master 'step'
Start-Sleep -Seconds 1
Write-Sample 'step2'

Write-Note 'STEP 5 resume'
Send-Master 'resume'
Start-Sleep -Seconds 3
Write-Sample 'resumed'
Write-Sample 'resumed'
Send-Line $master 'table'; Start-Sleep -Seconds 1

Write-Note 'STEP 6 restart with new virtual time'
Send-Master 'restart 2026-06-22 13:00'
Start-Sleep -Seconds 4
1..3 | ForEach-Object { Write-Sample 'restarted'; Start-Sleep -Seconds 1 }
Send-Line $master 'table'; Start-Sleep -Seconds 1

Write-Note 'STEP 7 weather: pause, then pv-1 and pv-2 report the same values for the same virtual time'
Send-Master 'pause'
Start-Sleep -Seconds 3
Write-Sample 'weather'
Send-Line $diags['pv-1'] 'device'
Send-Line $diags['pv-2'] 'device'
Start-Sleep -Seconds 1
Send-Master 'resume'
Start-Sleep -Seconds 2

Write-Note 'STEP 8 diagnostic reads header and device values'
Send-Line $diags['pv-1'] 'header'
Send-Line $diags['pv-1'] 'device'
Send-Line $diags['battery-1'] 'header'
Send-Line $diags['battery-1'] 'device'
Start-Sleep -Seconds 1

Write-Note 'STEP 9 diagnostic writes limit'
Send-Line $diags['pv-1'] 'header' # before: limit 100 percent
Send-Line $diags['pv-1'] 'write limit 50'
Start-Sleep -Seconds 4
Send-Line $diags['pv-1'] 'write limit 150' # out of range: rejected by client
Start-Sleep -Seconds 1
Send-Line $diags['pv-1'] 'header' # after
Send-Line $diags['pv-2'] 'header' # pv-2 unchanged for comparison
Start-Sleep -Seconds 2

Write-Note 'STEP 10 controlled shutdown'
foreach ($name in $diags.Keys) { Send-Line $diags[$name] 'quit' }
Start-Sleep -Seconds 1
foreach ($name in $devices.Keys) { Send-Line $devices[$name] 'quit' }
Start-Sleep -Seconds 3
Send-Master 'quit'
Start-Sleep -Seconds 2
if (-not $weather.HasExited) { $weather.Kill() } # no Ctrl+C for background processes on Windows

foreach ($name in $diags.Keys) { [void]$diags[$name].WaitForExit(15000); Write-Note "exit diag-$name code $($diags[$name].ExitCode)" }
foreach ($name in $devices.Keys) { [void]$devices[$name].WaitForExit(15000); Write-Note "exit $name code $($devices[$name].ExitCode)" }
[void]$master.WaitForExit(15000); Write-Note "exit master code $($master.ExitCode)"
[void]$weather.WaitForExit(15000); Write-Note "exit weather code $($weather.ExitCode) (killed, see header)"
