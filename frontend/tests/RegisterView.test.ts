import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import RegisterView from '../src/views/RegisterView.vue';
import { createRouter, createWebHistory } from 'vue-router';
import { setActivePinia, createPinia } from 'pinia';

vi.mock('../src/services/api', () => ({
  authApi: {
    register: vi.fn(() => Promise.resolve({ data: { token: 'test-token', user: { id: 1, username: 'test', email: 'test@test.com' } } })),
    login: vi.fn(),
    me: vi.fn(),
    google: vi.fn(),
    github: vi.fn(),
  },
}));

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: { template: '<div>Home</div>' } },
    { path: '/login', component: { template: '<div>Login</div>' } },
    { path: '/register', component: RegisterView },
    { path: '/dashboard', component: { template: '<div>Dashboard</div>' } },
  ]
});

describe('RegisterView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('renders create account heading', () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('h1').text()).toBe('Create Account');
  });

  it('renders username input field', () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    const usernameInput = wrapper.find('#username');
    expect(usernameInput.exists()).toBe(true);
    expect(usernameInput.attributes('type')).toBe('text');
  });

  it('renders email input field', () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    const emailInput = wrapper.find('#email');
    expect(emailInput.exists()).toBe(true);
    expect(emailInput.attributes('type')).toBe('email');
  });

  it('renders password input field', () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    const passwordInput = wrapper.find('#password');
    expect(passwordInput.exists()).toBe(true);
    expect(passwordInput.attributes('type')).toBe('password');
  });

  it('renders create account button', () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    const submitBtn = wrapper.find('.submit-btn');
    expect(submitBtn.exists()).toBe(true);
    expect(submitBtn.text()).toBe('Create Account');
  });

  it('renders login link', () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    const link = wrapper.find('.login-link');
    expect(link.exists()).toBe(true);
    expect(link.text()).toContain('Already have an account?');
  });

  it('does not show error initially', () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('.error').exists()).toBe(false);
  });

  it('updates username on input', async () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    await wrapper.find('#username').setValue('testuser');
    expect(wrapper.find('#username').element.value).toBe('testuser');
  });

  it('updates email on input', async () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    await wrapper.find('#email').setValue('test@example.com');
    expect(wrapper.find('#email').element.value).toBe('test@example.com');
  });

  it('updates password on input', async () => {
    const wrapper = mount(RegisterView, {
      global: { plugins: [router] }
    });
    
    await wrapper.find('#password').setValue('password123');
    expect(wrapper.find('#password').element.value).toBe('password123');
  });
});