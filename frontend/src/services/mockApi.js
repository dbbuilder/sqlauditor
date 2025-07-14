// Mock API service for demo/development without backend
import { v4 as uuidv4 } from 'uuid'

const delay = (ms) => new Promise(resolve => setTimeout(resolve, ms))

const mockDatabase = {
  name: 'DemoDatabase',
  serverVersion: 'SQL Server 2019 (15.0.2000.5)',
  edition: 'Developer Edition (64-bit)',
  sizeMB: 2048,
  tableCount: 42,
  indexCount: 87,
  procedureCount: 23,
  viewCount: 15,
  totalRows: 1250000
}

const mockFindings = [
  {
    id: uuidv4(),
    category: 'Performance',
    severity: 'High',
    title: 'Missing Index Detected',
    description: 'Table [dbo].[Orders] would benefit from an index on columns (CustomerID, OrderDate)',
    impact: 'Query performance could improve by 65%'
  },
  {
    id: uuidv4(),
    category: 'Performance',
    severity: 'Medium',
    title: 'Index Fragmentation',
    description: 'Index [IX_Products_Name] on table [dbo].[Products] is 78% fragmented',
    impact: 'I/O operations are less efficient'
  },
  {
    id: uuidv4(),
    category: 'Security',
    severity: 'High',
    title: 'Elevated Permissions Found',
    description: 'User "app_user" has db_owner role which may be excessive',
    impact: 'Potential security risk'
  }
]

class MockApiService {
  constructor() {
    this.activeJobs = new Map()
    this.completedJobs = new Map()
    this.signalRCallbacks = new Map()
  }

  async testConnection(request) {
    await delay(1000)
    
    // Simulate connection test
    if (request.connectionString.includes('invalid')) {
      return {
        success: false,
        error: 'A network-related or instance-specific error occurred'
      }
    }
    
    return {
      success: true,
      databaseName: 'DemoDatabase',
      serverVersion: 'SQL Server 2019 (15.0.2000.5)'
    }
  }

  async getAnalysisTypes() {
    return [
      { id: 'quick', name: 'Quick Analysis', description: 'Basic health check and overview' },
      { id: 'performance', name: 'Performance Analysis', description: 'Indexes, fragmentation, slow queries' },
      { id: 'security', name: 'Security Audit', description: 'Permissions, vulnerabilities, compliance' },
      { id: 'comprehensive', name: 'Comprehensive Analysis', description: 'Full database analysis' }
    ]
  }

  async startAnalysis(request) {
    const jobId = uuidv4()
    const job = {
      id: jobId,
      status: 'Queued',
      progressPercentage: 0,
      currentStep: 'Initializing',
      startedAt: new Date().toISOString(),
      request
    }
    
    this.activeJobs.set(jobId, job)
    
    // Simulate analysis progress
    this.simulateAnalysis(jobId)
    
    return {
      jobId,
      status: 'Started',
      message: 'Analysis started successfully'
    }
  }

  async simulateAnalysis(jobId) {
    const steps = [
      { progress: 10, step: 'Connecting to database', duration: 1000 },
      { progress: 20, step: 'Gathering database information', duration: 1500 },
      { progress: 40, step: 'Analyzing tables and indexes', duration: 2000 },
      { progress: 60, step: 'Checking query performance', duration: 2000 },
      { progress: 80, step: 'Performing security audit', duration: 1500 },
      { progress: 90, step: 'Generating recommendations', duration: 1000 },
      { progress: 100, step: 'Analysis complete', duration: 500 }
    ]
    
    for (const step of steps) {
      await delay(step.duration)
      
      const job = this.activeJobs.get(jobId)
      if (!job || job.status === 'Cancelled') break
      
      job.status = 'Running'
      job.progressPercentage = step.progress
      job.currentStep = step.step
      
      // Notify SignalR listeners
      this.notifyProgress(jobId, job)
      
      if (step.progress === 100) {
        job.status = 'Completed'
        job.completedAt = new Date().toISOString()
        
        // Generate mock results
        const results = this.generateMockResults(jobId)
        this.completedJobs.set(jobId, results)
      }
    }
  }

  notifyProgress(jobId, status) {
    const callbacks = this.signalRCallbacks.get('AnalysisProgress') || []
    callbacks.forEach(cb => cb(status))
  }

  async getAnalysisStatus(jobId) {
    const job = this.activeJobs.get(jobId)
    if (!job) return null
    
    return {
      jobId,
      status: job.status,
      progressPercentage: job.progressPercentage,
      currentStep: job.currentStep,
      startedAt: job.startedAt,
      completedAt: job.completedAt
    }
  }

  async getAnalysisResults(jobId) {
    await delay(500)
    return this.completedJobs.get(jobId) || this.generateMockResults(jobId)
  }

  generateMockResults(jobId) {
    return {
      jobId,
      database: mockDatabase,
      findings: mockFindings,
      performance: {
        missingIndexes: [
          {
            tableName: '[dbo].[Orders]',
            impactScore: 8546.32,
            equalityColumns: ['CustomerID'],
            inequalityColumns: ['OrderDate'],
            includedColumns: ['TotalAmount', 'Status']
          }
        ],
        fragmentedIndexes: [
          {
            tableName: '[dbo].[Products]',
            indexName: 'IX_Products_Name',
            fragmentationPercent: 78.4,
            pageCount: 1250
          }
        ],
        outdatedStatistics: [
          {
            tableName: '[dbo].[Customers]',
            statisticName: '_WA_Sys_Email',
            lastUpdated: '2024-01-15T10:30:00Z',
            daysSinceUpdate: 45,
            rowCount: 50000,
            modificationCount: 12500
          }
        ],
        slowQueries: []
      },
      security: {
        permissions: [
          {
            principalName: 'app_user',
            principalType: 'SQL_USER',
            permissionName: 'CONTROL',
            state: 'GRANT'
          }
        ],
        vulnerabilities: [],
        hasElevatedPermissions: true,
        hasWeakPasswords: false
      },
      recommendations: [
        {
          category: 'Performance',
          title: 'Create Missing Indexes',
          description: 'Creating the recommended indexes could improve query performance by up to 65%',
          priority: 'High',
          estimatedImpact: '65% performance improvement',
          actions: [
            'CREATE INDEX IX_Orders_CustomerID_OrderDate ON dbo.Orders (CustomerID, OrderDate) INCLUDE (TotalAmount, Status)'
          ]
        },
        {
          category: 'Maintenance',
          title: 'Rebuild Fragmented Indexes',
          description: 'Rebuilding fragmented indexes will improve I/O efficiency',
          priority: 'Medium',
          estimatedImpact: '20% I/O improvement',
          actions: [
            'ALTER INDEX IX_Products_Name ON dbo.Products REBUILD'
          ]
        }
      ],
      analyzedAt: new Date().toISOString()
    }
  }

  async cancelAnalysis(jobId) {
    const job = this.activeJobs.get(jobId)
    if (job) {
      job.status = 'Cancelled'
      job.completedAt = new Date().toISOString()
      return true
    }
    return false
  }

  async getAnalysisHistory(page = 1, pageSize = 10) {
    await delay(300)
    
    const history = []
    for (let i = 0; i < 15; i++) {
      history.push({
        jobId: uuidv4(),
        databaseName: 'DemoDatabase',
        analysisType: ['quick', 'performance', 'comprehensive'][i % 3],
        startedAt: new Date(Date.now() - i * 86400000).toISOString(),
        completedAt: new Date(Date.now() - i * 86400000 + 300000).toISOString(),
        status: 'Completed',
        findingsCount: Math.floor(Math.random() * 10) + 1
      })
    }
    
    const start = (page - 1) * pageSize
    return history.slice(start, start + pageSize)
  }

  async exportResults(jobId, format) {
    await delay(1000)
    
    const results = this.completedJobs.get(jobId) || this.generateMockResults(jobId)
    
    if (format === 'json') {
      const blob = new Blob([JSON.stringify(results, null, 2)], { type: 'application/json' })
      return blob
    }
    
    // For PDF, return a mock PDF blob
    const pdfContent = `%PDF-1.4
SQL Analyzer Report
Job ID: ${jobId}
Database: ${results.database.name}
Generated: ${new Date().toISOString()}
Findings: ${results.findings.length}
%%EOF`
    
    return new Blob([pdfContent], { type: 'application/pdf' })
  }

  // SignalR mock
  onSignalR(event, callback) {
    if (!this.signalRCallbacks.has(event)) {
      this.signalRCallbacks.set(event, [])
    }
    this.signalRCallbacks.get(event).push(callback)
  }

  offSignalR(event, callback) {
    const callbacks = this.signalRCallbacks.get(event) || []
    const index = callbacks.indexOf(callback)
    if (index > -1) {
      callbacks.splice(index, 1)
    }
  }
}

export default new MockApiService()