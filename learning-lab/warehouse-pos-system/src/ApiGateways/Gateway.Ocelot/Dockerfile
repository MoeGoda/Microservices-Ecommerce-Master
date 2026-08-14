# Build context is the repo root — see Identity.API's Dockerfile for the
# general reasoning. The gateway sits one level shallower in the source
# tree than a Services/<X>/<Y>.API project (ApiGateways/Gateway.Ocelot vs.
# Services/X/Y.API), which is why its SharedSettings walk-up in Program.cs
# is "../.." rather than "../../.." — the WORKDIR below matches that.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/ ./src/
WORKDIR /app/src/ApiGateways/Gateway.Ocelot
RUN dotnet publish Gateway.Ocelot.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app/src/ApiGateways/Gateway.Ocelot
COPY --from=build /app/publish .
COPY --from=build /app/src/SharedSettings /app/src/SharedSettings
ENV ASPNETCORE_URLS=http://+:5058
# Selects ocelot.Docker.json (merged on top of ocelot.json) — see
# Program.cs's own comment on the AddJsonFile call this triggers.
ENV ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 5058
ENTRYPOINT ["dotnet", "Gateway.Ocelot.dll"]
