#!/bin/bash
echo "SQL Analyzer - End-to-End Test Runner"
export SQLSERVER_TEST_CONNECTION="Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
export RUN_INTEGRATION_TESTS="true"
export RUN_EXTERNAL_TESTS="true"
export TEST_ENVIRONMENT="E2E"
dotnet test tests/SqlAnalyzer.Tests/SqlAnalyzer.Tests.csproj --configuration Release --filter "Category=E2E" --logger "console;verbosity=normal"