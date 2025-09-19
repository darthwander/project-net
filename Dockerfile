FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release
ARG ARTIFACTS_ENDPOINT
ARG ACCESS_TOKEN
ARG USER
ARG VERSION

WORKDIR /src
COPY ["/EnrichIpedWorker/EnrichIped.BackgroundServices.csproj", "EnrichIpedWorker/"]

RUN echo "<?xml version='1.0' encoding='utf-8'?><configuration><packageSources><add key='nuget.org' value='https://api.nuget.org/v3/index.json' protocolVersion='3' /><add key='Kula' value='$ARTIFACTS_ENDPOINT' /></packageSources><packageSourceCredentials><Kula><add key='Username' value='$USER' /><add key='ClearTextPassword' value='$ACCESS_TOKEN' /></Kula></packageSourceCredentials></configuration>" > NuGet.Config

RUN dotnet restore "./EnrichIpedWorker/EnrichIped.BackgroundServices.csproj"
COPY . .
WORKDIR "/src/EnrichIpedWorker"
RUN dotnet build "./EnrichIped.BackgroundServices.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EnrichIped.BackgroundServices.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnrichIped.BackgroundServices.dll"]