import { expect } from '@playwright/test';
import { test } from './fixtures/test-database';

test.describe('Analysis Workflow E2E', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display homepage with navigation', async ({ page }) => {
    // Check main elements
    await expect(page.locator('nav')).toBeVisible();
    await expect(page.locator('h1:has-text("SQL Analyzer")')).toBeVisible();
    
    // Check navigation links
    await expect(page.locator('nav >> text=Dashboard')).toBeVisible();
    await expect(page.locator('nav >> text=New Analysis')).toBeVisible();
    await expect(page.locator('nav >> text=History')).toBeVisible();
  });

  test('should navigate to new analysis page', async ({ page }) => {
    // Click new analysis
    await page.click('nav >> text=New Analysis');
    
    // Check URL and page content
    await expect(page).toHaveURL(/.*\/analysis\/new/);
    await expect(page.locator('h2:has-text("New Database Analysis")')).toBeVisible();
    
    // Check form elements
    await expect(page.locator('label:has-text("Database Type")')).toBeVisible();
    await expect(page.locator('label:has-text("Connection String")')).toBeVisible();
    await expect(page.locator('button:has-text("Test Connection")')).toBeVisible();
    await expect(page.locator('button:has-text("Start Analysis")')).toBeVisible();
  });

  test('should test database connection', async ({ page, testDatabase }) => {
    // Navigate to new analysis
    await page.goto('/analysis/new');
    
    // Select SQL Server
    await page.selectOption('select[name="databaseType"]', 'SqlServer');
    
    // Enter connection string
    await page.fill('input[name="connectionString"]', testDatabase.connectionString);
    
    // Click test connection
    await page.click('button:has-text("Test Connection")');
    
    // Wait for result dialog
    await expect(page.locator('.test-success')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('text=Connection successful!')).toBeVisible();
    await expect(page.locator('text=Database: TestDB')).toBeVisible();
  });

  test('should handle invalid connection', async ({ page }) => {
    // Navigate to new analysis
    await page.goto('/analysis/new');
    
    // Enter invalid connection string
    await page.fill('input[name="connectionString"]', 
      'Server=invalid.server;Database=test;User Id=sa;Password=wrong;');
    
    // Click test connection
    await page.click('button:has-text("Test Connection")');
    
    // Wait for error
    await expect(page.locator('.test-error')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('text=Connection failed!')).toBeVisible();
  });

  test('should run complete analysis workflow', async ({ page, testDatabase }) => {
    // Navigate to new analysis
    await page.goto('/analysis/new');
    
    // Fill form
    await page.selectOption('select[name="databaseType"]', 'SqlServer');
    await page.fill('input[name="connectionString"]', testDatabase.connectionString);
    await page.selectOption('select[name="analysisType"]', 'quick');
    
    // Configure options
    await page.check('input[id="indexAnalysis"]');
    await page.uncheck('input[id="fragmentation"]');
    
    // Start analysis
    await page.click('button:has-text("Start Analysis")');
    
    // Should navigate to progress page
    await expect(page).toHaveURL(/.*\/analysis\/[a-f0-9-]+$/);
    
    // Check progress elements
    await expect(page.locator('h2:has-text("Analysis Progress")')).toBeVisible();
    await expect(page.locator('.progress-bar')).toBeVisible();
    await expect(page.locator('text=Job ID:')).toBeVisible();
    
    // Wait for completion (max 30 seconds for quick analysis)
    await expect(page.locator('.tag:has-text("Completed")')).toBeVisible({ timeout: 30000 });
    
    // Check results tabs
    await expect(page.locator('.tab-panel >> text=Summary')).toBeVisible();
    await expect(page.locator('.tab-panel >> text=Findings')).toBeVisible();
    await expect(page.locator('.tab-panel >> text=Performance')).toBeVisible();
    await expect(page.locator('.tab-panel >> text=Security')).toBeVisible();
    await expect(page.locator('.tab-panel >> text=Recommendations')).toBeVisible();
    
    // Check summary content
    await page.click('.tab-panel >> text=Summary');
    await expect(page.locator('text=Database: TestDB')).toBeVisible();
    await expect(page.locator('text=Tables:')).toBeVisible();
    
    // Check export buttons
    await expect(page.locator('button:has-text("Export PDF")')).toBeVisible();
    await expect(page.locator('button:has-text("Export JSON")')).toBeVisible();
  });

  test('should cancel running analysis', async ({ page, testDatabase }) => {
    // Start a comprehensive (long) analysis
    await page.goto('/analysis/new');
    await page.fill('input[name="connectionString"]', testDatabase.connectionString);
    await page.selectOption('select[name="analysisType"]', 'comprehensive');
    await page.click('button:has-text("Start Analysis")');
    
    // Wait for it to start
    await expect(page.locator('.tag:has-text("Running")')).toBeVisible({ timeout: 10000 });
    
    // Cancel
    await page.click('button:has-text("Cancel Analysis")');
    
    // Confirm cancellation
    await expect(page.locator('.tag:has-text("Cancelled")')).toBeVisible({ timeout: 10000 });
  });

  test('should display analysis history', async ({ page, testDatabase }) => {
    // Run a quick analysis first
    await page.goto('/analysis/new');
    await page.fill('input[name="connectionString"]', testDatabase.connectionString);
    await page.selectOption('select[name="analysisType"]', 'quick');
    await page.click('button:has-text("Start Analysis")');
    
    // Wait for completion
    await expect(page.locator('.tag:has-text("Completed")')).toBeVisible({ timeout: 30000 });
    
    // Navigate to history
    await page.click('nav >> text=History');
    await expect(page).toHaveURL(/.*\/history/);
    
    // Check history table
    await expect(page.locator('table')).toBeVisible();
    await expect(page.locator('th:has-text("Database")')).toBeVisible();
    await expect(page.locator('th:has-text("Analysis Type")')).toBeVisible();
    await expect(page.locator('th:has-text("Status")')).toBeVisible();
    await expect(page.locator('th:has-text("Started")')).toBeVisible();
    
    // Should have at least one entry
    await expect(page.locator('td:has-text("TestDB")')).toBeVisible();
    await expect(page.locator('td:has-text("quick")')).toBeVisible();
    await expect(page.locator('td:has-text("Completed")')).toBeVisible();
  });

  test('should handle real-time updates via SignalR', async ({ page, testDatabase }) => {
    // Start analysis
    await page.goto('/analysis/new');
    await page.fill('input[name="connectionString"]', testDatabase.connectionString);
    await page.selectOption('select[name="analysisType"]', 'performance');
    await page.click('button:has-text("Start Analysis")');
    
    // Collect progress updates
    const progressValues: number[] = [];
    page.on('console', msg => {
      if (msg.type() === 'log' && msg.text().includes('Progress update:')) {
        const match = msg.text().match(/progressPercentage: (\d+)/);
        if (match) {
          progressValues.push(parseInt(match[1]));
        }
      }
    });
    
    // Wait for completion
    await expect(page.locator('.tag:has-text("Completed")')).toBeVisible({ timeout: 60000 });
    
    // Should have received progress updates
    expect(progressValues.length).toBeGreaterThan(0);
    expect(progressValues[progressValues.length - 1]).toBe(100);
  });

  test('should export analysis results', async ({ page, testDatabase }) => {
    // Run analysis
    await page.goto('/analysis/new');
    await page.fill('input[name="connectionString"]', testDatabase.connectionString);
    await page.selectOption('select[name="analysisType"]', 'quick');
    await page.click('button:has-text("Start Analysis")');
    
    // Wait for completion
    await expect(page.locator('.tag:has-text("Completed")')).toBeVisible({ timeout: 30000 });
    
    // Test JSON export
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.click('button:has-text("Export JSON")')
    ]);
    
    // Verify download
    expect(download.suggestedFilename()).toMatch(/analysis-.*\.json$/);
    
    // Read and verify content
    const content = await download.stream();
    const buffer = await streamToBuffer(content);
    const jsonContent = JSON.parse(buffer.toString());
    
    expect(jsonContent).toHaveProperty('jobId');
    expect(jsonContent).toHaveProperty('database');
    expect(jsonContent.database.name).toBe('TestDB');
  });

  test('should handle responsive design', async ({ page }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    
    // Navigation should be accessible (hamburger menu)
    await expect(page.locator('.mobile-menu-toggle')).toBeVisible();
    
    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await expect(page.locator('nav')).toBeVisible();
    
    // Test desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await expect(page.locator('nav')).toBeVisible();
  });
});

// Helper function to convert stream to buffer
async function streamToBuffer(stream: NodeJS.ReadableStream): Promise<Buffer> {
  const chunks: Buffer[] = [];
  for await (const chunk of stream) {
    chunks.push(chunk);
  }
  return Buffer.concat(chunks);
}