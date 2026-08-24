# Builds and runs AGC.Server only — the desktop client is never part of this image.
# Two stages: compile with the full SDK, then ship just the published output on the
# much smaller ASP.NET runtime image.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from just the project files, so this (slow) layer is cached across
# builds that only change application code.
COPY src/AGC.Shared/AGC.Shared.csproj src/AGC.Shared/
COPY src/AGC.Server/AGC.Server.csproj src/AGC.Server/
RUN dotnet restore src/AGC.Server/AGC.Server.csproj

COPY src/AGC.Shared/ src/AGC.Shared/
COPY src/AGC.Server/ src/AGC.Server/
RUN dotnet publish src/AGC.Server/AGC.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# SQLITE_DB_PATH and STORAGE_ROOT are set in fly.toml to point at the mounted volume;
# these ENV lines are just the container-listening address, not deployment-specific.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AGC.Server.dll"]
