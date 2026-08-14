# Build context is the repo root (learning-lab/warehouse-pos-system), not
# this directory — every service's Dockerfile needs the whole src/ tree
# (BuildingBlocks + SharedSettings + its own Domain/Application/Infrastructure
# project references), so `docker compose build` sets `context: .` and
# `dockerfile: src/Services/Identity/Identity.API/Dockerfile` per service.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/ ./src/
WORKDIR /app/src/Services/Identity/Identity.API
RUN dotnet publish Identity.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
# The WORKDIR depth mirrors the source tree on purpose: Program.cs resolves
# SharedSettings/jwt.settings.json via Path.Combine(ContentRootPath, "..",
# "..", "..", "SharedSettings", "jwt.settings.json") — a relative walk-up
# that assumes ContentRootPath sits 3 levels under src/. Keeping that same
# depth here means the existing code runs unmodified in a container
# instead of special-casing an absolute path just for Docker.
WORKDIR /app/src/Services/Identity/Identity.API
COPY --from=build /app/publish .
COPY --from=build /app/src/SharedSettings /app/src/SharedSettings
ENV ASPNETCORE_URLS=http://+:5218
EXPOSE 5218
ENTRYPOINT ["dotnet", "Identity.API.dll"]
