# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY OpenBioCardServer/OpenBioCardServer.csproj OpenBioCardServer/
RUN dotnet restore OpenBioCardServer/OpenBioCardServer.csproj

COPY . .
RUN dotnet publish OpenBioCardServer/OpenBioCardServer.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenBioCardServer.dll"]
