import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import LoginView from '../src/views/LoginView.vue';
import { createRouter, createWebHistory } from 'vue-router';
import { setActivePinia, createPinia } from 'pinia';

vi.mock('../src/services/api', () => ({
  authApi: {
    register: vi.fn(),
    login: vi.fn(() => Promise.resolve({ data: { token: 'test-token', user: { id: 1, username: 'test', email: 'test@test.com' } } })),
    me: vi.fn(),
    google: vi.fn(),
    github: vi.fn(),
  },
}));

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: { template: '<div>Home</div>' } },
    { path: '/login', component: LoginView },
    { path: '/register', component: { template: '<div>Register</div>' } },
    { path: '/dashboard', component: { template: '<div>Dashboard</div>' } },
  ]
});

describe('LoginView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('renders sign in heading', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('h1').text()).toBe('Sign In');
  });

  it('renders email input field', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    const emailInput = wrapper.find('#email');
    expect(emailInput.exists()).toBe(true);
    expect(emailInput.attributes('type')).toBe('email');
  });

  it('renders password input field', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    const passwordInput = wrapper.find('#password');
    expect(passwordInput.exists()).toBe(true);
    expect(passwordInput.attributes('type')).toBe('password');
  });

  it('renders sign in button', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    const submitBtn = wrapper.find('.submit-btn');
    expect(submitBtn.exists()).toBe(true);
    expect(submitBtn.text()).toBe('Sign In');
  });

  it('renders OAuth section with divider', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('.divider').exists()).toBe(true);
  });

  it('renders Google OAuth button', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    const googleBtn = wrapper.find('.oauth-btn.google');
    expect(googleBtn.exists()).toBe(true);
    expect(googleBtn.text()).toContain('Google');
  });

  it('renders GitHub OAuth button', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    const githubBtn = wrapper.find('.oauth-btn.github');
    expect(githubBtn.exists()).toBe(true);
    expect(githubBtn.text()).toContain('GitHub');
  });

  it('renders register link', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    const link = wrapper.find('.register-link');
    expect(link.exists()).toBe(true);
    expect(link.text()).toContain("Don't have an account?");
  });

  it('does not show error initially', () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('.error').exists()).toBe(false);
  });

  it('updates email on input', async () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    await wrapper.find('#email').setValue('test@example.com');
    expect(wrapper.find('#email').element.value).toBe('test@example.com');
  });

  it('updates password on input', async () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] }
    });
    
    await wrapper.find('#password').setValue('password123');
    expect(wrapper.find('#password').element.value).toBe('password123');
  });
});