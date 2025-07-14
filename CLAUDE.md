# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SQL Analyzer is a comprehensive database analysis tool for SQL Server, PostgreSQL, and MySQL. It's a .NET 8.0 solution currently in early development that will analyze database schemas, identify performance issues, detect security vulnerabilities, and generate documentation.

## Build and Development Commands

```bash
# Build the entire solution
dotnet build

# Run tests
dotnet test

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific project
dotnet run --project src/SqlAnalyzer.CLI/SqlAnalyzer.CLI.csproj

# Create release build
dotnet build -c Release

# Restore NuGet packages
dotnet restore
```

## Architecture Overview

The solution follows clean architecture principles with clear separation of concerns:

### Core Projects
- **SqlAnalyzer.Core**: Business logic, analyzers, and domain models
  - `Analyzers/` - Database analysis implementations (to be implemented)
  - `Connections/` - Database connection abstractions with `ISqlAnalyzerConnection` interface
  - `Models/` - Domain models (to be implemented)
  - `Rules/` - Analysis rules engine (to be implemented)
  - `Configuration/` - Configuration handling (to be implemented)

- **SqlAnalyzer.CLI**: Command-line interface (currently placeholder)
- **SqlAnalyzer.Shared**: Shared utilities across projects
- **SqlAnalyzer.Web**: Future web interface (Phase 2)

### Key Design Patterns
1. **Dependency Injection**: All components use interface-based design for testability
2. **Async/Await**: All database operations should be async
3. **Plugin Architecture**: Analyzers are designed to be extensible
4. **Configuration-Driven**: Analysis rules will be configurable via JSON

## Database Connection Architecture

When implementing database connections, follow the existing pattern:
- Implement `ISqlAnalyzerConnection` interface from `SqlAnalyzer.Core/Connections/`
- Support async operations for all database interactions
- Handle connection pooling and retry policies with Polly
- Each database type (SQL Server, PostgreSQL, MySQL) has its own implementation

## Testing Strategy

- **Framework**: xUnit with Coverlet for code coverage
- **Test Project**: `tests/SqlAnalyzer.Tests/`
- **Run Single Test**: `dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"`
- **Coverage Goal**: 80%+ code coverage

## Current Implementation Status

The project structure is established but most features are not yet implemented. Key areas needing development:
1. Database connection implementations (PostgreSQL and MySQL)
2. All analyzer implementations
3. CLI command implementation
4. Configuration system
5. Unit tests for all components

## Development Priorities

Based on TODO.md, the immediate priorities are:
1. Complete database connection implementations
2. Implement core analyzers (TableAnalyzer, ColumnAnalyzer, etc.)
3. Build CLI commands for analysis
4. Add comprehensive unit tests

## Important Notes

- The solution supports .NET 8.0 with C# 12 features
- All new analyzers should implement a common interface for consistency
- Database-specific logic should be isolated in connection implementations
- Follow async patterns throughout the codebase
- External configuration will use JSON format in the `config/` directory