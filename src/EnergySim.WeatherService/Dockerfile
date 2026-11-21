# WeatherSim container. Build context = repo root:
#   docker build -f src/EnergySim.WeatherService/Dockerfile -t energysim/weather:lb04 .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/EnergySim.WeatherService -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5200
# docker stop sends SIGTERM, the service ends clean with exit code 0
ENTRYPOINT ["dotnet", "EnergySim.WeatherService.dll"]
CMD ["--port", "5200"]
