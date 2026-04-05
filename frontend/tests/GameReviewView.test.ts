import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import GameReviewView from '../src/views/GameReviewView.vue';
import { createRouter, createWebHistory } from 'vue-router';
import { setActivePinia, createPinia } from 'pinia';

vi.mock('chess.js', () => {
  const mockChess = {
    loadPgn: vi.fn(),
    fen: vi.fn(() => 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'),
    history: vi.fn(() => [
      { san: 'e4', from: 'e2', to: 'e4', promotion: undefined }
    ]),
    load: vi.fn(),
    reset: vi.fn(),
    move: vi.fn(),
    board: vi.fn(() => Array(8).fill(null).map(() => Array(8).fill(null))),
  };
  return { Chess: vi.fn(() => mockChess) };
});

vi.mock('../src/stores/game', () => ({
  useGameStore: vi.fn(() => ({
    currentGame: { id: '1', pgn: '1. e4', white: 'White', black: 'Black', result: '1-0' },
    currentPosition: 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1',
    gameHistory: [
      { san: 'e4', uci: 'e2e4', fen: 'test', moveNumber: 1, side: 'w' }
    ],
    currentMoveIndex: 0,
    currentAnalysis: null,
    lastMove: { from: 'e2', to: 'e4' },
    loading: false,
    analyzing: false,
    error: null,
    fetchGame: vi.fn(),
    goToMove: vi.fn(),
    analyzeGame: vi.fn()
  }))
}));

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: { template: '<div>Home</div>' } },
    { path: '/dashboard', component: { template: '<div>Dashboard</div>' } },
    { path: '/game/:id', component: GameReviewView },
  ]
});

describe('GameReviewView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('renders back button', () => {
    const wrapper = mount(GameReviewView, {
      global: { 
        plugins: [router],
        stubs: {
          ChessBoard: { template: '<div class="chessboard"></div>' },
          MoveList: { template: '<div class="movelist"></div>' },
          ExplanationPanel: { template: '<div class="explanation"></div>' }
        }
      },
      props: { id: '1' }
    });
    
    const backBtn = wrapper.find('.back-btn');
    expect(backBtn.exists()).toBe(true);
    expect(backBtn.text()).toBe('← Back');
  });

  it('renders game title with players', () => {
    const wrapper = mount(GameReviewView, {
      global: { 
        plugins: [router],
        stubs: {
          ChessBoard: { template: '<div class="chessboard"></div>' },
          MoveList: { template: '<div class="movelist"></div>' },
          ExplanationPanel: { template: '<div class="explanation"></div>' }
        }
      },
      props: { id: '1' }
    });
    
    expect(wrapper.find('.game-title').text()).toContain('White vs Black');
  });

  it('renders analyze button', () => {
    const wrapper = mount(GameReviewView, {
      global: { 
        plugins: [router],
        stubs: {
          ChessBoard: { template: '<div class="chessboard"></div>' },
          MoveList: { template: '<div class="movelist"></div>' },
          ExplanationPanel: { template: '<div class="explanation"></div>' }
        }
      },
      props: { id: '1' }
    });
    
    const analyzeBtn = wrapper.find('.analyze-btn');
    expect(analyzeBtn.exists()).toBe(true);
    expect(analyzeBtn.text()).toBe('Analyze');
  });

  it('renders ChessBoard component', () => {
    const wrapper = mount(GameReviewView, {
      global: { 
        plugins: [router],
        stubs: {
          ChessBoard: { template: '<div class="chessboard"></div>' },
          MoveList: { template: '<div class="movelist"></div>' },
          ExplanationPanel: { template: '<div class="explanation"></div>' }
        }
      },
      props: { id: '1' }
    });
    
    expect(wrapper.find('.board-section').exists()).toBe(true);
  });

  it('renders MoveList component', () => {
    const wrapper = mount(GameReviewView, {
      global: { 
        plugins: [router],
        stubs: {
          ChessBoard: { template: '<div class="chessboard"></div>' },
          MoveList: { template: '<div class="movelist"></div>' },
          ExplanationPanel: { template: '<div class="explanation"></div>' }
        }
      },
      props: { id: '1' }
    });
    
    expect(wrapper.find('.moves-section').exists()).toBe(true);
  });

  it('renders ExplanationPanel component', () => {
    const wrapper = mount(GameReviewView, {
      global: { 
        plugins: [router],
        stubs: {
          ChessBoard: { template: '<div class="chessboard"></div>' },
          MoveList: { template: '<div class="movelist"></div>' },
          ExplanationPanel: { template: '<div class="explanation"></div>' }
        }
      },
      props: { id: '1' }
    });
    
    expect(wrapper.find('.explanation-section').exists()).toBe(true);
  });
});