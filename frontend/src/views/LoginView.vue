<template>
  <div class="login-container">
    <div class="login-box">
      <div class="login-header">
        <h1>SQL Analyzer</h1>
        <p>Please sign in to continue</p>
      </div>
      
      <form @submit.prevent="handleLogin" class="login-form">
        <div class="field">
          <label for="username">Username</label>
          <InputText 
            id="username" 
            v-model="credentials.username" 
            placeholder="Enter username"
            :class="{ 'p-invalid': errors.username }"
            @input="clearError('username')"
          />
          <small v-if="errors.username" class="p-error">{{ errors.username }}</small>
        </div>
        
        <div class="field">
          <label for="password">Password</label>
          <Password 
            id="password" 
            v-model="credentials.password" 
            placeholder="Enter password"
            :feedback="false"
            :toggleMask="true"
            :class="{ 'p-invalid': errors.password }"
            @input="clearError('password')"
          />
          <small v-if="errors.password" class="p-error">{{ errors.password }}</small>
        </div>
        
        <Message v-if="errorMessage" severity="error" :closable="false">
          {{ errorMessage }}
        </Message>
        
        <Button 
          type="submit" 
          label="Sign In" 
          :loading="loading"
          :disabled="loading"
          class="w-full"
        />
      </form>
      
      <div class="login-footer">
        <small>Default credentials: admin / AnalyzeThis!!</small>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'

const router = useRouter()
const authStore = useAuthStore()
const toast = useToast()

const credentials = reactive({
  username: '',
  password: ''
})

const errors = reactive({
  username: '',
  password: ''
})

const errorMessage = ref('')
const loading = ref(false)

const clearError = (field) => {
  errors[field] = ''
  errorMessage.value = ''
}

const validate = () => {
  let isValid = true
  
  if (!credentials.username) {
    errors.username = 'Username is required'
    isValid = false
  }
  
  if (!credentials.password) {
    errors.password = 'Password is required'
    isValid = false
  }
  
  return isValid
}

const handleLogin = async () => {
  errorMessage.value = ''
  
  if (!validate()) {
    return
  }
  
  loading.value = true
  
  const result = await authStore.login(credentials.username, credentials.password)
  
  if (result.success) {
    toast.add({
      severity: 'success',
      summary: 'Login Successful',
      detail: 'Welcome back!',
      life: 3000
    })
    
    // Redirect to dashboard or previous page
    const redirect = router.currentRoute.value.query.redirect || '/'
    router.push(redirect)
  } else {
    errorMessage.value = result.error || 'Invalid username or password'
  }
  
  loading.value = false
}
</script>

<style scoped lang="scss">
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 1rem;
}

.login-box {
  background: white;
  border-radius: 10px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  width: 100%;
  max-width: 400px;
  padding: 2rem;
}

.login-header {
  text-align: center;
  margin-bottom: 2rem;
  
  h1 {
    color: #333;
    margin-bottom: 0.5rem;
  }
  
  p {
    color: #666;
    margin: 0;
  }
}

.login-form {
  .field {
    margin-bottom: 1.5rem;
    
    label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 600;
      color: #333;
    }
    
    :deep(.p-inputtext),
    :deep(.p-password-input) {
      width: 100%;
    }
  }
  
  :deep(.p-message) {
    margin-bottom: 1rem;
  }
}

.login-footer {
  margin-top: 1.5rem;
  text-align: center;
  color: #666;
}
</style>