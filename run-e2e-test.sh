#!/bin/bash

echo "SQL Analyzer - End-to-End Test Runner"
echo "====================================="
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
echo "  - Integration Tests: Enabled"
echo "  - External Tests: Enabled"
echo ""

# Build the solution first
echo "Building solution..."
if ! dotnet build SqlAnalyzer.sln --configuration Release; then
    echo "Build failed!"
    exit 1
fi

echo "Build successful!"
echo ""

# Run the comprehensive E2E test using xUnit
echo "Running E2E test suite..."
echo "========================="
echo ""

dotnet test tests/SqlAnalyzer.Tests/SqlAnalyzer.Tests.csproj \
    --configuration Release \
    --no-build \
    --logger "console;verbosity=detailed" \
    --filter "Category=E2E" \
    --results-directory ./test-results \
    --diag ./test-results/diag.log

TEST_RESULT=$?

echo ""
if [ $TEST_RESULT -eq 0 ]; then
    echo "All E2E tests passed!"
else
    echo "Some E2E tests failed!"
    echo "Check test-results/diag.log for detailed diagnostics"
fi

echo ""
echo "E2E test execution complete!"
exit $TEST_RESULT