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

# DATABASE_URL / SUPABASE_* / STRIPE_* / etc. are set as environment variables in
# your hosting platform's dashboard (e.g. Render's Environment tab) — this ENV line
# is just the container-listening address, not deployment-specific.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Two defensive settings for constrained/sandboxed free-tier hosts:
# - Disabling the diagnostics IPC listener avoids a known class of startup-time
#   native crash (SIGSEGV) on container runtimes that restrict the socket/namespace
#   operations it needs — this app is never profiled live in production anyway.
# - Workstation GC has a much lighter startup memory footprint than the default
#   Server GC (which reserves a heap per logical core), which matters on a small
#   RAM allowance and isn't a meaningful throughput trade-off at this app's scale.
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_gcServer=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "AGC.Server.dll"]
