FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /StreamTier

COPY StreamTier.sln global.json ./
COPY StreamTier.API/StreamTier.API.csproj StreamTier.API/
COPY StreamTierUnitTests/StreamTierUnitTests.csproj StreamTierUnitTests/
RUN dotnet restore StreamTier.sln

COPY StreamTier.API/ StreamTier.API/
COPY StreamTierUnitTests/ StreamTierUnitTests/


FROM build AS test
ENTRYPOINT ["dotnet", "test", "--project", "StreamTierUnitTests"]


FROM build AS publish
RUN dotnet publish StreamTier.API/StreamTier.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "StreamTier.API.dll"]
