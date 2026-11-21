# Master (TimeSim) container. Build context = repo root:
#   docker build -f src/EnergySim.Time.Cli/Dockerfile -t energysim/master:lb04 .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/EnergySim.Time.Cli -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 1502
# no terminal in a container: headless. --host 0.0.0.0 = reachable from the Docker network,
# --dial-device-id = master dials devices by their id (compose service name)
ENTRYPOINT ["dotnet", "EnergySim.Time.Cli.dll", "--headless", "--host", "0.0.0.0", "--dial-device-id"]
CMD ["--autostart"]
