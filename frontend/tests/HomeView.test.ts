import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import HomeView from '../src/views/HomeView.vue';
import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/login', component: { template: '<div>Login</div>' } },
    { path: '/register', component: { template: '<div>Register</div>' } },
  ]
});

describe('HomeView', () => {
  it('renders welcome heading', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('h1').text()).toBe('Welcome to Chesster');
  });

  it('renders tagline', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('.tagline').text()).toBe('Learn chess through Chernev-style analysis');
  });

  it('renders Get Started button', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    const primaryBtn = wrapper.find('.btn.primary');
    expect(primaryBtn.exists()).toBe(true);
    expect(primaryBtn.text()).toBe('Get Started');
  });

  it('renders Sign In button', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    const secondaryBtn = wrapper.find('.btn.secondary');
    expect(secondaryBtn.exists()).toBe(true);
    expect(secondaryBtn.text()).toBe('Sign In');
  });

  it('renders all feature sections', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    const features = wrapper.findAll('.feature');
    expect(features).toHaveLength(4);
  });

  it('renders Chernev-style analysis feature', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.text()).toContain('Chernev-Style Analysis');
  });

  it('renders Stockfish integration feature', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.text()).toContain('Stockfish Integration');
  });

  it('renders game import feature', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.text()).toContain('Game Import');
  });

  it('renders secure login feature', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.text()).toContain('Secure Login');
  });

  it('applies correct classes to buttons', () => {
    const wrapper = mount(HomeView, {
      global: { plugins: [router] }
    });
    
    expect(wrapper.find('.btn.primary').classes()).toContain('primary');
    expect(wrapper.find('.btn.secondary').classes()).toContain('secondary');
  });
});