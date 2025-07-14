# SQL Analyzer

A comprehensive database analysis tool for SQL Server, PostgreSQL, and MySQL that generates documentation, identifies best practices violations, and creates coding convention guides.

## Features

- 🔍 **Deep Database Analysis**: Analyzes schemas, stored procedures, functions, views, and more
- 📊 **Performance Insights**: Identifies missing indexes, unused objects, and performance bottlenecks
- 🔒 **Security Auditing**: Reviews permissions, roles, and potential security issues
- 📝 **Documentation Generation**: Creates comprehensive data dictionaries and dependency maps
- 🎯 **Best Practices**: Detects naming convention violations and anti-patterns
- ⚙️ **Configurable Rules**: External JSON configuration for custom analysis rules
- 🗄️ **Multi-Database Support**: SQL Server, PostgreSQL, and MySQL

## Installation

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ (for web interface)
- SQL Server 2016+, PostgreSQL 12+, or MySQL 8.0+

### Command Line Tool
```bash
# Clone the repository
git clone https://github.com/dbbuilder/sqlauditor.git
cd sqlauditor

# Install .NET dependencies
dotnet restore

# Build the CLI tool
dotnet build src/SqlAnalyzer.CLI

# Install as global tool
dotnet tool install --global --add-source ./nupkg SqlAnalyzer.CLI
```### NuGet Packages Required
```bash
# Core packages
dotnet add package Microsoft.Data.SqlClient --version 5.1.5
dotnet add package Npgsql --version 8.0.2
dotnet add package MySql.Data --version 8.3.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.2
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.2
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.2

# Infrastructure packages
dotnet add package Serilog --version 3.1.1
dotnet add package Serilog.Sinks.Console --version 5.0.1
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package Serilog.Sinks.ApplicationInsights --version 4.0.0
dotnet add package Polly --version 8.3.0
dotnet add package Hangfire.Core --version 1.8.9
dotnet add package Hangfire.SqlServer --version 1.8.9

# Configuration and security
dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add package Azure.Security.KeyVault.Secrets --version 4.6.0
dotnet add package Azure.Identity --version 1.10.4

# Reporting
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet add package HtmlAgilityPack --version 1.11.57
dotnet add package iTextSharp --version 5.5.13.3
```## Usage

### Command Line
```bash
# Analyze SQL Server database
sqlanalyzer analyze --server localhost --database MyDB --type sqlserver --output ./reports

# Analyze PostgreSQL database
sqlanalyzer analyze --host localhost --database mydb --type postgres --output ./reports

# Analyze MySQL database
sqlanalyzer analyze --host localhost --database mydb --type mysql --output ./reports

# Use configuration file
sqlanalyzer analyze --config ./config/analysis-config.json

# Generate only specific reports
sqlanalyzer analyze --server localhost --database MyDB --reports schema,performance
```

### Configuration
Create a `config/analysis-config.json` file:
```json
{
  "database": {
    "type": "sqlserver",
    "connectionString": "Server=localhost;Database=MyDB;Trusted_Connection=true;"
  },
  "analysis": {
    "includeSystemObjects": false,
    "performanceThresholds": {
      "slowQueryMs": 1000,
      "missingIndexImpactThreshold": 95
    }
  }
}
```## Architecture

```
SqlAnalyzer/
├── src/
│   ├── SqlAnalyzer.Core/          # Core analysis engine
│   │   ├── Analyzers/             # Database-specific analyzers
│   │   ├── Models/                # Domain models
│   │   ├── Rules/                 # Analysis rules
│   │   └── Connections/           # Database connections
│   ├── SqlAnalyzer.CLI/           # Command-line interface
│   ├── SqlAnalyzer.Web/           # Web interface (Phase 2)
│   └── SqlAnalyzer.Shared/        # Shared utilities
├── config/                        # Configuration files
│   ├── rules/                     # Analysis rules
│   └── prompts/                   # AI prompts (future)
└── tests/                         # Unit and integration tests
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.