# Device simulator container (pv, battery, charger, wind). Build context = repo root:
#   docker build -f src/EnergySim.Cli/Dockerfile -t energysim/device:lb04 .
# The device is chosen at start (compose "command"), the image is the same for all four.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/EnergySim.Cli -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 1511 1512 1513 1514
# stdin must stay open (compose: stdin_open: true), end of stdin = quit
ENTRYPOINT ["dotnet", "EnergySim.Cli.dll"]
CMD ["--help"]
