#!/usr/bin/env bash
# LB03 manual test run: RabbitMQ (Docker), the LB02 system, gateway and viewer, each program in its own terminal window
# usage: scripts/testrun_task3.sh          start everything
#        scripts/testrun_task3.sh stop     stop programs and the broker
# needs: .env (copy of .env.example), docker, dotnet 10 SDK, one terminal: kitty, alacritty, ghostty, konsole or xterm
set -u

cd "$(dirname "$0")/.." || exit 2
config=Debug/net10.0
gatewayDll=src/EnergySim.Gateway/bin/$config/EnergySim.Gateway.dll
viewerDll=src/EnergySim.Viewer/bin/$config/EnergySim.Viewer.dll

if [ "${1:-}" = "stop" ]; then
  pkill -f 'EnergySim\.(Gateway|Viewer)\.dll'
  scripts/testrun_task2.sh stop
  docker compose down
  exit 0
fi

if [ ! -f .env ]; then
  echo ".env missing: cp .env.example .env and set the password"
  exit 1
fi

terminal=""
for candidate in kitty alacritty ghostty konsole xterm; do
  if command -v "$candidate" > /dev/null 2>&1; then
    terminal=$candidate
    break
  fi
done
if [ -z "$terminal" ]; then
  echo "no terminal found (kitty, alacritty, ghostty, konsole, xterm)"
  exit 1
fi

# one window per program, stays open after the program ends
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

docker compose up -d --wait || exit 1 # --wait = until healthcheck is green
scripts/testrun_task2.sh || exit 1 # builds the solution too
openWindow "viewer" "dotnet $viewerDll"
openWindow "gateway" "dotnet $gatewayDll"

echo "broker UI: http://localhost:15672 (login from .env)"
echo "gateway/viewer windows: quit or Ctrl+C stops them"
echo "stop everything: scripts/testrun_task3.sh stop"
