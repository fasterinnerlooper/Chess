import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { User, RegisterRequest, LoginRequest } from '@/types/chess';
import { authApi } from '@/services/api';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const token = ref<string | null>(localStorage.getItem('token'));
  const loading = ref(false);
  const error = ref<string | null>(null);

  const isAuthenticated = computed(() => !!token.value && !!user.value);

  function setToken(newToken: string | null) {
    token.value = newToken;
    if (newToken) {
      localStorage.setItem('token', newToken);
    } else {
      localStorage.removeItem('token');
    }
  }

  async function register(data: RegisterRequest) {
    loading.value = true;
    error.value = null;
    try {
      const response = await authApi.register(data);
      setToken(response.data.token);
      user.value = response.data.user;
      return true;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Registration failed';
      return false;
    } finally {
      loading.value = false;
    }
  }

  async function login(data: LoginRequest) {
    loading.value = true;
    error.value = null;
    try {
      const response = await authApi.login(data);
      setToken(response.data.token);
      user.value = response.data.user;
      return true;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Login failed';
      return false;
    } finally {
      loading.value = false;
    }
  }

  async function fetchUser() {
    if (!token.value) return;
    try {
      const response = await authApi.me();
      user.value = response.data;
    } catch (e) {
      logout();
    }
  }

  function logout() {
    setToken(null);
    user.value = null;
  }

  function initFromToken() {
    if (token.value) {
      fetchUser();
    }
  }

  return {
    user,
    token,
    loading,
    error,
    isAuthenticated,
    register,
    login,
    logout,
    fetchUser,
    initFromToken
  };
});