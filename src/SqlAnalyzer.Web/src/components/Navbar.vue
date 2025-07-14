<template>
  <nav class="navbar">
    <div class="container">
      <div class="navbar-brand">
        <router-link to="/" class="navbar-logo">
          SQL Analyzer
        </router-link>
      </div>
      <div class="navbar-menu">
        <div class="navbar-end">
          <router-link to="/" class="navbar-item">
            New Analysis
          </router-link>
          <a href="#" class="navbar-item" @click.prevent="toggleMockMode">
            {{ mockModeEnabled ? 'Mock Mode' : 'Live Mode' }}
          </a>
          <div class="navbar-divider"></div>
          <div class="navbar-user">
            <span class="navbar-username">{{ authStore.username }}</span>
            <Button 
              label="Logout" 
              @click="handleLogout"
              severity="secondary"
              size="small"
              outlined
            />
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'

const authStore = useAuthStore()
const toast = useToast()

const mockModeEnabled = computed(() => {
  return import.meta.env.VITE_ENABLE_MOCK_DATA === 'true'
})

const toggleMockMode = () => {
  // In production, this won't actually toggle
  console.log('Mock mode is:', mockModeEnabled.value)
}

const handleLogout = async () => {
  await authStore.logout()
  toast.add({
    severity: 'info',
    summary: 'Logged Out',
    detail: 'You have been logged out successfully',
    life: 3000
  })
}
</script>

<style scoped lang="scss">
.navbar {
  background-color: #2c3e50;
  color: white;
  padding: 1rem 0;
  margin-bottom: 2rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

  .container {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  &-brand {
    font-size: 1.5rem;
    font-weight: bold;
  }

  &-logo {
    color: white;
    text-decoration: none;
    transition: opacity 0.3s;

    &:hover {
      opacity: 0.8;
    }
  }

  &-menu {
    display: flex;
    align-items: center;
  }

  &-end {
    display: flex;
    gap: 1rem;
    align-items: center;
  }
  
  &-divider {
    width: 1px;
    height: 24px;
    background-color: rgba(255, 255, 255, 0.3);
    margin: 0 0.5rem;
  }
  
  &-user {
    display: flex;
    align-items: center;
    gap: 1rem;
  }
  
  &-username {
    color: rgba(255, 255, 255, 0.9);
    font-weight: 500;
  }

  &-item {
    color: white;
    text-decoration: none;
    padding: 0.5rem 1rem;
    border-radius: 4px;
    transition: background-color 0.3s;

    &:hover {
      background-color: rgba(255, 255, 255, 0.1);
    }

    &.router-link-active {
      background-color: rgba(255, 255, 255, 0.2);
    }
  }
}
</style>