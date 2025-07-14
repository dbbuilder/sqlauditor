#!/bin/bash

# Use Windows .NET SDK from WSL
DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"

echo "SQL Analyzer - E2E Test Runner (WSL)"
echo "===================================="
echo ""

# Set environment variables
echo "Setting up test environment..."
export SQLSERVER_TEST_CONNECTION="Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
export RUN_INTEGRATION_TESTS="true"
export RUN_EXTERNAL_TESTS="true"
export TEST_ENVIRONMENT="E2E"

echo "Environment configured:"
echo "  - SQL Server: sqltest.schoolvision.net,14333"
echo "  - Database: SVDB_CaptureT"
echo "  - .NET SDK: $("$DOTNET" --version)"
echo ""

# Build the solution
echo "Building solution..."
if ! "$DOTNET" build SqlAnalyzer.sln --configuration Release; then
    echo "Build failed!"
    exit 1
fi

echo ""
echo "Build successful!"
echo ""

# Run the E2E tests
echo "Running E2E tests..."
echo "===================="
echo ""

"$DOTNET" test tests/SqlAnalyzer.Tests/SqlAnalyzer.Tests.csproj \
    --configuration Release \
    --no-build \
    --logger "console;verbosity=normal" \
    --filter "FullyQualifiedName~SimpleE2ETest" \
    --results-directory ./test-results

TEST_RESULT=$?

echo ""
if [ $TEST_RESULT -eq 0 ]; then
    echo "E2E tests passed!"
else
    echo "E2E tests failed!"
fi

exit $TEST_RESULT