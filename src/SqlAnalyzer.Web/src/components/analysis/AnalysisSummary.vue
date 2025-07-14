<template>
  <div class="analysis-summary">
    <h3>Analysis Summary</h3>
    <div v-if="!findings || findings.length === 0" class="no-findings">
      <p>No issues found. Your database appears to be in good health!</p>
    </div>
    <div v-else>
      <div class="summary-stats">
        <div class="stat-item">
          <span class="stat-value">{{ findings.length }}</span>
          <span class="stat-label">Total Findings</span>
        </div>
        <div class="stat-item high">
          <span class="stat-value">{{ countBySeverity('High') }}</span>
          <span class="stat-label">High Severity</span>
        </div>
        <div class="stat-item medium">
          <span class="stat-value">{{ countBySeverity('Medium') }}</span>
          <span class="stat-label">Medium Severity</span>
        </div>
        <div class="stat-item low">
          <span class="stat-value">{{ countBySeverity('Low') }}</span>
          <span class="stat-label">Low Severity</span>
        </div>
      </div>

      <div class="findings-list">
        <h4>Key Findings</h4>
        <div v-for="finding in sortedFindings" :key="finding.id" class="finding-item" :class="`severity-${finding.severity.toLowerCase()}`">
          <div class="finding-header">
            <span class="finding-category">{{ finding.category }}</span>
            <span class="finding-severity">{{ finding.severity }}</span>
          </div>
          <h5>{{ finding.title }}</h5>
          <p>{{ finding.description }}</p>
          <p v-if="finding.impact" class="impact"><strong>Impact:</strong> {{ finding.impact }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  findings: Array
})

const countBySeverity = (severity) => {
  return props.findings?.filter(f => f.severity === severity).length || 0
}

const sortedFindings = computed(() => {
  if (!props.findings) return []
  
  const severityOrder = { 'High': 0, 'Medium': 1, 'Low': 2 }
  return [...props.findings].sort((a, b) => {
    return (severityOrder[a.severity] || 999) - (severityOrder[b.severity] || 999)
  })
})
</script>

<style scoped lang="scss">
.analysis-summary {
  .no-findings {
    text-align: center;
    padding: 3rem;
    background: #d4edda;
    border-radius: 4px;
    color: #155724;
  }

  .summary-stats {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: 1rem;
    margin-bottom: 2rem;

    .stat-item {
      text-align: center;
      padding: 1.5rem;
      background: #f8f9fa;
      border-radius: 4px;
      border: 2px solid transparent;

      &.high {
        border-color: #dc3545;
        .stat-value {
          color: #dc3545;
        }
      }

      &.medium {
        border-color: #ffc107;
        .stat-value {
          color: #ffc107;
        }
      }

      &.low {
        border-color: #28a745;
        .stat-value {
          color: #28a745;
        }
      }

      .stat-value {
        display: block;
        font-size: 2.5rem;
        font-weight: bold;
        margin-bottom: 0.5rem;
      }

      .stat-label {
        display: block;
        font-size: 0.875rem;
        color: #6c757d;
      }
    }
  }

  .findings-list {
    h4 {
      margin-bottom: 1rem;
    }

    .finding-item {
      padding: 1rem;
      margin-bottom: 1rem;
      border-radius: 4px;
      border-left: 4px solid #6c757d;

      &.severity-high {
        background: #f8d7da;
        border-left-color: #dc3545;
      }

      &.severity-medium {
        background: #fff3cd;
        border-left-color: #ffc107;
      }

      &.severity-low {
        background: #d4edda;
        border-left-color: #28a745;
      }

      .finding-header {
        display: flex;
        justify-content: space-between;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;

        .finding-category {
          color: #6c757d;
          font-weight: 500;
        }

        .finding-severity {
          font-weight: bold;
        }
      }

      h5 {
        margin: 0.5rem 0;
        color: #2c3e50;
      }

      p {
        margin: 0.5rem 0;
        color: #495057;

        &.impact {
          font-size: 0.9rem;
          font-style: italic;
        }
      }
    }
  }
}
</style>