<template>
  <div class="database-overview">
    <h3>Database Overview</h3>
    <div v-if="!data" class="no-data">
      No database information available
    </div>
    <div v-else class="overview-grid">
      <div class="overview-item">
        <span class="label">Database Name:</span>
        <span class="value">{{ data.name }}</span>
      </div>
      <div class="overview-item">
        <span class="label">Server Version:</span>
        <span class="value">{{ data.serverVersion }}</span>
      </div>
      <div class="overview-item">
        <span class="label">Edition:</span>
        <span class="value">{{ data.edition }}</span>
      </div>
      <div class="overview-item">
        <span class="label">Size:</span>
        <span class="value">{{ (data.sizeMB / 1024).toFixed(2) }} GB</span>
      </div>
      <div class="overview-item">
        <span class="label">Tables:</span>
        <span class="value">{{ data.tableCount }}</span>
      </div>
      <div class="overview-item">
        <span class="label">Indexes:</span>
        <span class="value">{{ data.indexCount }}</span>
      </div>
      <div class="overview-item">
        <span class="label">Stored Procedures:</span>
        <span class="value">{{ data.procedureCount }}</span>
      </div>
      <div class="overview-item">
        <span class="label">Views:</span>
        <span class="value">{{ data.viewCount }}</span>
      </div>
      <div class="overview-item">
        <span class="label">Total Rows:</span>
        <span class="value">{{ data.totalRows?.toLocaleString() }}</span>
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
.database-overview {
  .overview-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1rem;
  }

  .overview-item {
    padding: 1rem;
    background: #f8f9fa;
    border-radius: 4px;
    display: flex;
    justify-content: space-between;
    align-items: center;

    .label {
      font-weight: 500;
      color: #6c757d;
    }

    .value {
      font-weight: 600;
      color: #2c3e50;
    }
  }

  .no-data {
    text-align: center;
    color: #6c757d;
    padding: 2rem;
  }
}
</style>