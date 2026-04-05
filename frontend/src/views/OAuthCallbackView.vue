<template>
  <div class="oauth-callback">
    <div v-if="loading">Processing login...</div>
    <div v-else-if="error" class="error">
      {{ error }}
      <router-link to="/login">Back to login</router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const authStore = useAuthStore();
const loading = ref(true);
const error = ref('');

onMounted(async () => {
  const urlParams = new URLSearchParams(window.location.search);
  const token = urlParams.get('token');
  
  if (token) {
    localStorage.setItem('token', token);
    await authStore.fetchUser();
    router.push('/dashboard');
  } else {
    error.value = 'No token received';
  }
  
  loading.value = false;
});
</script>

<style scoped>
.oauth-callback {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.error {
  text-align: center;
  color: #c62828;
}

.error a {
  display: block;
  margin-top: 16px;
  color: #4CAF50;
}
</style>