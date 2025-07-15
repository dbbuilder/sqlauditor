<template>
  <div class="new-analysis">
    <Card>
      <template #header>
        <h2>New Database Analysis</h2>
      </template>
      
      <template #content>
        <div class="form-section">
          <h3>Database Connection</h3>
          
          <div class="field">
            <label for="dbType">Database Type</label>
            <Dropdown 
              v-model="form.databaseType" 
              :options="databaseTypes" 
              optionLabel="label"
              optionValue="value"
              placeholder="Select database type"
            />
          </div>

          <div class="field">
            <label>Connection Method</label>
            <SelectButton 
              v-model="connectionMode" 
              :options="connectionModes" 
              optionLabel="label"
              optionValue="value"
            />
          </div>

          <!-- Connection Builder Mode -->
          <div v-if="connectionMode === 'builder'" class="connection-builder">
            <div class="field">
              <label for="server">Server</label>
              <InputText 
                v-model="connectionBuilder.server" 
                id="server"
                placeholder="localhost or server.domain.com"
                class="w-full"
              />
            </div>

            <div class="field" v-if="form.databaseType === 0">
              <label for="port">Port (optional)</label>
              <InputText 
                v-model="connectionBuilder.port" 
                id="port"
                placeholder="1433"
                class="w-full"
              />
            </div>

            <div class="field">
              <label for="database">Database</label>
              <InputText 
                v-model="connectionBuilder.database" 
                id="database"
                placeholder="Database name"
                class="w-full"
              />
            </div>

            <div class="field">
              <label>Authentication</label>
              <SelectButton 
                v-model="connectionBuilder.authType" 
                :options="authTypes" 
                optionLabel="label"
                optionValue="value"
              />
            </div>

            <div v-if="connectionBuilder.authType === 'sql'" class="auth-fields">
              <div class="field">
                <label for="username">Username</label>
                <InputText 
                  v-model="connectionBuilder.username" 
                  id="username"
                  placeholder="sa"
                  class="w-full"
                />
              </div>

              <div class="field">
                <label for="password">Password</label>
                <Password 
                  v-model="connectionBuilder.password" 
                  id="password"
                  placeholder="Enter password"
                  :feedback="false"
                  toggleMask
                  class="w-full"
                />
              </div>
            </div>

            <div class="field">
              <label>Additional Options</label>
              <div class="field-checkbox">
                <Checkbox v-model="connectionBuilder.trustServerCertificate" :binary="true" inputId="trustCert" />
                <label for="trustCert">Trust Server Certificate</label>
              </div>
              <div class="field-checkbox">
                <Checkbox v-model="connectionBuilder.encrypt" :binary="true" inputId="encrypt" />
                <label for="encrypt">Encrypt Connection</label>
              </div>
            </div>

            <div class="built-connection-string">
              <label>Generated Connection String:</label>
              <InputText 
                :value="builtConnectionString" 
                readonly
                class="w-full monospace"
              />
            </div>
          </div>

          <!-- Manual Mode -->
          <div v-else class="field">
            <label for="connectionString">Connection String</label>
            <Textarea 
              v-model="form.connectionString" 
              id="connectionString"
              rows="3"
              placeholder="Server=localhost;Database=mydb;User Id=sa;Password=..."
              class="w-full monospace"
            />
          </div>

          <Button 
            label="Test Connection" 
            icon="pi pi-check"
            @click="testConnection"
            :loading="testing"
            severity="secondary"
          />
        </div>

        <Divider />

        <div class="form-section">
          <h3>Analysis Options</h3>
          
          <div class="field">
            <label for="analysisType">Analysis Type</label>
            <Dropdown 
              v-model="form.analysisType" 
              :options="analysisTypes" 
              optionLabel="name"
              optionValue="id"
              placeholder="Select analysis type"
            />
          </div>

          <div class="options-grid">
            <div class="field-checkbox">
              <Checkbox v-model="form.options.includeIndexAnalysis" :binary="true" inputId="indexAnalysis" />
              <label for="indexAnalysis">Index Analysis</label>
            </div>
            
            <div class="field-checkbox">
              <Checkbox v-model="form.options.includeFragmentation" :binary="true" inputId="fragmentation" />
              <label for="fragmentation">Fragmentation Check</label>
            </div>
            
            <div class="field-checkbox">
              <Checkbox v-model="form.options.includeStatistics" :binary="true" inputId="statistics" />
              <label for="statistics">Statistics Analysis</label>
            </div>
            
            <div class="field-checkbox">
              <Checkbox v-model="form.options.includeSecurityAudit" :binary="true" inputId="security" />
              <label for="security">Security Audit</label>
            </div>
            
            <div class="field-checkbox">
              <Checkbox v-model="form.options.includeQueryPerformance" :binary="true" inputId="performance" />
              <label for="performance">Query Performance</label>
            </div>
            
            <div class="field-checkbox">
              <Checkbox v-model="form.options.includeDependencies" :binary="true" inputId="dependencies" />
              <label for="dependencies">Object Dependencies</label>
            </div>
          </div>
        </div>
      </template>

      <template #footer>
        <div class="flex justify-content-between">
          <Button 
            label="Cancel" 
            icon="pi pi-times"
            @click="$router.push('/')"
            severity="secondary"
            outlined
          />
          
          <Button 
            label="Start Analysis" 
            icon="pi pi-play"
            @click="startAnalysis"
            :loading="starting"
            :disabled="!form.connectionString || form.databaseType === null || form.databaseType === undefined || !form.analysisType"
          />
        </div>
      </template>
    </Card>

    <Dialog v-model:visible="showTestResult" modal header="Connection Test Result" :style="{ width: '30rem' }">
      <div v-if="testResult">
        <div v-if="testResult.success" class="test-success">
          <i class="pi pi-check-circle"></i>
          <p>Connection successful!</p>
          <p>Database: {{ testResult.databaseName }}</p>
          <p>Server: {{ testResult.serverVersion }}</p>
        </div>
        <div v-else class="test-error">
          <i class="pi pi-times-circle"></i>
          <p>Connection failed!</p>
          <p>{{ testResult.error }}</p>
        </div>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useAnalysisStore } from '@/stores/analysis'
import Card from 'primevue/card'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Dropdown from 'primevue/dropdown'
import Checkbox from 'primevue/checkbox'
import Divider from 'primevue/divider'
import Dialog from 'primevue/dialog'
import SelectButton from 'primevue/selectbutton'
import Password from 'primevue/password'
import Textarea from 'primevue/textarea'

const router = useRouter()
const toast = useToast()
const analysisStore = useAnalysisStore()

const form = reactive({
  connectionString: '',
  databaseType: 0, // SqlServer = 0
  analysisType: 'comprehensive',
  options: {
    includeIndexAnalysis: true,
    includeFragmentation: true,
    includeStatistics: true,
    includeSecurityAudit: true,
    includeQueryPerformance: true,
    includeDependencies: true,
    timeoutMinutes: 30
  }
})

const databaseTypes = [
  { label: 'SQL Server', value: 0 }, // SqlServer = 0
  { label: 'PostgreSQL', value: 1 }, // PostgreSql = 1
  { label: 'MySQL', value: 2 }      // MySql = 2
]

const analysisTypes = ref([])
const testing = ref(false)
const starting = ref(false)
const showTestResult = ref(false)
const testResult = ref(null)

// Connection builder
const connectionMode = ref('builder')
const connectionModes = [
  { label: 'Builder', value: 'builder' },
  { label: 'Manual', value: 'manual' }
]

const authTypes = [
  { label: 'SQL Authentication', value: 'sql' },
  { label: 'Windows Authentication', value: 'windows' }
]

const connectionBuilder = reactive({
  server: '',
  port: '',
  database: '',
  authType: 'sql',
  username: '',
  password: '',
  trustServerCertificate: true,
  encrypt: false
})

// Build connection string from components
const builtConnectionString = computed(() => {
  if (!connectionBuilder.server || !connectionBuilder.database) {
    return ''
  }

  let parts = []
  
  // Server and port
  if (connectionBuilder.port) {
    parts.push(`Server=${connectionBuilder.server},${connectionBuilder.port}`)
  } else {
    parts.push(`Server=${connectionBuilder.server}`)
  }
  
  // Database
  parts.push(`Database=${connectionBuilder.database}`)
  
  // Authentication
  if (connectionBuilder.authType === 'sql') {
    if (connectionBuilder.username) {
      parts.push(`User Id=${connectionBuilder.username}`)
    }
    if (connectionBuilder.password) {
      parts.push(`Password=${connectionBuilder.password}`)
    }
  } else {
    parts.push('Integrated Security=true')
  }
  
  // Additional options
  if (connectionBuilder.trustServerCertificate) {
    parts.push('TrustServerCertificate=true')
  }
  if (connectionBuilder.encrypt) {
    parts.push('Encrypt=true')
  }
  
  return parts.join(';')
})

// Watch for connection string changes
watch(builtConnectionString, (newValue) => {
  if (connectionMode.value === 'builder') {
    form.connectionString = newValue
  }
})

// Load analysis types
async function loadAnalysisTypes() {
  try {
    // Set default analysis types instead of fetching from API
    analysisTypes.value = [
      { id: 'comprehensive', name: 'Comprehensive Analysis', description: 'Full database analysis' },
      { id: 'performance', name: 'Performance Analysis', description: 'Focus on performance issues' },
      { id: 'security', name: 'Security Audit', description: 'Security-focused analysis' },
      { id: 'quick', name: 'Quick Scan', description: 'Fast basic analysis' }
    ]
  } catch (error) {
    console.error('Failed to load analysis types:', error)
  }
}

async function testConnection() {
  testing.value = true
  try {
    testResult.value = await analysisStore.testConnection(
      form.connectionString,
      form.databaseType
    )
    showTestResult.value = true
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Test Failed',
      detail: error.message,
      life: 5000
    })
  } finally {
    testing.value = false
  }
}

async function startAnalysis() {
  starting.value = true
  try {
    const jobId = await analysisStore.startAnalysis(form)
    
    toast.add({
      severity: 'success',
      summary: 'Analysis Started',
      detail: 'Your analysis has been queued',
      life: 3000
    })
    
    // Navigate to analysis progress view
    router.push(`/analysis/${jobId}`)
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Failed to Start',
      detail: error.message,
      life: 5000
    })
  } finally {
    starting.value = false
  }
}

// Load analysis types on mount
loadAnalysisTypes()

// Ensure form is properly initialized
console.log('Form initialized with:', form)
</script>

<style lang="scss" scoped>
.new-analysis {
  max-width: 800px;
  margin: 0 auto;
}

.form-section {
  margin-bottom: 2rem;

  h3 {
    margin-bottom: 1rem;
    color: #495057;
  }
}

.field {
  margin-bottom: 1.5rem;

  label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 500;
  }

  .w-full {
    width: 100%;
  }
}

.field-checkbox {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.options-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.test-success,
.test-error {
  text-align: center;
  padding: 1rem;

  i {
    font-size: 3rem;
    margin-bottom: 1rem;
  }

  p {
    margin: 0.5rem 0;
  }
}

.test-success {
  i { color: #22c55e; }
}

.test-error {
  i { color: #ef4444; }
}

.connection-builder {
  margin-top: 1rem;
  
  .auth-fields {
    margin-top: 1rem;
  }
}

.built-connection-string {
  margin-top: 1.5rem;
  padding: 1rem;
  background-color: #f8f9fa;
  border-radius: 4px;
  
  label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 500;
    color: #6c757d;
  }
}

.monospace {
  font-family: 'Courier New', monospace;
}
</style>