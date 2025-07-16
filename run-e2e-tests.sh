#!/bin/bash

# SQL Analyzer E2E Test Runner for Linux/Mac

set -e

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║            SQL ANALYZER E2E TEST RUNNER                      ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

TEST_TYPE=${1:-all}  # all, api, web
VERBOSE=${2:-false}
KEEP_CONTAINERS=${3:-false}

START_TIME=$(date +%s)
EXIT_CODE=0

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Function to check if Docker is running
check_docker() {
    if ! docker version >/dev/null 2>&1; then
        echo -e "${RED}ERROR: Docker is not running or not installed${NC}"
        echo -e "${YELLOW}Please start Docker and try again${NC}"
        return 1
    fi
    return 0
}

# Function to check if port is available
check_port() {
    local port=$1
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
        return 1
    fi
    return 0
}

# Function to cleanup containers
cleanup_containers() {
    echo -e "${YELLOW}Cleaning up test containers...${NC}"
    docker ps -a --filter "name=sqlanalyzer-test-" --format "{{.Names}}" | xargs -r docker rm -f 2>/dev/null || true
}

# Check prerequisites
echo -e "${YELLOW}▶ CHECKING PREREQUISITES${NC}"

if ! check_docker; then
    exit 1
fi

# Check critical ports
CRITICAL_PORTS=(15500 15525 15565)
PORTS_IN_USE=()

for port in "${CRITICAL_PORTS[@]}"; do
    if ! check_port $port; then
        PORTS_IN_USE+=($port)
    fi
done

if [ ${#PORTS_IN_USE[@]} -gt 0 ]; then
    echo -e "${RED}ERROR: The following ports are in use: ${PORTS_IN_USE[*]}${NC}"
    echo -e "${YELLOW}Please free these ports and try again${NC}"
    exit 1
fi

echo -e "  ${GREEN}✓ Docker is running${NC}"
echo -e "  ${GREEN}✓ Required ports are available${NC}"

# Cleanup any existing test containers
if [ "$KEEP_CONTAINERS" != "true" ]; then
    cleanup_containers
fi

# Run API E2E Tests
if [ "$TEST_TYPE" = "all" ] || [ "$TEST_TYPE" = "api" ]; then
    echo ""
    echo -e "${YELLOW}▶ RUNNING API E2E TESTS${NC}"
    echo -e "  ${GRAY}Using ports: 15500-15540${NC}"
    
    cd tests/SqlAnalyzer.Api.Tests.E2E
    
    # Restore packages
    echo -e "  ${GRAY}Restoring packages...${NC}"
    dotnet restore
    
    # Build
    echo -e "  ${GRAY}Building test project...${NC}"
    dotnet build --no-restore
    
    # Run tests
    echo -e "  ${GRAY}Running tests...${NC}"
    if [ "$VERBOSE" = "true" ]; then
        dotnet test --no-build --logger "console;verbosity=detailed" \
                    --logger "html;LogFileName=api-test-results.html" \
                    --results-directory ./TestResults
    else
        dotnet test --no-build --logger "console;verbosity=normal" \
                    --logger "html;LogFileName=api-test-results.html" \
                    --results-directory ./TestResults
    fi
    
    if [ $? -eq 0 ]; then
        echo -e "  ${GREEN}✓ API E2E tests passed${NC}"
    else
        echo -e "  ${RED}✗ API E2E tests failed${NC}"
        EXIT_CODE=1
    fi
    
    cd ../..
fi

# Run Web UI E2E Tests
if [ "$TEST_TYPE" = "all" ] || [ "$TEST_TYPE" = "web" ]; then
    echo ""
    echo -e "${YELLOW}▶ RUNNING WEB UI E2E TESTS${NC}"
    echo -e "  ${GRAY}Using ports: 15561-15580${NC}"
    
    cd tests/SqlAnalyzer.Web.Tests.E2E
    
    # Install dependencies
    echo -e "  ${GRAY}Installing npm packages...${NC}"
    npm install
    
    # Install Playwright browsers if needed
    echo -e "  ${GRAY}Installing Playwright browsers...${NC}"
    npx playwright install
    
    # Run tests
    echo -e "  ${GRAY}Running tests...${NC}"
    if [ "$VERBOSE" = "true" ]; then
        npm run test -- --reporter=list,html
    else
        npm run test
    fi
    
    if [ $? -eq 0 ]; then
        echo -e "  ${GREEN}✓ Web UI E2E tests passed${NC}"
    else
        echo -e "  ${RED}✗ Web UI E2E tests failed${NC}"
        EXIT_CODE=1
    fi
    
    cd ../..
fi

# Cleanup
if [ "$KEEP_CONTAINERS" != "true" ]; then
    cleanup_containers
fi

# Summary
END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
MINUTES=$((DURATION / 60))
SECONDS=$((DURATION % 60))

echo ""
echo -e "${CYAN}▶ E2E TEST SUMMARY${NC}"
echo -e "  Duration: ${MINUTES}m ${SECONDS}s"
echo -e "  Test Type: $TEST_TYPE"

if [ $EXIT_CODE -eq 0 ]; then
    echo -e "  Result: ${GREEN}PASSED ✓${NC}"
else
    echo -e "  Result: ${RED}FAILED ✗${NC}"
fi

# Show test results locations
if [ -f "tests/SqlAnalyzer.Api.Tests.E2E/TestResults/api-test-results.html" ]; then
    echo ""
    echo -e "${GRAY}API test results: tests/SqlAnalyzer.Api.Tests.E2E/TestResults/api-test-results.html${NC}"
fi

if [ -f "tests/SqlAnalyzer.Web.Tests.E2E/playwright-report/index.html" ]; then
    echo -e "${GRAY}Web UI test results: tests/SqlAnalyzer.Web.Tests.E2E/playwright-report/index.html${NC}"
fi

exit $EXIT_CODE