#!/usr/bin/env bash
# LB02 proof run: master, weather, pv-1, pv-2, battery-1, charger-1, wind-1, diagnostic clients
# usage: scripts/proof-run.sh [logDir]   (run from repo root, after dotnet build EnergySim.sln)
set -u
set -m   # job control: background programs keep SIGINT, so Ctrl+C style shutdown works

logDir=${1:-proof-logs}
masterPort=${MASTER_PORT:-1502}
cfg=Debug/net10.0
cd "$(dirname "$0")/.." || exit 2
mkdir -p "$logDir"
logDir=$(cd "$logDir" && pwd)
pipeDir=$(mktemp -d)

masterDll=src/EnergySim.Time.Cli/bin/$cfg/EnergySim.Time.Cli.dll
weatherDll=src/EnergySim.WeatherService/bin/$cfg/EnergySim.WeatherService.dll
deviceDll=src/EnergySim.Cli/bin/$cfg/EnergySim.Cli.dll
diagDll=src/EnergySim.Diagnostic/bin/$cfg/EnergySim.Diagnostic.dll

nowMs() { date +%s%3N; }
note() { echo "$(nowMs) $*" | tee -a "$logDir/steps.log"; }
waitFor() { # file pattern timeoutSeconds
  for _ in $(seq $(($3 * 10))); do
    grep -q "$2" "$1" 2>/dev/null && return 0
    sleep 0.1
  done
  echo "TIMEOUT waiting for '$2' in $1" | tee -a "$logDir/steps.log"
  return 1
}

# master: stdin from fifo, kept open on fd 9
mkfifo "$pipeDir/master.in"
dotnet $masterDll --headless --port "$masterPort" --start 2026-06-21T10:00:00Z --speed 60 \
  < "$pipeDir/master.in" > "$logDir/master.log" 2>&1 &
masterPid=$!
exec 9<>"$pipeDir/master.in"
sendMaster() { note "master< $*"; echo "$*" >&9; }
waitFor "$logDir/master.log" "commands:" 10

# weather service
dotnet $weatherDll > "$logDir/weather.log" 2>&1 &
weatherPid=$!
sleep 1

# devices: stdin kept open with a fifo each
devicePids=""
startDevice() { # name type modbusPort clockPort
  mkfifo "$pipeDir/$1.in"
  dotnet $deviceDll --device "$2" --id "$1" --clock shared --time-port "$masterPort" \
    --clock-port "$4" --Modbus:Port "$3" --location salzburg --weather-url http://localhost:5200 \
    < "$pipeDir/$1.in" > "$logDir/$1.log" 2>&1 &
  devicePids="$devicePids $!"
  eval "exec $5<>\"$pipeDir/$1.in\""
}
startDevice pv-1 pv 1511 1601 10
startDevice battery-1 battery 1512 1602 11
startDevice charger-1 charger 1513 1603 12
startDevice wind-1 wind 1514 1604 13
startDevice pv-2 pv 1521 1611 14
sleep 4

# diagnostic clients, persistent: pv-1 on fd 20, battery-1 on fd 21
diagStart() { # name port fd
  mkfifo "$pipeDir/diag-$1.in"
  dotnet $diagDll --Port "$2" < "$pipeDir/diag-$1.in" >> "$logDir/diag-$1.log" 2>&1 &
  diagPids="${diagPids:-} $!"
  eval "exec $3<>\"$pipeDir/diag-$1.in\""
}
diagStart pv-1 1511 20
diagStart battery-1 1512 21
diagStart pv-2 1521 22
sleep 2

# sample: marker with real time, then header on both clients back to back
sampleNo=0
sample() { # label
  sampleNo=$((sampleNo + 1))
  local marker="SAMPLE $sampleNo $1 realMs=$(nowMs)"
  echo "$marker" | tee -a "$logDir/diag-pv-1.log" "$logDir/diag-pv-2.log" >> "$logDir/diag-battery-1.log"
  echo "$marker" >> "$logDir/steps.log"
  echo table >&9   # master skew column at the same moment
  echo header >&20
  echo header >&21
  echo header >&22
  sleep 0.5
}

note "STEP 1 all programs started"
sendMaster start
sleep 3
note "STEP 2 skew at speed 60"
for _ in 1 2 3 4 5; do sample speed60; sleep 1; done
echo table >&9; sleep 1

note "STEP 3 speed change to 600 while running"
sendMaster speed 600
sleep 3
for _ in 1 2 3 4 5; do sample speed600; sleep 1; done
sendMaster speed 60
sleep 3
sample back60

note "STEP 4 pause: virtual time frozen, Modbus reachable"
sendMaster pause
sleep 3
sample paused1
sleep 3
sample paused2
echo "device" >&20   # Modbus still answers while paused

note "STEP 5 resume"
sendMaster resume
sleep 3
sample resumed
sample resumed
echo table >&9; sleep 1

note "STEP 6 restart with new virtual time"
sendMaster "restart 2026-06-22 13:00"
sleep 4
for _ in 1 2 3; do sample restarted; sleep 1; done
echo table >&9; sleep 1

note "STEP 7 weather: pause, then pv-1 and pv-2 report the same values for the same virtual time"
sendMaster pause
sleep 3
sample weather
echo device >&20
echo device >&22
sleep 1
sendMaster resume
sleep 2

note "STEP 8 diagnostic reads header and device values"
echo header >&20
echo device >&20
echo header >&21
echo device >&21
sleep 1

note "STEP 9 diagnostic writes limit"
echo header >&20   # before: limit 100 percent
echo "write limit 50" >&20
sleep 4
echo "write limit 150" >&20   # out of range: rejected by client
sleep 1
echo header >&20   # after
echo header >&22   # pv-2 unchanged for comparison
sleep 2

note "STEP 10 controlled shutdown"
for fd in 20 21 22; do echo quit >&$fd; done
sleep 1
for fd in 10 11 12 13 14; do echo quit >&$fd; done
sleep 3
sendMaster quit
sleep 2
kill -INT $weatherPid 2>/dev/null

# exit codes
for pid in $devicePids $diagPids; do
  wait "$pid"; note "exit pid $pid code $?"
done
wait $masterPid; note "exit master code $?"
wait $weatherPid; note "exit weather code $?"
rm -rf "$pipeDir"
