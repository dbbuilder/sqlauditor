<template>
  <div class="performance-analysis">
    <h3>Performance Analysis</h3>
    <div v-if="!data" class="no-data">
      No performance data available
    </div>
    <div v-else>
      <div v-if="data.missingIndexes && data.missingIndexes.length > 0" class="section">
        <h4>Missing Indexes</h4>
        <div v-for="(index, i) in data.missingIndexes" :key="i" class="item">
          <strong>{{ index.tableName }}</strong>
          <p>Impact Score: {{ index.impactScore?.toFixed(2) }}</p>
          <p>Equality Columns: {{ index.equalityColumns?.join(', ') || 'None' }}</p>
          <p>Inequality Columns: {{ index.inequalityColumns?.join(', ') || 'None' }}</p>
          <p>Included Columns: {{ index.includedColumns?.join(', ') || 'None' }}</p>
        </div>
      </div>

      <div v-if="data.fragmentedIndexes && data.fragmentedIndexes.length > 0" class="section">
        <h4>Fragmented Indexes</h4>
        <div v-for="(index, i) in data.fragmentedIndexes" :key="i" class="item">
          <strong>{{ index.tableName }} - {{ index.indexName }}</strong>
          <p>Fragmentation: {{ index.fragmentationPercent?.toFixed(1) }}%</p>
          <p>Page Count: {{ index.pageCount }}</p>
        </div>
      </div>

      <div v-if="data.outdatedStatistics && data.outdatedStatistics.length > 0" class="section">
        <h4>Outdated Statistics</h4>
        <div v-for="(stat, i) in data.outdatedStatistics" :key="i" class="item">
          <strong>{{ stat.tableName }} - {{ stat.statisticName }}</strong>
          <p>Last Updated: {{ new Date(stat.lastUpdated).toLocaleDateString() }}</p>
          <p>Days Since Update: {{ stat.daysSinceUpdate }}</p>
          <p>Row Count: {{ stat.rowCount?.toLocaleString() }}</p>
          <p>Modifications: {{ stat.modificationCount?.toLocaleString() }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  data: Object
})
</script>

<style scoped lang="scss">
.performance-analysis {
  .section {
    margin-bottom: 2rem;

    h4 {
      margin-bottom: 1rem;
      color: #2c3e50;
    }
  }

  .item {
    padding: 1rem;
    background: #f8f9fa;
    border-radius: 4px;
    margin-bottom: 1rem;
    border-left: 4px solid #007bff;

    strong {
      display: block;
      margin-bottom: 0.5rem;
      color: #2c3e50;
    }

    p {
      margin: 0.25rem 0;
      color: #6c757d;
      font-size: 0.9rem;
    }
  }

  .no-data {
    text-align: center;
    color: #6c757d;
    padding: 2rem;
  }
}
</style>