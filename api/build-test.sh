#!/bin/bash

echo "Testing API build with Docker..."
echo "Start time: $(date '+%I:%M %p')"

# Simple build test
docker run --rm -v "$PWD:/src" -w /src/SqlAnalyzer.Api mcr.microsoft.com/dotnet/sdk:8.0 bash -c "
    echo '=== Restoring packages ==='
    dotnet restore
    
    echo '=== Building project ==='
    dotnet build -c Release
    
    echo '=== Publishing project ==='
    dotnet publish -c Release -o /src/test-publish
    
    echo '=== Build complete ==='
    ls -la /src/test-publish | head -10
"

echo "End time: $(date '+%I:%M %p')"