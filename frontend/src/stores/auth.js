import { defineStore } from 'pinia'
import axios from 'axios'
import { useRouter } from 'vue-router'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'
console.log('Auth Store API Base URL:', API_BASE_URL) // Debug log

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
    token: localStorage.getItem('token'),
    isAuthenticated: false,
    loading: false,
    error: null
  }),

  getters: {
    username: (state) => state.user?.username || '',
    isLoggedIn: (state) => !!state.token
  },

  actions: {
    async login(username, password) {
      this.loading = true
      this.error = null
      
      try {
        const response = await axios.post(`${API_BASE_URL}/api/v1/auth/login`, {
          username,
          password
        })
        
        const { token, username: user, expiresAt } = response.data
        
        this.token = token
        this.user = { username: user }
        this.isAuthenticated = true
        
        // Store token in localStorage
        localStorage.setItem('token', token)
        localStorage.setItem('tokenExpiry', expiresAt)
        
        // Set default authorization header
        axios.defaults.headers.common['Authorization'] = `Bearer ${token}`
        
        return { success: true }
      } catch (error) {
        this.error = error.response?.data?.message || 'Login failed'
        return { success: false, error: this.error }
      } finally {
        this.loading = false
      }
    },

    async logout() {
      try {
        await axios.post(`${API_BASE_URL}/api/v1/auth/logout`)
      } catch (error) {
        console.error('Logout error:', error)
      }
      
      // Clear auth state
      this.token = null
      this.user = null
      this.isAuthenticated = false
      
      // Clear localStorage
      localStorage.removeItem('token')
      localStorage.removeItem('tokenExpiry')
      
      // Remove authorization header
      delete axios.defaults.headers.common['Authorization']
      
      // Redirect to login
      const router = useRouter()
      router.push('/login')
    },

    async checkAuth() {
      if (!this.token) {
        return false
      }

      // Check if token is expired
      const expiry = localStorage.getItem('tokenExpiry')
      if (expiry && new Date(expiry) < new Date()) {
        await this.logout()
        return false
      }

      try {
        // Set authorization header
        axios.defaults.headers.common['Authorization'] = `Bearer ${this.token}`
        
        // Verify token with server
        const response = await axios.get(`${API_BASE_URL}/api/v1/auth/verify`)
        
        this.user = { username: response.data.username, role: response.data.role }
        this.isAuthenticated = true
        
        return true
      } catch (error) {
        await this.logout()
        return false
      }
    },

    initializeAuth() {
      // Check for existing token on app startup
      if (this.token) {
        axios.defaults.headers.common['Authorization'] = `Bearer ${this.token}`
        // Don't check auth on startup to avoid 400 errors
        this.isAuthenticated = true
      }
    }
  }
})