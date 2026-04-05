<template>
  <div id="app">
    <nav class="navbar" v-if="showNav">
      <div class="nav-brand">
        <router-link to="/">Chesster</router-link>
      </div>
      <div class="nav-links">
        <router-link to="/dashboard" v-if="isAuthenticated">My Games</router-link>
        <router-link to="/login" v-if="!isAuthenticated">Login</router-link>
        <router-link to="/register" v-if="!isAuthenticated">Register</router-link>
        <button @click="logout" v-if="isAuthenticated" class="logout-btn">Logout</button>
      </div>
    </nav>
    <main>
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const authStore = useAuthStore();

const isAuthenticated = computed(() => authStore.isAuthenticated);

const showNav = computed(() => {
  const route = router.currentRoute.value;
  return route.name !== 'oauth-callback' && route.name !== 'game';
});

onMounted(() => {
  authStore.initFromToken();
});

function logout() {
  authStore.logout();
  router.push('/');
}
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
  background: #fff;
  color: #333;
}

#app {
  min-height: 100vh;
}

.navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  background: #2c3e50;
  color: #fff;
}

.nav-brand a {
  font-size: 24px;
  font-weight: bold;
  color: #fff;
  text-decoration: none;
}

.nav-links {
  display: flex;
  gap: 16px;
  align-items: center;
}

.nav-links a {
  color: #fff;
  text-decoration: none;
}

.nav-links a:hover {
  color: #4CAF50;
}

.logout-btn {
  background: none;
  border: 1px solid #fff;
  color: #fff;
  padding: 6px 12px;
  border-radius: 4px;
  cursor: pointer;
}

.logout-btn:hover {
  background: #fff;
  color: #2c3e50;
}

main {
  min-height: calc(100vh - 60px);
}
</style>