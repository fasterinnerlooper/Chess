import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import DashboardView from '../src/views/DashboardView.vue';

vi.mock('../src/stores/game', () => ({
  useGameStore: vi.fn(() => ({
    games: [],
    fetchGames: vi.fn(),
    error: null
  }))
}));

describe('DashboardView', () => {
  it('renders page heading', () => {
    const wrapper = mount(DashboardView, {
      global: {
        stubs: {
          GameImporter: { template: '<div></div>' }
        }
      }
    });
    
    expect(wrapper.find('h1').text()).toBe('My Games');
  });

  it('renders import button', () => {
    const wrapper = mount(DashboardView, {
      global: {
        stubs: {
          GameImporter: { template: '<div></div>' }
        }
      }
    });
    
    const importBtn = wrapper.find('.import-btn');
    expect(importBtn.exists()).toBe(true);
    expect(importBtn.text()).toBe('Import Game');
  });
});