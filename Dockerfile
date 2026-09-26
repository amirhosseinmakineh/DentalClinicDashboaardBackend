# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DentalDashboard.slnx ./
COPY DentalDashboard/DentalDashboard.csproj DentalDashboard/
COPY DentalDashboard.ApplicationService/DentalDashboard.ApplicationService.csproj DentalDashboard.ApplicationService/
COPY DentalDashboard.ApplicationService.Contract/DentalDashboard.ApplicationService.Contract.csproj DentalDashboard.ApplicationService.Contract/
COPY DentalDashboard.Domain/DentalDashboard.Domain.csproj DentalDashboard.Domain/
COPY DentalDashboard.Framwork/DentalDashboard.Framwork.csproj DentalDashboard.Framwork/
COPY DentalDashboard.Infrastracture/DentalDashboard.Infrastracture.csproj DentalDashboard.Infrastracture/
COPY DentalDashboard.Security/DentalDashboard.Security.csproj DentalDashboard.Security/
COPY DentalDashboard.Utilities/DentalDashboard.Utilities.csproj DentalDashboard.Utilities/

RUN dotnet restore DentalDashboard/DentalDashboard.csproj

COPY . .
RUN dotnet publish DentalDashboard/DentalDashboard.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    && rm -f /app/publish/appsettings*.json \
    && cp DentalDashboard/appsettings.Container.json /app/publish/appsettings.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    TZ=Asia/Tehran

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD ["curl", "--fail", "--silent", "--show-error", "http://127.0.0.1:8080/healthz"]

ENTRYPOINT ["dotnet", "mywebapp.dll"]
