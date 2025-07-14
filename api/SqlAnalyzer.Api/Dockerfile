# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj", "src/SqlAnalyzer.Api/"]
COPY ["src/SqlAnalyzer.Core/SqlAnalyzer.Core.csproj", "src/SqlAnalyzer.Core/"]
COPY ["src/SqlAnalyzer.Shared/SqlAnalyzer.Shared.csproj", "src/SqlAnalyzer.Shared/"]

# Restore dependencies
RUN dotnet restore "src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/SqlAnalyzer.Api"
RUN dotnet build "SqlAnalyzer.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "SqlAnalyzer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SqlAnalyzer.Api.dll"]