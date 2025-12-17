FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["OpenBioCardServer.csproj", "./"]
RUN dotnet restore "OpenBioCardServer.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "OpenBioCardServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user for security (optional but recommended)
# .NET 8 images already include a non-root user 'app', but we stick to default for simplicity
# unless specific permission requirements exist.

COPY --from=build /app/publish .

# .NET 8 container images listen on port 8080 by default
EXPOSE 8080

# Environment variables can be overridden in docker-compose or run command
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "OpenBioCardServer.dll"]
