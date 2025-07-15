import axios from 'axios'
import mockApi from './mockApi'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'
console.log('API Base URL:', API_BASE_URL) // Debug log
const USE_MOCK = import.meta.env.VITE_ENABLE_MOCK_DATA === 'true'

// Create axios instance
const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api/v1`,
  headers: {
    'Content-Type': 'application/json',
    'api-version': '1.0',
  },
  timeout: 30000,
})

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized
      localStorage.removeItem('token')
      localStorage.removeItem('tokenExpiry')
      // Use router instead of direct navigation
      import('@/router').then(({ default: router }) => {
        router.push('/login')
      })
    }
    return Promise.reject(error)
  }
)

class ApiService {
  // Analysis endpoints
  async startAnalysis(request) {
    if (USE_MOCK) return mockApi.startAnalysis(request)
    
    // Transform request to match API expectations
    const apiRequest = {
      ConnectionString: request.connectionString,
      DatabaseType: request.databaseType,
      AnalysisType: request.analysisType,
      Options: {
        IncludeIndexAnalysis: request.options.includeIndexAnalysis,
        IncludeFragmentation: request.options.includeFragmentation,
        IncludeStatistics: request.options.includeStatistics,
        IncludeSecurityAudit: request.options.includeSecurityAudit,
        IncludeQueryPerformance: request.options.includeQueryPerformance,
        IncludeDependencies: request.options.includeDependencies,
        TimeoutMinutes: request.options.timeoutMinutes
      }
    }
    
    const response = await apiClient.post('/analysis/start', apiRequest)
    return response.data
  }

  async getAnalysisStatus(jobId) {
    if (USE_MOCK) return mockApi.getAnalysisStatus(jobId)
    const response = await apiClient.get(`/analysis/status/${jobId}`)
    return response.data
  }

  async getAnalysisResults(jobId) {
    if (USE_MOCK) return mockApi.getAnalysisResults(jobId)
    const response = await apiClient.get(`/analysis/results/${jobId}`)
    return response.data
  }

  async testConnection(connectionString, databaseType) {
    if (USE_MOCK) return mockApi.testConnection({ connectionString, databaseType })
    const response = await apiClient.post('/analysis/test-connection', {
      ConnectionString: connectionString,
      DatabaseType: databaseType,
    })
    return response.data
  }

  async getAnalysisTypes() {
    if (USE_MOCK) return mockApi.getAnalysisTypes()
    const response = await apiClient.get('/analysis/types')
    return response.data
  }

  async cancelAnalysis(jobId) {
    if (USE_MOCK) return mockApi.cancelAnalysis(jobId)
    const response = await apiClient.post(`/analysis/cancel/${jobId}`)
    return response.data
  }

  async getAnalysisHistory(page = 1, pageSize = 10) {
    if (USE_MOCK) return mockApi.getAnalysisHistory(page, pageSize)
    const response = await apiClient.get('/analysis/history', {
      params: { page, pageSize },
    })
    return response.data
  }

  async exportResults(jobId, format = 'pdf') {
    if (USE_MOCK) {
      const blob = await mockApi.exportResults(jobId, format)
      return blob
    }
    
    const response = await apiClient.get(`/analysis/export/${jobId}`, {
      params: { format },
      responseType: 'blob',
    })
    return response.data
  }

  // Mock SignalR support
  onSignalR(event, callback) {
    if (USE_MOCK) {
      mockApi.onSignalR(event, callback)
    }
  }

  offSignalR(event, callback) {
    if (USE_MOCK) {
      mockApi.offSignalR(event, callback)
    }
  }
}

export default new ApiService()