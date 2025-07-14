<template>
  <div class="performance-metrics">
    <h3>Performance Metrics</h3>
    <div v-if="!metrics" class="no-data">
      No performance metrics available
    </div>
    <div v-else class="metrics-grid">
      <div class="metric-card">
        <div class="metric-icon">📊</div>
        <div class="metric-content">
          <h4>Query Performance</h4>
          <p class="metric-value">{{ metrics.avgQueryTime || 'N/A' }} ms</p>
          <p class="metric-label">Average Query Time</p>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon">💾</div>
        <div class="metric-content">
          <h4>Buffer Cache</h4>
          <p class="metric-value">{{ metrics.bufferCacheHitRatio || 'N/A' }}%</p>
          <p class="metric-label">Hit Ratio</p>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon">⚡</div>
        <div class="metric-content">
          <h4>Page Life Expectancy</h4>
          <p class="metric-value">{{ metrics.pageLifeExpectancy || 'N/A' }}s</p>
          <p class="metric-label">Seconds</p>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon">🔒</div>
        <div class="metric-content">
          <h4>Lock Waits</h4>
          <p class="metric-value">{{ metrics.lockWaits || 0 }}</p>
          <p class="metric-label">Current Waits</p>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon">📈</div>
        <div class="metric-content">
          <h4>CPU Usage</h4>
          <p class="metric-value">{{ metrics.cpuUsage || 'N/A' }}%</p>
          <p class="metric-label">Current Usage</p>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon">🗄️</div>
        <div class="metric-content">
          <h4>Memory Usage</h4>
          <p class="metric-value">{{ metrics.memoryUsage || 'N/A' }} GB</p>
          <p class="metric-label">In Use</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  metrics: Object
})
</script>

<style scoped lang="scss">
.performance-metrics {
  .metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1.5rem;
  }

  .metric-card {
    background: #f8f9fa;
    border-radius: 8px;
    padding: 1.5rem;
    display: flex;
    align-items: center;
    gap: 1rem;
    transition: transform 0.2s, box-shadow 0.2s;

    &:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .metric-icon {
      font-size: 2.5rem;
      opacity: 0.8;
    }

    .metric-content {
      flex: 1;

      h4 {
        margin: 0 0 0.5rem 0;
        font-size: 0.875rem;
        color: #6c757d;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }

      .metric-value {
        margin: 0;
        font-size: 1.75rem;
        font-weight: bold;
        color: #2c3e50;
      }

      .metric-label {
        margin: 0;
        font-size: 0.875rem;
        color: #6c757d;
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