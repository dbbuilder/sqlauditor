<template>
  <div class="recommendations-list">
    <h3>Recommendations</h3>
    <div v-if="!recommendations || recommendations.length === 0" class="no-data">
      No recommendations available
    </div>
    <div v-else>
      <div v-for="rec in sortedRecommendations" :key="rec.title" class="recommendation-item" :class="`priority-${rec.priority.toLowerCase()}`">
        <div class="recommendation-header">
          <h4>{{ rec.title }}</h4>
          <span class="priority-badge">{{ rec.priority }}</span>
        </div>
        <p class="description">{{ rec.description }}</p>
        <div v-if="rec.estimatedImpact" class="impact">
          <strong>Estimated Impact:</strong> {{ rec.estimatedImpact }}
        </div>
        <div v-if="rec.actions && rec.actions.length > 0" class="actions">
          <h5>Recommended Actions:</h5>
          <pre v-for="(action, i) in rec.actions" :key="i"><code>{{ action }}</code></pre>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  recommendations: Array
})

const sortedRecommendations = computed(() => {
  if (!props.recommendations) return []
  
  const priorityOrder = { 'High': 0, 'Medium': 1, 'Low': 2 }
  return [...props.recommendations].sort((a, b) => {
    return (priorityOrder[a.priority] || 999) - (priorityOrder[b.priority] || 999)
  })
})
</script>

<style scoped lang="scss">
.recommendations-list {
  .recommendation-item {
    padding: 1.5rem;
    margin-bottom: 1rem;
    border-radius: 4px;
    background: #f8f9fa;
    border-left: 4px solid #6c757d;

    &.priority-high {
      border-left-color: #dc3545;
      background: #f8d7da;
    }

    &.priority-medium {
      border-left-color: #ffc107;
      background: #fff3cd;
    }

    &.priority-low {
      border-left-color: #28a745;
      background: #d4edda;
    }

    .recommendation-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;

      h4 {
        margin: 0;
        color: #2c3e50;
      }

      .priority-badge {
        padding: 0.25rem 0.75rem;
        border-radius: 20px;
        font-size: 0.875rem;
        font-weight: 500;
        background: rgba(0, 0, 0, 0.1);
      }
    }

    .description {
      margin-bottom: 1rem;
      color: #495057;
    }

    .impact {
      margin-bottom: 1rem;
      padding: 0.5rem;
      background: rgba(255, 255, 255, 0.5);
      border-radius: 4px;
      font-size: 0.9rem;
    }

    .actions {
      h5 {
        margin: 0.5rem 0;
        font-size: 0.9rem;
        color: #495057;
      }

      pre {
        margin: 0.5rem 0;
        padding: 0.75rem;
        background: #2c3e50;
        color: #fff;
        border-radius: 4px;
        overflow-x: auto;
        font-size: 0.85rem;

        code {
          font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
        }
      }
    }
  }

  .no-data {
    text-align: center;
    color: #6c757d;
    padding: 2rem;
  }
}
</style>