import { test as base } from '@playwright/test';
import { execSync } from 'child_process';

// Test database configuration using port range
const TEST_DB_PORT = 15525;
const TEST_DB_PASSWORD = 'Test@123456!';

export const test = base.extend({
  testDatabase: async ({}, use) => {
    // Start SQL Server container
    const containerName = `sqlanalyzer-test-db-${Date.now()}`;
    
    try {
      // Start container
      execSync(`docker run -d --name ${containerName} \
        -e "ACCEPT_EULA=Y" \
        -e "MSSQL_SA_PASSWORD=${TEST_DB_PASSWORD}" \
        -e "MSSQL_PID=Developer" \
        -p ${TEST_DB_PORT}:1433 \
        mcr.microsoft.com/mssql/server:2019-latest`);
      
      // Wait for SQL Server to be ready
      await new Promise(resolve => setTimeout(resolve, 10000));
      
      const connectionString = `Server=localhost,${TEST_DB_PORT};Database=TestDB;User Id=sa;Password=${TEST_DB_PASSWORD};TrustServerCertificate=true;`;
      
      // Create test database
      execSync(`docker exec ${containerName} /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P "${TEST_DB_PASSWORD}" \
        -Q "CREATE DATABASE TestDB"`);
      
      // Create test tables
      execSync(`docker exec ${containerName} /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P "${TEST_DB_PASSWORD}" -d TestDB \
        -Q "CREATE TABLE TestTable1 (Id INT PRIMARY KEY IDENTITY, Name NVARCHAR(100)); \
            CREATE INDEX IX_TestTable1_Name ON TestTable1(Name); \
            INSERT INTO TestTable1 (Name) VALUES ('Test1'), ('Test2'), ('Test3');"`);
      
      await use({
        connectionString,
        port: TEST_DB_PORT,
        containerName
      });
    } finally {
      // Cleanup
      try {
        execSync(`docker stop ${containerName}`);
        execSync(`docker rm ${containerName}`);
      } catch (e) {
        console.error('Failed to cleanup test container:', e);
      }
    }
  },
});