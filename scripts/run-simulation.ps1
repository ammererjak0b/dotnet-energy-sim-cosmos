<#
EnergySim: start the whole simulation, every program in its own PowerShell window (Windows, PowerShell 5.1 or 7)
  .\scripts\run-simulation.ps1               native: everything as dotnet processes, RabbitMQ from Docker
  .\scripts\run-simulation.ps1 pv2           native plus a second PV (same weather check)
  .\scripts\run-simulation.ps1 docker        Docker: master, weather, wind-1, RabbitMQ, Portainer as containers,
                                             gateway, viewer, diagnostic as dotnet processes
  .\scripts\run-simulation.ps1 stop          stop all EnergySim programs and the containers
needs: dotnet 10 SDK, Docker Desktop with compose, .env (copy .env.example to .env, set RABBITMQ_PASSWORD)
if scripts are blocked: powershell -ExecutionPolicy Bypass -File .\scripts\run-simulation.ps1
master window (native): P start/pause/resume, S step (paused), V speed, R restart, Q quit
#>
param(
    [ValidateSet('native', 'pv2', 'docker', 'stop')]
    [string]$Mode = 'native'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$config = 'Debug/net10.0' # build configuration
$programPattern = 'EnergySim\.(Cli|Time\.Cli|WeatherService|Diagnostic|Gateway|Viewer)\.dll'
$masterDll = "src/EnergySim.Time.Cli/bin/$config/EnergySim.Time.Cli.dll"
$weatherDll = "src/EnergySim.WeatherService/bin/$config/EnergySim.WeatherService.dll"
$deviceDll = "src/EnergySim.Cli/bin/$config/EnergySim.Cli.dll"
$diagnosticDll = "src/EnergySim.Diagnostic/bin/$config/EnergySim.Diagnostic.dll"
$gatewayDll = "src/EnergySim.Gateway/bin/$config/EnergySim.Gateway.dll"
$viewerDll = "src/EnergySim.Viewer/bin/$config/EnergySim.Viewer.dll"
$startTime = '2026-06-21T04:00:00Z' # native master start (virtual UTC), the Docker master start is in compose.yaml
if ($env:START_TIME) { $startTime = $env:START_TIME }

function Test-PortBusy([int]$port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $task = $client.ConnectAsync('127.0.0.1', $port)
        return ($task.Wait(300) -and $client.Connected)
    }
    catch { return $false }
    finally { $client.Dispose() }
}

function Wait-Port([int]$port, [int]$timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-PortBusy $port) { return }
        Start-Sleep -Milliseconds 200
    }
    throw "timeout: nothing listens on port $port"
}

function Invoke-Native([string]$command) { # stops the script when a command fails
    Invoke-Expression $command
    if ($LASTEXITCODE -ne 0) { throw "failed (exit code $LASTEXITCODE): $command" }
}

$shell = 'powershell'
if (Get-Command pwsh -ErrorAction SilentlyContinue) { $shell = 'pwsh' }

# one window per program, .env loaded (RabbitMQ user and password), stays open after the program ends
function Open-Window([string]$title, [string]$command) {
    $text = @"
`$Host.UI.RawUI.WindowTitle = '$title'
Get-Content .env | ForEach-Object { if (`$_ -match '^\s*([^#=\s]+)\s*=\s*(.*)$') { [Environment]::SetEnvironmentVariable(`$Matches[1], `$Matches[2]) } }
$command
Write-Host ''
Write-Host "[$title] exited with code `$LASTEXITCODE"
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($text))
    Start-Process $shell -WorkingDirectory $root -ArgumentList '-NoExit', '-EncodedCommand', $encoded | Out-Null
}

if ($Mode -eq 'stop') {
    # hard stop (TerminateProcess). Controlled shutdown: press Q / Ctrl+C / type quit in each window first
    Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
        Where-Object { $_.CommandLine -match $programPattern } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
    docker compose down
    Write-Host 'stopped'
    return
}

if (-not (Test-Path .env)) {
    Write-Host '.env missing: copy .env.example to .env and set RABBITMQ_PASSWORD'
    exit 1
}

$ports = @(1502, 1511, 1512, 1513, 1514, 1521, 1601, 1602, 1603, 1604, 1611, 5200, 5672)
if ($Mode -eq 'docker') { $ports = @(1502, 1514, 5200, 5672, 9443) } # container ports on this machine
$busy = @($ports | Where-Object { Test-PortBusy $_ })
if ($busy.Count -gt 0) {
    Write-Host ("ports busy: " + ($busy -join ' ') + " - old programs or containers running? try: .\scripts\run-simulation.ps1 stop")
    exit 1
}

Invoke-Native 'dotnet build EnergySim.sln -v q --nologo' # 0 warnings expected

function Get-DeviceCommand([string]$name, [string]$type, [int]$modbusPort, [int]$clockPort) {
    return "dotnet $deviceDll --device $type --id $name --clock shared --clock-port $clockPort --Modbus:Port $modbusPort"
}

if ($Mode -eq 'docker') {
    Invoke-Native 'docker compose up -d --build --wait' # master, weather, wind-1, rabbitmq, portainer
    Open-Window 'viewer' "dotnet $viewerDll --Rabbit:Routing 'telemetry.wind.*'"
    Open-Window 'gateway' "dotnet $gatewayDll"
    Open-Window 'diagnostic' "dotnet $diagnosticDll --Host 127.0.0.1 --Port 1514"
    Write-Host 'containers: docker compose ps. master commands: docker attach master (pause, resume, step, speed 600, detach with Ctrl+P Ctrl+Q)'
    Write-Host 'overview: https://localhost:9443 (Portainer), broker UI: http://localhost:15672'
    Write-Host 'the gateway reports pv-1, battery-1, charger-1 as not reachable once, they do not run in this mode'
    Write-Host 'stop everything: .\scripts\run-simulation.ps1 stop'
    return
}

Invoke-Native 'docker compose up -d --wait rabbitmq' # only the broker, the simulators run native
Open-Window 'master' "dotnet $masterDll --start $startTime --speed 60"
Wait-Port 1502 15
Open-Window 'weather' "dotnet $weatherDll"
Wait-Port 5200 15
Open-Window 'battery-1' (Get-DeviceCommand 'battery-1' 'battery' 1512 1602)
Open-Window 'charger-1' (Get-DeviceCommand 'charger-1' 'charger' 1513 1603)
Open-Window 'pv-1' (Get-DeviceCommand 'pv-1' 'pv' 1511 1601)
Open-Window 'wind-1' (Get-DeviceCommand 'wind-1' 'wind' 1514 1604)
if ($Mode -eq 'pv2') {
    Open-Window 'pv-2' (Get-DeviceCommand 'pv-2' 'pv' 1521 1611)
}
Start-Sleep -Seconds 3
Open-Window 'diagnostic' "dotnet $diagnosticDll --Port 1511"
Open-Window 'viewer' "dotnet $viewerDll"
Open-Window 'gateway' "dotnet $gatewayDll"

Write-Host 'started in PowerShell windows. master window: P start, S step, V speed, R restart, Q quit'
Write-Host 'diagnostic window: header | device | write limit 40 | write on|off | target | quit'
Write-Host 'broker UI: http://localhost:15672 (login from .env)'
Write-Host 'stop everything: .\scripts\run-simulation.ps1 stop'
