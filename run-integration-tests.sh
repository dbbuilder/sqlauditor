#!/bin/bash

# Bash script to run integration tests with SQL Server

echo -e "\033[32mSQL Analyzer - Integration Tests Runner\033[0m"
echo -e "\033[32m======================================\033[0m"

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo -e "\033[31mERROR: .env file not found!\033[0m"
    echo -e "\033[33mPlease create a .env file with your test database connection string.\033[0m"
    exit 1
fi

# Load environment variables
echo -e "\n\033[33mLoading environment variables from .env file...\033[0m"
export $(grep -v '^#' .env | xargs)

# Check if integration tests are enabled
if [ "$RUN_INTEGRATION_TESTS" != "true" ]; then
    echo -e "\n\033[33mIntegration tests are disabled.\033[0m"
    echo -e "\033[33mSet RUN_INTEGRATION_TESTS=true in .env file to enable them.\033[0m"
    exit 0
fi

# Display connection info (masked)
if [ ! -z "$SQLSERVER_TEST_CONNECTION" ]; then
    MASKED_CONN=$(echo "$SQLSERVER_TEST_CONNECTION" | sed 's/Password=[^;]*/Password=****/g')
    echo -e "\n\033[36mUsing connection: $MASKED_CONN\033[0m"
fi

# Run integration tests
echo -e "\n\033[32mRunning integration tests...\033[0m"
echo -e "\033[33mNote: These tests are READ-ONLY and will not modify any data.\n\033[0m"

# Run specific integration test classes
TEST_CLASSES=(
    "SqlServerConnectionIntegrationTests"
    "TableAnalyzerIntegrationTests"
    "ConnectionFactoryIntegrationTests"
)

for TEST_CLASS in "${TEST_CLASSES[@]}"; do
    echo -e "\n\033[36mRunning $TEST_CLASS...\033[0m"
    dotnet test --filter "FullyQualifiedName~IntegrationTests.$TEST_CLASS" --logger "console;verbosity=normal"
    
    if [ $? -ne 0 ]; then
        echo -e "\033[31mTests failed for $TEST_CLASS!\033[0m"
    fi
done

# Run all integration tests with coverage
echo -e "\n\n\033[32mRunning all integration tests with code coverage...\033[0m"
dotnet test --filter "FullyQualifiedName~IntegrationTests" --collect:"XPlat Code Coverage" --results-directory ./TestResults

echo -e "\n\n\033[32mIntegration tests completed!\033[0m"
echo -e "\033[33mCheck ./TestResults for coverage reports.\033[0m"