import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore } from '../src/stores/auth';

vi.mock('../src/services/api', () => ({
  authApi: {
    register: vi.fn(() => Promise.resolve({ data: { token: 'test-token', user: { id: 1, username: 'test', email: 'test@test.com' } } })),
    login: vi.fn(() => Promise.resolve({ data: { token: 'test-token', user: { id: 1, username: 'test', email: 'test@test.com' } } })),
    me: vi.fn(() => Promise.resolve({ data: { id: 1, username: 'test', email: 'test@test.com' } })),
    google: vi.fn(),
    github: vi.fn(),
  },
}));

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('should initialize with null user and token', () => {
    const store = useAuthStore();
    expect(store.user).toBeNull();
    expect(store.loading).toBe(false);
    expect(store.error).toBeNull();
    expect(store.isAuthenticated).toBe(false);
  });

  it('should set token via login', async () => {
    const store = useAuthStore();
    
    await store.login({
      username: 'testuser',
      password: 'password123'
    });
    
    expect(store.token).toBe('test-token');
    expect(localStorage.getItem('token')).toBe('test-token');
  });

  it('should remove token on logout', async () => {
    localStorage.setItem('token', 'existing-token');
    const store = useAuthStore();
    store.token = 'existing-token';
    
    store.logout();
    
    expect(store.token).toBeNull();
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('should register successfully', async () => {
    const store = useAuthStore();
    
    const result = await store.register({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123'
    });
    
    expect(result).toBe(true);
    expect(store.token).toBe('test-token');
    expect(store.user).toEqual({
      id: 1,
      username: 'test',
      email: 'test@test.com'
    });
    expect(store.error).toBeNull();
    expect(store.loading).toBe(false);
  });

  it('should handle registration error', async () => {
    const { authApi } = await import('../src/services/api');
    const store = useAuthStore();
    
    const mockError = {
      response: {
        data: {
          message: 'Username already exists'
        }
      }
    };
    
    vi.mocked(authApi.register).mockRejectedValue(mockError);
    
    const result = await store.register({
      username: 'existinguser',
      email: 'test@example.com',
      password: 'password123'
    });
    
    expect(result).toBe(false);
    expect(store.error).toBe('Username already exists');
    expect(store.loading).toBe(false);
    expect(store.user).toBeNull();
  });

  it('should handle registration error without response data', async () => {
    const { authApi } = await import('../src/services/api');
    const store = useAuthStore();
    
    const mockError = new Error('Network error');
    vi.mocked(authApi.register).mockRejectedValue(mockError);
    
    const result = await store.register({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123'
    });
    
    expect(result).toBe(false);
    expect(store.error).toBe('Registration failed');
    expect(store.loading).toBe(false);
  });

  it('should login successfully', async () => {
    const store = useAuthStore();
    
    const result = await store.login({
      username: 'testuser',
      password: 'password123'
    });
    
    expect(result).toBe(true);
    expect(store.token).toBe('test-token');
    expect(store.loading).toBe(false);
  });

  it('should handle login error', async () => {
    const { authApi } = await import('../src/services/api');
    const store = useAuthStore();
    
    const mockError = {
      response: {
        data: {
          message: 'Invalid credentials'
        }
      }
    };
    
    vi.mocked(authApi.login).mockRejectedValue(mockError);
    
    const result = await store.login({
      username: 'wronguser',
      password: 'wrongpassword'
    });
    
    expect(result).toBe(false);
    expect(store.error).toBe('Invalid credentials');
    expect(store.loading).toBe(false);
  });

  it('should set loading state during async operations', async () => {
    const { authApi } = await import('../src/services/api');
    const store = useAuthStore();
    
    vi.mocked(authApi.login).mockImplementation(() => 
      new Promise(resolve => {
        setTimeout(() => resolve({
          data: {
            token: 'test-token',
            user: { id: 1, username: 'testuser', email: 'test@example.com' }
          }
        }), 100);
      })
    );
    
    const loginPromise = store.login({
      username: 'testuser',
      password: 'password123'
    });
    
    expect(store.loading).toBe(true);
    
    await loginPromise;
    expect(store.loading).toBe(false);
  });

  it('should clear error on successful operation', async () => {
    const store = useAuthStore();
    
    store.error = 'Previous error';
    
    await store.login({
      username: 'testuser',
      password: 'password123'
    });
    
    expect(store.error).toBeNull();
  });

  it('should return isAuthenticated computed property correctly', () => {
    const store = useAuthStore();
    
    expect(store.isAuthenticated).toBe(false);
    
    store.token = 'test-token';
    expect(store.isAuthenticated).toBe(false);
    
    store.user = { id: 1, username: 'testuser', email: 'test@example.com' };
    expect(store.isAuthenticated).toBe(true);
    
    store.token = null;
    expect(store.isAuthenticated).toBe(false);
  });

  it('should fetch user successfully', async () => {
    const store = useAuthStore();
    
    store.token = 'test-token';
    
    await store.fetchUser();
    
    expect(store.user).toEqual({
      id: 1,
      username: 'test',
      email: 'test@test.com'
    });
    expect(store.error).toBeNull();
  });

  it('should handle fetch user error', async () => {
    const { authApi } = await import('../src/services/api');
    const store = useAuthStore();
    
    store.token = 'test-token';
    
    const mockError = {
      response: {
        status: 401
      }
    };
    
    vi.mocked(authApi.me).mockRejectedValue(mockError);
    
    await store.fetchUser();
    
    expect(store.user).toBeNull();
    expect(store.token).toBeNull();
  });

  it('should not call me API if no token is present', async () => {
    const { authApi } = await import('../src/services/api');
    const store = useAuthStore();
    
    await store.fetchUser();
    
    expect(authApi.me).not.toHaveBeenCalled();
  });
});