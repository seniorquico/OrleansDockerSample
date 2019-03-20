FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
EXPOSE 11111
EXPOSE 30000

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["src/OrleansDockerSample.Interfaces/OrleansDockerSample.Interfaces.csproj", "src/OrleansDockerSample.Interfaces/"]
COPY ["src/OrleansDockerSample.Silo/OrleansDockerSample.Silo.csproj", "src/OrleansDockerSample.Silo/"]
RUN dotnet restore src/OrleansDockerSample.Silo/OrleansDockerSample.Silo.csproj
COPY . .
WORKDIR /src/src/OrleansDockerSample.Silo
RUN dotnet build OrleansDockerSample.Silo.csproj --configuration Release --no-restore

FROM build AS publish
RUN dotnet publish OrleansDockerSample.Silo.csproj --configuration Release --no-build --no-restore --output /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OrleansDockerSample.Silo.dll"]
