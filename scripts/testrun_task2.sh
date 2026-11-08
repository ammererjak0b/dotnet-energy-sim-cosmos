#!/usr/bin/env bash
# LB02 manual test run: opens master, weather, 4 devices and the diagnostic client, each in its own terminal window
# usage: scripts/testrun_task2.sh          start everything
#        scripts/testrun_task2.sh pv2      start everything plus a second PV (same weather test)
#        scripts/testrun_task2.sh stop     stop all EnergySim programs (their windows close with them)
# needs: dotnet 10 SDK and one terminal: kitty, alacritty, ghostty, konsole or xterm
# keys in the master window: P start/pause/resume, V speed, R restart, Q quit
set -u

cd "$(dirname "$0")/.." || exit 2
config=Debug/net10.0
programPattern='EnergySim\.(Cli|Time\.Cli|WeatherService|Diagnostic)\.dll'
masterDll=src/EnergySim.Time.Cli/bin/$config/EnergySim.Time.Cli.dll
weatherDll=src/EnergySim.WeatherService/bin/$config/EnergySim.WeatherService.dll
deviceDll=src/EnergySim.Cli/bin/$config/EnergySim.Cli.dll
diagnosticDll=src/EnergySim.Diagnostic/bin/$config/EnergySim.Diagnostic.dll

if [ "${1:-}" = "stop" ]; then
  pkill -f "$programPattern"
  echo "stopped"
  exit 0
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

# terminal: first one found
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

# one window per program, stays open after the program ends
openWindow() { # title command
  local title=$1
  local command="$2; code=\$?; echo; echo \"[$title] exited with code \$code\"; read -r -p 'Enter to close '"
  case $terminal in
    kitty) kitty --title "$title" bash -c "$command" > /dev/null 2>&1 & ;;
    alacritty) alacritty --title "$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
    ghostty) ghostty --title="$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
    konsole) konsole -p tabtitle="$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
    xterm) xterm -T "$title" -e bash -c "$command" > /dev/null 2>&1 & ;;
  esac
}

busyPorts=""
for port in 1502 1511 1512 1513 1514 1521 1601 1602 1603 1604 1611 5200; do
  if portIsBusy "$port"; then busyPorts="$busyPorts $port"; fi
done
if [ -n "$busyPorts" ]; then
  echo "ports busy:$busyPorts - old programs running? try: scripts/testrun_task2.sh stop"
  exit 1
fi

dotnet build EnergySim.sln -v q --nologo || exit 1

device() { # name type modbusPort clockPort
  echo "dotnet $deviceDll --device $2 --id $1 --clock shared --clock-port $4 --Modbus:Port $3"
}

openWindow "master" "dotnet $masterDll --start 2026-06-21T04:00:00Z --speed 60"
waitForPort 1502 15 || exit 1
openWindow "weather" "dotnet $weatherDll"
waitForPort 5200 15 || exit 1
openWindow "battery-1" "$(device battery-1 battery 1512 1602)"
openWindow "charger-1" "$(device charger-1 charger 1513 1603)"
openWindow "pv-1" "$(device pv-1 pv 1511 1601)"
openWindow "wind-1" "$(device wind-1 wind 1514 1604)"
if [ "${1:-}" = "pv2" ]; then
  openWindow "pv-2" "$(device pv-2 pv 1521 1611)"
fi
sleep 3
openWindow "diagnostic" "dotnet $diagnosticDll --Port 1511"

echo "started in $terminal windows. master window: P start, V speed, R restart, Q quit"
echo "diagnostic window: header | device | write limit 40 | write on|off | target | quit"
echo "stop everything: scripts/testrun_task2.sh stop"
