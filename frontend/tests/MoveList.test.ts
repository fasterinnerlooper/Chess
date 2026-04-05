import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import MoveList from '../src/components/MoveList.vue';
import type { MoveInfo } from '../src/types/chess';

const createMoves = (count: number): MoveInfo[] => {
  const moves: MoveInfo[] = [];
  for (let i = 0; i < count; i++) {
    moves.push({
      san: i % 2 === 0 ? 'e4' : 'e5',
      uci: i % 2 === 0 ? 'e2e4' : 'e7e5',
      fen: 'test-fen',
      moveNumber: Math.floor(i / 2) + 1,
      side: i % 2 === 0 ? 'w' : 'b'
    });
  }
  return moves;
};

describe('MoveList', () => {
  it('renders navigation buttons', () => {
    const wrapper = mount(MoveList, {
      props: { moves: [], currentIndex: -1 }
    });
    
    expect(wrapper.findAll('.nav-btn')).toHaveLength(4);
  });

  it('displays Start at beginning position', () => {
    const wrapper = mount(MoveList, {
      props: { moves: [], currentIndex: -1 }
    });
    
    expect(wrapper.find('.move-info').text()).toBe('Start');
  });

  it('displays End when at last move', () => {
    const moves = createMoves(2);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: 2 }
    });
    
    expect(wrapper.find('.move-info').text()).toBe('End');
  });

  it('displays correct move info', () => {
    const moves = createMoves(2);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: 0 }
    });
    
    expect(wrapper.find('.move-info').text()).toBe('Move 1.White');
  });

  it('displays black move info correctly', () => {
    const moves = createMoves(2);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: 1 }
    });
    
    expect(wrapper.find('.move-info').text()).toBe('Move 1.Black');
  });

  it('renders move groups correctly', () => {
    const moves = createMoves(4);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: -1 }
    });
    
    const moveGroups = wrapper.findAll('.move-group');
    expect(moveGroups).toHaveLength(2);
  });

  it('displays move numbers', () => {
    const moves = createMoves(4);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: -1 }
    });
    
    const moveNumbers = wrapper.findAll('.move-number');
    expect(moveNumbers[0].text()).toBe('1.');
    expect(moveNumbers[1].text()).toBe('2.');
  });

  it('highlights active move', () => {
    const moves = createMoves(4);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: 1 }
    });
    
    const activeButtons = wrapper.findAll('.move-btn.active');
    expect(activeButtons).toHaveLength(1);
  });

  it('emits select event when move button is clicked', async () => {
    const moves = createMoves(2);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: -1 }
    });
    
    const moveButton = wrapper.findAll('.move-btn')[0];
    await moveButton.trigger('click');
    
    expect(wrapper.emitted('select')).toBeTruthy();
    expect(wrapper.emitted('select')?.[0]).toEqual([0]);
  });

  it('disables goToStart button at start position', () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(2), currentIndex: -1 }
    });
    
    const goToStartBtn = wrapper.findAll('.nav-btn')[0];
    expect(goToStartBtn.attributes('disabled')).toBeDefined();
  });

  it('enables goToStart button when not at start', () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(2), currentIndex: 0 }
    });
    
    const goToStartBtn = wrapper.findAll('.nav-btn')[0];
    expect(goToStartBtn.attributes('disabled')).toBeUndefined();
  });

  it('disables previous button at start position', () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(2), currentIndex: -1 }
    });
    
    const prevBtn = wrapper.findAll('.nav-btn')[1];
    expect(prevBtn.attributes('disabled')).toBeDefined();
  });

  it('enables previous button when not at start', () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(2), currentIndex: 1 }
    });
    
    const prevBtn = wrapper.findAll('.nav-btn')[1];
    expect(prevBtn.attributes('disabled')).toBeUndefined();
  });

  it('disables next button at end position', () => {
    const moves = createMoves(2);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: 1 }
    });
    
    const nextBtn = wrapper.findAll('.nav-btn')[2];
    expect(nextBtn.attributes('disabled')).toBeDefined();
  });

  it('enables next button when not at end', () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(2), currentIndex: 0 }
    });
    
    const nextBtn = wrapper.findAll('.nav-btn')[2];
    expect(nextBtn.attributes('disabled')).toBeUndefined();
  });

  it('disables goToEnd button at end position', () => {
    const moves = createMoves(2);
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: 1 }
    });
    
    const goToEndBtn = wrapper.findAll('.nav-btn')[3];
    expect(goToEndBtn.attributes('disabled')).toBeDefined();
  });

  it('emits select -1 when goToStart is clicked', async () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(2), currentIndex: 1 }
    });
    
    const goToStartBtn = wrapper.findAll('.nav-btn')[0];
    await goToStartBtn.trigger('click');
    
    expect(wrapper.emitted('select')?.[0]).toEqual([-1]);
  });

  it('emits correct index when previousMove is clicked', async () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(4), currentIndex: 2 }
    });
    
    const prevBtn = wrapper.findAll('.nav-btn')[1];
    await prevBtn.trigger('click');
    
    expect(wrapper.emitted('select')?.[0]).toEqual([1]);
  });

  it('emits correct index when nextMove is clicked', async () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(4), currentIndex: 0 }
    });
    
    const nextBtn = wrapper.findAll('.nav-btn')[2];
    await nextBtn.trigger('click');
    
    expect(wrapper.emitted('select')?.[0]).toEqual([1]);
  });

  it('emits last index when goToEnd is clicked', async () => {
    const wrapper = mount(MoveList, {
      props: { moves: createMoves(4), currentIndex: -1 }
    });
    
    const goToEndBtn = wrapper.findAll('.nav-btn')[3];
    await goToEndBtn.trigger('click');
    
    expect(wrapper.emitted('select')?.[0]).toEqual([3]);
  });

  it('renders empty when no moves', () => {
    const wrapper = mount(MoveList, {
      props: { moves: [], currentIndex: -1 }
    });
    
    expect(wrapper.find('.moves-container').exists()).toBe(true);
    expect(wrapper.findAll('.move-group')).toHaveLength(0);
  });

  it('handles single move correctly', () => {
    const moves = [{
      san: 'e4',
      uci: 'e2e4',
      fen: 'test',
      moveNumber: 1,
      side: 'w' as const
    }];
    
    const wrapper = mount(MoveList, {
      props: { moves, currentIndex: -1 }
    });
    
    const moveGroups = wrapper.findAll('.move-group');
    expect(moveGroups).toHaveLength(1);
  });
});