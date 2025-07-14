<template>
  <div class="analysis-progress">
    <Card v-if="jobStatus">
      <template #header>
        <div class="header-content">
          <h2>Analysis Progress</h2>
          <Tag :value="jobStatus.status" :severity="getStatusSeverity(jobStatus.status)" />
        </div>
      </template>
      
      <template #content>
        <div class="progress-section">
          <h3>{{ jobStatus.currentStep }}</h3>
          <ProgressBar :value="jobStatus.progressPercentage" :showValue="true" />
          
          <div class="progress-details">
            <p><strong>Job ID:</strong> {{ jobId }}</p>
            <p><strong>Started:</strong> {{ formatDate(jobStatus.startedAt) }}</p>
            <p v-if="jobStatus.completedAt">
              <strong>Completed:</strong> {{ formatDate(jobStatus.completedAt) }}
            </p>
            <p v-if="jobStatus.duration">
              <strong>Duration:</strong> {{ formatDuration(jobStatus.duration) }}
            </p>
          </div>
        </div>

        <div v-if="jobStatus.errorMessage" class="error-section">
          <Message severity="error" :closable="false">
            {{ jobStatus.errorMessage }}
          </Message>
        </div>

        <div v-if="jobStatus.status === 'Completed' && analysisResult" class="results-section">
          <Divider />
          <h3>Analysis Results</h3>
          
          <TabView>
            <TabPanel header="Summary">
              <AnalysisSummary :result="analysisResult" />
            </TabPanel>
            
            <TabPanel header="Findings" :badge="analysisResult.findings.length">
              <FindingsList :findings="analysisResult.findings" />
            </TabPanel>
            
            <TabPanel header="Performance">
              <PerformanceMetrics :metrics="analysisResult.performance" />
            </TabPanel>
            
            <TabPanel header="Security">
              <SecurityAudit :audit="analysisResult.security" />
            </TabPanel>
            
            <TabPanel header="Recommendations">
              <RecommendationsList :recommendations="analysisResult.recommendations" />
            </TabPanel>
          </TabView>
        </div>
      </template>

      <template #footer>
        <div class="flex justify-content-between">
          <Button 
            label="Back to Dashboard" 
            icon="pi pi-arrow-left"
            @click="$router.push('/')"
            severity="secondary"
            outlined
          />
          
          <div class="button-group">
            <Button 
              v-if="isRunning"
              label="Cancel Analysis" 
              icon="pi pi-times"
              @click="cancelAnalysis"
              severity="danger"
              outlined
            />
            
            <Button 
              v-if="jobStatus.status === 'Completed'"
              label="Export PDF" 
              icon="pi pi-file-pdf"
              @click="exportResults('pdf')"
              severity="info"
            />
            
            <Button 
              v-if="jobStatus.status === 'Completed'"
              label="Export JSON" 
              icon="pi pi-download"
              @click="exportResults('json')"
              severity="secondary"
            />
          </div>
        </div>
      </template>
    </Card>

    <div v-else class="loading">
      <ProgressSpinner />
      <p>Loading analysis status...</p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useAnalysisStore } from '@/stores/analysis'
import Card from 'primevue/card'
import Button from 'primevue/button'
import ProgressBar from 'primevue/progressbar'
import ProgressSpinner from 'primevue/progressspinner'
import Tag from 'primevue/tag'
import Divider from 'primevue/divider'
import Message from 'primevue/message'
import TabView from 'primevue/tabview'
import TabPanel from 'primevue/tabpanel'

// Import sub-components
import AnalysisSummary from '@/components/analysis/AnalysisSummary.vue'
import FindingsList from '@/components/analysis/FindingsList.vue'
import PerformanceMetrics from '@/components/analysis/PerformanceMetrics.vue'
import SecurityAudit from '@/components/analysis/SecurityAudit.vue'
import RecommendationsList from '@/components/analysis/RecommendationsList.vue'

const route = useRoute()
const toast = useToast()
const analysisStore = useAnalysisStore()

const jobId = computed(() => route.params.jobId)
const jobStatus = computed(() => analysisStore.activeJobs.get(jobId.value))
const analysisResult = ref(null)

const isRunning = computed(() => 
  jobStatus.value && ['Queued', 'Running'].includes(jobStatus.value.status)
)

function getStatusSeverity(status) {
  switch (status) {
    case 'Completed': return 'success'
    case 'Failed': return 'danger'
    case 'Cancelled': return 'warning'
    default: return 'info'
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleString()
}

function formatDuration(duration) {
  const seconds = Math.floor(duration / 1000)
  const minutes = Math.floor(seconds / 60)
  const hours = Math.floor(minutes / 60)
  
  if (hours > 0) {
    return `${hours}h ${minutes % 60}m ${seconds % 60}s`
  } else if (minutes > 0) {
    return `${minutes}m ${seconds % 60}s`
  } else {
    return `${seconds}s`
  }
}

async function loadResults() {
  try {
    analysisResult.value = await analysisStore.getAnalysisResults(jobId.value)
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Failed to Load Results',
      detail: error.message,
      life: 5000
    })
  }
}

async function cancelAnalysis() {
  try {
    await analysisStore.cancelAnalysis(jobId.value)
    toast.add({
      severity: 'info',
      summary: 'Analysis Cancelled',
      detail: 'The analysis has been cancelled',
      life: 3000
    })
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Failed to Cancel',
      detail: error.message,
      life: 5000
    })
  }
}

async function exportResults(format) {
  try {
    await analysisStore.exportResults(jobId.value, format)
    toast.add({
      severity: 'success',
      summary: 'Export Complete',
      detail: `Results exported as ${format.toUpperCase()}`,
      life: 3000
    })
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Export Failed',
      detail: error.message,
      life: 5000
    })
  }
}

// Watch for completion
watch(() => jobStatus.value?.status, (newStatus) => {
  if (newStatus === 'Completed') {
    loadResults()
  }
})

onMounted(() => {
  // If already completed, load results
  if (jobStatus.value?.status === 'Completed') {
    loadResults()
  }
})
</script>

<style lang="scss" scoped>
.analysis-progress {
  max-width: 1200px;
  margin: 0 auto;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.progress-section {
  margin-bottom: 2rem;

  h3 {
    margin-bottom: 1rem;
    color: #495057;
  }
}

.progress-details {
  margin-top: 1rem;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.5rem;

  p {
    margin: 0;
  }
}

.error-section {
  margin: 2rem 0;
}

.results-section {
  margin-top: 2rem;
}

.button-group {
  display: flex;
  gap: 0.5rem;
}

.loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  gap: 1rem;
}
</style>