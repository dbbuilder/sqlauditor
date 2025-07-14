<template>
  <div class="security-audit">
    <h3>Security Audit</h3>
    <div v-if="!data || !data.permissions" class="no-data">
      No security data available
    </div>
    <div v-else>
      <div class="security-summary">
        <div class="summary-item" :class="{ 'warning': data.hasElevatedPermissions }">
          <span class="label">Elevated Permissions:</span>
          <span class="value">{{ data.hasElevatedPermissions ? 'Yes' : 'No' }}</span>
        </div>
        <div class="summary-item" :class="{ 'warning': data.hasWeakPasswords }">
          <span class="label">Weak Passwords:</span>
          <span class="value">{{ data.hasWeakPasswords ? 'Yes' : 'No' }}</span>
        </div>
      </div>

      <div v-if="data.permissions && data.permissions.length > 0" class="permissions-list">
        <h4>Permissions</h4>
        <table>
          <thead>
            <tr>
              <th>Principal</th>
              <th>Type</th>
              <th>Permission</th>
              <th>State</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="perm in data.permissions" :key="`${perm.principalName}-${perm.permissionName}`">
              <td>{{ perm.principalName }}</td>
              <td>{{ perm.principalType }}</td>
              <td>{{ perm.permissionName }}</td>
              <td>{{ perm.state }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="data.vulnerabilities && data.vulnerabilities.length > 0" class="vulnerabilities">
        <h4>Vulnerabilities</h4>
        <div v-for="vuln in data.vulnerabilities" :key="vuln.id" class="vulnerability-item">
          <h5>{{ vuln.title }}</h5>
          <p>{{ vuln.description }}</p>
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
.security-audit {
  .security-summary {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1rem;
    margin-bottom: 2rem;

    .summary-item {
      padding: 1rem;
      background: #f8f9fa;
      border-radius: 4px;
      display: flex;
      justify-content: space-between;

      &.warning {
        background: #fff3cd;
        border: 1px solid #ffeeba;
      }

      .label {
        font-weight: 500;
      }
    }
  }

  .permissions-list {
    margin-top: 2rem;

    table {
      width: 100%;
      border-collapse: collapse;

      th, td {
        padding: 0.75rem;
        text-align: left;
        border-bottom: 1px solid #dee2e6;
      }

      th {
        background: #f8f9fa;
        font-weight: 600;
      }
    }
  }

  .vulnerabilities {
    margin-top: 2rem;

    .vulnerability-item {
      padding: 1rem;
      background: #f8d7da;
      border: 1px solid #f5c6cb;
      border-radius: 4px;
      margin-bottom: 1rem;

      h5 {
        margin: 0 0 0.5rem 0;
        color: #721c24;
      }

      p {
        margin: 0;
        color: #721c24;
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