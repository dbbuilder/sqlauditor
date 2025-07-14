# SQL Analyzer Requirements

## Overview
A comprehensive SQL Server, PostgreSQL, and MySQL analyzer that examines database schemas, stored procedures, functions, and other programmable elements to generate detailed documentation, best practices insights, and coding convention recommendations.

## Core Requirements

### 1. Database Analysis Capabilities
- **Schema Analysis**
  - Tables, columns, data types
  - Primary keys, foreign keys, indexes
  - Constraints (check, unique, default)
  - Computed columns
  - Partitioning schemes
  - Auto-increment/sequence configuration
  
- **Programmable Objects Analysis**
  - Stored procedures
  - Functions (scalar, table-valued)
  - Views
  - Triggers
  - User-defined types
  - Events (MySQL)
  - Packages (future: Oracle support)
  
- **Performance Analysis**
  - Index usage statistics
  - Query performance metrics
  - Missing index recommendations
  - Unused indexes identification
  - Table statistics
  - Query cache analysis (MySQL)  
- **Security Analysis**
  - User permissions
  - Role assignments
  - Schema ownership
  - Sensitive data identification
  - Encryption status
  - Authentication methods

### 2. Multi-Database Support
- **SQL Server**
  - All versions from 2016+
  - Azure SQL Database
  - SQL Server on Linux
  
- **PostgreSQL**
  - Versions 12+
  - Amazon RDS PostgreSQL
  - Google Cloud SQL PostgreSQL
  
- **MySQL**
  - Versions 8.0+
  - MariaDB 10.5+
  - Amazon RDS MySQL
  - Azure Database for MySQL

### 3. Reporting Features
- **Documentation Generation**
  - Complete data dictionary
  - Object dependency diagrams
  - ER diagrams (optional)
  - Cross-reference documentation  
- **Best Practices Report**
  - Naming convention violations
  - Performance anti-patterns
  - Security vulnerabilities
  - Design pattern recommendations
  - Database-specific best practices
  
- **Coding Convention Document**
  - Detected naming patterns
  - Common coding styles
  - Recommended conventions based on analysis

### 4. Technical Architecture
- **Modularity**
  - Separate analysis engines for each database type
  - Plugin architecture for custom analyzers
  - External configuration for prompts and rules
  
- **Configuration**
  - JSON-based rule definitions
  - Customizable analysis prompts
  - User-defined convention rules
  - Per-database type configurations
  
- **Deployment**
  - Initial: Command-line interface
  - Phase 2: Web interface with OAuth
  - Target platforms: Vercel (frontend), Supabase (backend)
  - Credential storage: Azure Key Vault