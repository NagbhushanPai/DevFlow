# Builds either the API (default) or the Admin portal:
#   docker build -f backend/Dockerfile .                                  -> DevFlow.API
#   docker build -f backend/Dockerfile --build-arg PROJECT=DevFlow.Admin . -> DevFlow.Admin
ARG PROJECT=DevFlow.API

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src

# Restore first (cached unless project files change).
COPY backend/DevFlow.slnx backend/Directory.Build.props backend/.editorconfig backend/
COPY backend/src/DevFlow.Domain/DevFlow.Domain.csproj backend/src/DevFlow.Domain/
COPY backend/src/DevFlow.Application/DevFlow.Application.csproj backend/src/DevFlow.Application/
COPY backend/src/DevFlow.Infrastructure/DevFlow.Infrastructure.csproj backend/src/DevFlow.Infrastructure/
COPY backend/src/DevFlow.API/DevFlow.API.csproj backend/src/DevFlow.API/
COPY backend/src/DevFlow.Admin/DevFlow.Admin.csproj backend/src/DevFlow.Admin/
RUN dotnet restore backend/src/${PROJECT}/${PROJECT}.csproj

COPY backend/src/ backend/src/
RUN dotnet publish backend/src/${PROJECT}/${PROJECT}.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG PROJECT
WORKDIR /app

# curl is used only by the container healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    APP_DLL=${PROJECT}.dll
EXPOSE 8080

COPY --from=build /app/publish .

# Run as the image's built-in non-root user.
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\" \"$@\"", "--"]
