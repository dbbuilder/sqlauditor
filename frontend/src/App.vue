<template>
  <div id="app">
    <Navbar v-if="showNavbar" />
    <main class="main-content" :class="{ 'no-navbar': !showNavbar }">
      <router-view v-slot="{ Component }">
        <transition name="fade" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </main>
    <Toast />
  </div>
</template>

<script setup>
import { onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAnalysisStore } from '@/stores/analysis'
import { useAuthStore } from '@/stores/auth'
import Navbar from '@/components/Navbar.vue'
import Toast from 'primevue/toast'

const analysisStore = useAnalysisStore()
const authStore = useAuthStore()
const route = useRoute()

// Check if we should show the navbar (not on login page)
const showNavbar = computed(() => route.name !== 'login')

onMounted(() => {
  // Initialize authentication
  authStore.initializeAuth()
  
  // Initialize SignalR connection only if authenticated and has token
  if (authStore.token) {
    analysisStore.initializeSignalR()
  }
})
</script>

<style lang="scss">
#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.main-content {
  flex: 1;
  padding: 2rem;
  background-color: #f8f9fa;
  
  &.no-navbar {
    padding: 0;
  }
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>