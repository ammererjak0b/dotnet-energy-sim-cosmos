#!/usr/bin/env bash
# LB02 test run: builds, runs the scripted full system (scripts/proof-run.sh) and checks the result
# usage: scripts/testrun_task2.sh [logDir]   (logDir default: temp dir, not committed)
# needs: dotnet 10 SDK, free ports 1502 1511-1514 1521 1601-1604 1611 5200
set -u

cd "$(dirname "$0")/.." || exit 2
logDir=${1:-$(mktemp -d)}
mkdir -p "$logDir"
logDir=$(cd "$logDir" && pwd)
failCount=0
passCount=0

pass() { echo "PASS  $*"; passCount=$((passCount + 1)); }
fail() { echo "FAIL  $*"; failCount=$((failCount + 1)); }
check() { # description command...
  local description=$1
  shift
  if "$@"; then pass "$description"; else fail "$description"; fi
}

# first line matching regex after the line matching marker, whitespace squeezed
firstAfter() { # file marker regex
  awk -v marker="$2" -v regex="$3" '
    found && $0 ~ regex { gsub(/[ \t]+/, " "); print; exit }
    $0 ~ marker { found = 1 }' "$1"
}

portIsBusy() { (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null; }

echo "== 1 ports free"
busyPorts=""
for port in 1502 1511 1512 1513 1514 1521 1601 1602 1603 1604 1611 5200; do
  if portIsBusy "$port"; then busyPorts="$busyPorts $port"; fi
done
if [ -n "$busyPorts" ]; then
  echo "FAIL  ports busy:$busyPorts (stop old EnergySim processes: pgrep -af EnergySim)"
  exit 1
fi
pass "all ports free"

echo "== 2 build"
if ! dotnet build EnergySim.sln -v q --nologo > "$logDir/build.log" 2>&1; then
  tail -20 "$logDir/build.log"
  echo "FAIL  build, log: $logDir/build.log"
  exit 1
fi
pass "build"

echo "== 3 full run (about 2 minutes), logs in $logDir"
bash scripts/proof-run.sh "$logDir" > "$logDir/proof-run.out" 2>&1
proofExit=$?
check "proof-run.sh finished with exit code 0" test "$proofExit" -eq 0

echo "== 4 checks"
masterLog="$logDir/master.log"
stepsLog="$logDir/steps.log"
pv1Log="$logDir/diag-pv-1.log"
pv2Log="$logDir/diag-pv-2.log"

for device in pv-1 pv-2 battery-1 charger-1 wind-1; do
  check "master lists $device as OK" grep -q "$device @ [0-9]* | OK |" "$masterLog"
done

check "no timeout while waiting for programs" bash -c "! grep -q TIMEOUT '$stepsLog'"
exitCodes=$(grep -c "^[0-9]* exit " "$stepsLog")
badExitCodes=$(grep "^[0-9]* exit " "$stepsLog" | grep -vc "code 0$")
check "all $exitCodes programs ended with exit code 0" test "$exitCodes" -eq 10 -a "$badExitCodes" -eq 0

# step 4: paused, virtual time identical in two samples
pausedTime1=$(firstAfter "$pv1Log" "SAMPLE [0-9]+ paused1 " "^time")
pausedTime2=$(firstAfter "$pv1Log" "SAMPLE [0-9]+ paused2 " "^time")
check "pause: virtual time stands still ($pausedTime1)" test -n "$pausedTime1" -a "$pausedTime1" = "$pausedTime2"
pausedSeq1=$(firstAfter "$pv1Log" "SAMPLE [0-9]+ paused1 " "^seq")
pausedSeq2=$(firstAfter "$pv1Log" "SAMPLE [0-9]+ paused2 " "^seq")
check "pause: Modbus answers, sequence constant ($pausedSeq1)" test -n "$pausedSeq1" -a "$pausedSeq1" = "$pausedSeq2"

# step 6: restart raises the generation
genBefore=$(firstAfter "$pv1Log" "SAMPLE [0-9]+ speed60 " "^gen" | awk '{print $2}')
genAfter=$(firstAfter "$pv1Log" "SAMPLE [0-9]+ restarted " "^gen" | awk '{print $2}')
check "restart: generation $genBefore -> $genAfter" test -n "$genBefore" -a -n "$genAfter" -a "$genAfter" -gt "$genBefore"

# step 7: same weather for same location and virtual time
for valueName in Irradiance ModuleTemperature SunElevation; do
  weather1=$(firstAfter "$pv1Log" "SAMPLE [0-9]+ weather " "^$valueName")
  weather2=$(firstAfter "$pv2Log" "SAMPLE [0-9]+ weather " "^$valueName")
  check "weather: pv-1 and pv-2 same $valueName (${weather1#* })" test -n "$weather1" -a "$weather1" = "$weather2"
done

# step 9: allowed setpoint written, forbidden one rejected
check "write limit 50 accepted" grep -q "written: limit 50" "$pv1Log"
check "write limit 150 rejected before sending" grep -q "rejected, not sent" "$pv1Log"

# no process of this run left behind
leftover=$(pgrep -f "EnergySim\.(Cli|Time\.Cli|WeatherService|Diagnostic)\.dll" | wc -l)
check "no EnergySim process left running" test "$leftover" -eq 0

echo
echo "result: $passCount passed, $failCount failed, logs: $logDir"
test "$failCount" -eq 0
