import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as signalR from '@microsoft/signalr'
import api from '@/services/api'

export const useAnalysisStore = defineStore('analysis', () => {
  // State
  const connection = ref(null)
  const isConnected = ref(false)
  const activeJobs = ref(new Map())
  const completedAnalyses = ref([])
  const currentJob = ref(null)

  // Getters
  const hasActiveJobs = computed(() => activeJobs.value.size > 0)
  const currentJobStatus = computed(() => 
    currentJob.value ? activeJobs.value.get(currentJob.value) : null
  )

  // Actions
  async function initializeSignalR() {
    try {
      // Get the auth token for SignalR
      const token = localStorage.getItem('token')
      
      if (!token) {
        console.log('No auth token, skipping SignalR initialization')
        return
      }
      
      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(`${import.meta.env.VITE_API_URL}/hubs/analysis`, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      connection.value.on('AnalysisProgress', (status) => {
        activeJobs.value.set(status.jobId, status)
      })

      connection.value.on('Connected', (connectionId) => {
        console.log('Connected to SignalR hub:', connectionId)
        isConnected.value = true
      })

      await connection.value.start()
    } catch (error) {
      console.error('Failed to connect to SignalR:', error)
    }
  }

  async function startAnalysis(request) {
    try {
      const response = await api.post('/analysis/start', request)
      const { jobId } = response.data
      
      currentJob.value = jobId
      activeJobs.value.set(jobId, {
        jobId,
        status: 'Queued',
        progressPercentage: 0,
        currentStep: 'Initializing'
      })

      // Subscribe to job updates
      if (connection.value && isConnected.value) {
        await connection.value.invoke('SubscribeToJob', jobId)
      }

      return jobId
    } catch (error) {
      throw error
    }
  }

  async function getAnalysisResults(jobId) {
    try {
      const response = await api.get(`/analysis/results/${jobId}`)
      return response.data
    } catch (error) {
      throw error
    }
  }

  async function cancelAnalysis(jobId) {
    try {
      await api.post(`/analysis/cancel/${jobId}`)
      activeJobs.value.delete(jobId)
      if (currentJob.value === jobId) {
        currentJob.value = null
      }
    } catch (error) {
      throw error
    }
  }

  async function testConnection(connectionString, databaseType) {
    try {
      const response = await api.post('/analysis/test-connection', {
        connectionString,
        databaseType
      })
      return response.data
    } catch (error) {
      throw error
    }
  }

  async function getAnalysisHistory(page = 1, pageSize = 10) {
    try {
      const response = await api.get('/analysis/history', {
        params: { page, pageSize }
      })
      return response.data
    } catch (error) {
      throw error
    }
  }

  async function exportResults(jobId, format = 'pdf') {
    try {
      const response = await api.get(`/analysis/export/${jobId}`, {
        params: { format },
        responseType: 'blob'
      })
      
      // Create download link
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `analysis-${jobId}.${format}`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
    } catch (error) {
      throw error
    }
  }

  return {
    // State
    isConnected,
    activeJobs,
    completedAnalyses,
    currentJob,
    
    // Getters
    hasActiveJobs,
    currentJobStatus,
    
    // Actions
    initializeSignalR,
    startAnalysis,
    getAnalysisResults,
    cancelAnalysis,
    testConnection,
    getAnalysisHistory,
    exportResults
  }
})