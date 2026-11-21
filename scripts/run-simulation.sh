#!/usr/bin/env bash
# EnergySim: start the whole simulation, every program in its own terminal window (Linux, bash)
#   scripts/run-simulation.sh              native: everything as dotnet processes, RabbitMQ from Docker
#   scripts/run-simulation.sh pv2          native plus a second PV (same weather check)
#   scripts/run-simulation.sh docker       Docker: master, weather, wind-1, RabbitMQ, Portainer as containers,
#                                          gateway, viewer, diagnostic as dotnet processes
#   scripts/run-simulation.sh stop         stop all EnergySim programs and the containers
# needs: dotnet 10 SDK, docker + compose, .env (cp .env.example .env), one terminal: kitty, alacritty, ghostty, konsole or xterm
# master window (native): P start/pause/resume, S step (paused), V speed, R restart, Q quit
set -u

cd "$(dirname "$0")/.." || exit 2
config=Debug/net10.0 # build configuration
programPattern='EnergySim\.(Cli|Time\.Cli|WeatherService|Diagnostic|Gateway|Viewer)\.dll'
masterDll=src/EnergySim.Time.Cli/bin/$config/EnergySim.Time.Cli.dll
weatherDll=src/EnergySim.WeatherService/bin/$config/EnergySim.WeatherService.dll
deviceDll=src/EnergySim.Cli/bin/$config/EnergySim.Cli.dll
diagnosticDll=src/EnergySim.Diagnostic/bin/$config/EnergySim.Diagnostic.dll
gatewayDll=src/EnergySim.Gateway/bin/$config/EnergySim.Gateway.dll
viewerDll=src/EnergySim.Viewer/bin/$config/EnergySim.Viewer.dll
startTime=${START_TIME:-2026-06-21T04:00:00Z} # native master start (virtual UTC), the Docker master start is in compose.yaml
mode=${1:-native}

if [ "$mode" = "stop" ]; then
  pkill -f "$programPattern" # SIGTERM = clean stop, exit code 0
  docker compose down
  echo "stopped"
  exit 0
fi
if [ "$mode" != "native" ] && [ "$mode" != "docker" ] && [ "$mode" != "pv2" ]; then
  echo "usage: scripts/run-simulation.sh [native|pv2|docker|stop]"
  exit 2
fi

if [ ! -f .env ]; then
  echo ".env missing: cp .env.example .env and set RABBITMQ_PASSWORD"
  exit 1
fi

portIsBusy() { (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null; }

waitForPort() { # port timeoutSeconds
  for _ in $(seq $(($2 * 10))); do
    portIsBusy "$1" && return 0
    sleep 0.1
  done
  echo "timeout: nothing listens on port $1"
  return 1
}

terminal=""
for candidate in kitty alacritty ghostty konsole xterm; do
  if command -v "$candidate" > /dev/null 2>&1; then
    terminal=$candidate
    break
  fi
done
if [ -z "$terminal" ]; then
  echo "no terminal found (kitty, alacritty, ghostty, konsole, xterm): start the programs by hand, see README.md"
  exit 1
fi

# one window per program, .env loaded (RabbitMQ user and password), stays open after the program ends
openWindow() { # title command
  local title=$1
  local command="set -a; source .env; set +a; $2; code=\$?; echo; echo \"[$title] exited with code \$code\"; read -r -p 'Enter to close '"
  case $terminal in
    kitty) kitty --title "$title" bash -c "$command" > /dev/null 2>&1 & ;;
    alacritty) alacritty --title "$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
    ghostty) ghostty --title="$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
    konsole) konsole -p tabtitle="$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
    xterm) xterm -T "$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
  esac
}

# ports that must be free before the start
if [ "$mode" = "docker" ]; then
  ports="1502 1514 5200 5672 9443" # container ports on this machine
else
  ports="1502 1511 1512 1513 1514 1521 1601 1602 1603 1604 1611 5200 5672"
fi
busyPorts=""
for port in $ports; do
  if portIsBusy "$port"; then busyPorts="$busyPorts $port"; fi
done
if [ -n "$busyPorts" ]; then
  echo "ports busy:$busyPorts - old programs or containers running? try: scripts/run-simulation.sh stop"
  exit 1
fi

dotnet build EnergySim.sln -v q --nologo || exit 1 # 0 warnings expected

device() { # name type modbusPort clockPort
  echo "dotnet $deviceDll --device $2 --id $1 --clock shared --clock-port $4 --Modbus:Port $3"
}

if [ "$mode" = "docker" ]; then
  docker compose up -d --build --wait || exit 1 # master, weather, wind-1, rabbitmq, portainer
  openWindow "viewer" "dotnet $viewerDll --Rabbit:Routing 'telemetry.wind.*'"
  openWindow "gateway" "dotnet $gatewayDll"
  openWindow "diagnostic" "dotnet $diagnosticDll --Host 127.0.0.1 --Port 1514"
  echo "containers: docker compose ps. master commands: docker attach master (pause, resume, step, speed 600, quit with Ctrl+P Ctrl+Q)"
  echo "overview: https://localhost:9443 (Portainer), broker UI: http://localhost:15672"
  echo "the gateway reports pv-1, battery-1, charger-1 as not reachable once, they do not run in this mode"
  echo "stop everything: scripts/run-simulation.sh stop"
  exit 0
fi

docker compose up -d --wait rabbitmq || exit 1 # only the broker, the simulators run native
openWindow "master" "dotnet $masterDll --start $startTime --speed 60"
waitForPort 1502 15 || exit 1
openWindow "weather" "dotnet $weatherDll"
waitForPort 5200 15 || exit 1
openWindow "battery-1" "$(device battery-1 battery 1512 1602)"
openWindow "charger-1" "$(device charger-1 charger 1513 1603)"
openWindow "pv-1" "$(device pv-1 pv 1511 1601)"
openWindow "wind-1" "$(device wind-1 wind 1514 1604)"
if [ "$mode" = "pv2" ]; then
  openWindow "pv-2" "$(device pv-2 pv 1521 1611)"
fi
sleep 3
openWindow "diagnostic" "dotnet $diagnosticDll --Port 1511"
openWindow "viewer" "dotnet $viewerDll"
openWindow "gateway" "dotnet $gatewayDll"

echo "started in $terminal windows. master window: P start, S step, V speed, R restart, Q quit"
echo "diagnostic window: header | device | write limit 40 | write on|off | target | quit"
echo "broker UI: http://localhost:15672 (login from .env)"
echo "stop everything: scripts/run-simulation.sh stop"
