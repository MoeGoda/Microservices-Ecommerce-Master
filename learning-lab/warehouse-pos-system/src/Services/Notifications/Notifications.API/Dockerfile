# Build context is the repo root — see Identity.API's Dockerfile for the
# full reasoning (identical pattern, only the project path differs).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/ ./src/
WORKDIR /app/src/Services/Notifications/Notifications.API
RUN dotnet publish Notifications.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app/src/Services/Notifications/Notifications.API
COPY --from=build /app/publish .
COPY --from=build /app/src/SharedSettings /app/src/SharedSettings
ENV ASPNETCORE_URLS=http://+:5298
EXPOSE 5298
ENTRYPOINT ["dotnet", "Notifications.API.dll"]
