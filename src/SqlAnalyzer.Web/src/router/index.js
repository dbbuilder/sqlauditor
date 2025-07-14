import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import NewAnalysis from '../views/NewAnalysis.vue'
import AnalysisProgress from '../views/AnalysisProgress.vue'
import LoginView from '../views/LoginView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { requiresAuth: false }
    },
    {
      path: '/',
      name: 'home',
      component: NewAnalysis,
      meta: { requiresAuth: true }
    },
    {
      path: '/analysis/:jobId',
      name: 'analysis-progress',
      component: AnalysisProgress,
      props: true,
      meta: { requiresAuth: true }
    }
  ]
})

// Navigation guard to check authentication
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore()
  
  // Skip auth check for login page
  if (to.meta.requiresAuth === false) {
    next()
    return
  }
  
  // Check if user has token (don't verify with server to avoid 400 errors)
  const hasToken = !!authStore.token
  
  if (to.meta.requiresAuth && !hasToken) {
    // Redirect to login with return URL
    next({
      path: '/login',
      query: { redirect: to.fullPath }
    })
  } else {
    next()
  }
})

export default router