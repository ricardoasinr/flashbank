# =============================================================================
# FlashBank - Dockerfile Multi-Etapa para Microservicios .NET 8
#
# Uso:
#   docker build \
#     --build-arg PROJECT_NAME=FlashBank.Accounts \
#     -t flashbank-accounts:latest .
#
# El contexto de build DEBE ser la raíz de la solución (donde está flashbank.sln)
# para que FlashBank.Shared esté disponible durante la restauración de NuGet.
# =============================================================================

ARG DOTNET_VERSION=8.0

# -----------------------------------------------------------------------------
# Etapa 1: base — imagen de runtime mínima
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS base
WORKDIR /app

# Puerto estándar de ASP.NET Core en contenedores
EXPOSE 8080
EXPOSE 8081

# -----------------------------------------------------------------------------
# Etapa 2: build — SDK completo para restaurar y compilar
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ARG PROJECT_NAME
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copiar archivos de proyecto primero para aprovechar la caché de capas de Docker.
# Si solo cambia el código fuente (no los .csproj), esta capa se reutiliza.
COPY ["FlashBank.Shared/FlashBank.Shared.csproj",            "FlashBank.Shared/"]
COPY ["FlashBank.Accounts/FlashBank.Accounts.csproj",        "FlashBank.Accounts/"]
COPY ["FlashBank.Transactions/FlashBank.Transactions.csproj","FlashBank.Transactions/"]
COPY ["FlashBank.Accounts.Worker/FlashBank.Accounts.Worker.csproj", "FlashBank.Accounts.Worker/"]
COPY ["FlashBank.History/FlashBank.History.csproj",          "FlashBank.History/"]
COPY ["flashbank.sln",                                        "."]

# Restaurar dependencias NuGet con toda la solución para resolver referencias entre proyectos
RUN dotnet restore "flashbank.sln"

# Copiar el código fuente completo
COPY . .

# Publicar únicamente el proyecto objetivo en modo Release
WORKDIR /src/${PROJECT_NAME}
RUN dotnet publish "${PROJECT_NAME}.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Etapa 3: final — imagen de producción ligera
# -----------------------------------------------------------------------------
FROM base AS final

# Capturar ARG como ENV para que esté disponible en tiempo de ejecución
ARG PROJECT_NAME
ENV PROJECT_DLL="${PROJECT_NAME}.dll"

WORKDIR /app

# Usuario no-root para mayor seguridad
USER $APP_UID

COPY --from=build /app/publish .

# ASP.NET Core escucha en todas las interfaces del contenedor
ENV ASPNETCORE_URLS=http://+:8080

# Se usa la forma shell para que $PROJECT_DLL se expanda correctamente en runtime
ENTRYPOINT dotnet $PROJECT_DLL
