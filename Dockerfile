# syntax=docker/dockerfile:1

# =============================================================================
#  TEMPEST Store · Blazor Web App (.NET 10 LTS) · multi-stage build
#    build   → mcr.microsoft.com/dotnet/sdk:10.0     (restore + publish)
#    runtime → mcr.microsoft.com/dotnet/aspnet:10.0  (only the published app, non-root)
# =============================================================================

# ---------- 1. Build ----------------------------------------------------------
# Runs on the builder's native platform and cross-publishes for the target platform,
# so multi-arch images (linux/amd64, linux/arm64) build without emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG CONFIGURATION=Release
WORKDIR /src

# Restore in its own layer: it is only invalidated when a project or props file changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Domain/Domain.csproj                 src/Domain/
COPY src/Application/Application.csproj       src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/WebUI/WebUI.csproj                   src/WebUI/
RUN dotnet restore src/WebUI/WebUI.csproj -a $TARGETARCH

# Sources + publish. Framework-dependent and without apphost: the runtime image provides `dotnet`.
COPY src/ src/
RUN dotnet publish src/WebUI/WebUI.csproj \
        --no-restore \
        -c $CONFIGURATION \
        -a $TARGETARCH \
        -o /app/publish \
        -p:UseAppHost=false

# ---------- 2. Runtime --------------------------------------------------------
# Ubuntu-based ASP.NET Core runtime: includes ICU, needed to format prices with the store culture.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Folders mounted as named volumes must exist and belong to the non-root user, so Docker
# initializes the volumes with that ownership. The application files stay root-owned (read-only).
#   /app/data            → SQLite database + Data Protection keys
#   /app/wwwroot/uploads → images uploaded from the admin panel
RUN mkdir -p /app/data /app/wwwroot/uploads \
    && chown -R $APP_UID:$APP_UID /app/data /app/wwwroot/uploads

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=80

# Port 80 works for the non-root user: Docker (20.10+) sets net.ipv4.ip_unprivileged_port_start=0 in containers.
EXPOSE 80

# Non-root user shipped with the official .NET images (UID 1654).
USER $APP_UID

# No curl/wget in the base image: probe /healthz (includes a SQLite check) with bash's /dev/tcp.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD bash -c "exec 3<>/dev/tcp/127.0.0.1/80 && printf 'GET /healthz HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3 && head -n 1 <&3 | grep -q ' 200 '"

ENTRYPOINT ["dotnet", "Tempest.WebUI.dll"]
