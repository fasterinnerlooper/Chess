import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import ChessBoard from '../src/components/ChessBoard.vue';

describe('ChessBoard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders 64 squares', () => {
    const wrapper = mount(ChessBoard);
    const squares = wrapper.findAll('.square');
    expect(squares).toHaveLength(64);
  });

  it('renders with default props', () => {
    const wrapper = mount(ChessBoard);
    expect(wrapper.find('.chessboard').exists()).toBe(true);
    expect(wrapper.find('.board').exists()).toBe(true);
  });

  it('applies flipped class when orientation is black', () => {
    const wrapper = mount(ChessBoard, {
      props: { orientation: 'black' }
    });
    expect(wrapper.find('.chessboard.flipped').exists()).toBe(true);
  });

  it('does not apply flipped class when orientation is white', () => {
    const wrapper = mount(ChessBoard, {
      props: { orientation: 'white' }
    });
    expect(wrapper.find('.chessboard.flipped').exists()).toBe(false);
  });

  it('highlights last move squares', () => {
    const wrapper = mount(ChessBoard, {
      props: { lastMove: { from: 'e2', to: 'e4' } }
    });
    const squares = wrapper.findAll('.square');
    const lastMoveSquares = squares.filter(s => s.classes('last-move'));
    expect(lastMoveSquares).toHaveLength(2);
  });

  it('does not highlight last move when highlightLastMove is false', () => {
    const wrapper = mount(ChessBoard, {
      props: { 
        lastMove: { from: 'e2', to: 'e4' },
        highlightLastMove: false
      }
    });
    const lastMoveSquare = wrapper.find('.square.last-move');
    expect(lastMoveSquare.exists()).toBe(false);
  });

  it('displays coordinates by default', () => {
    const wrapper = mount(ChessBoard, {
      props: { showCoordinates: true }
    });
    expect(wrapper.find('.coordinate').exists()).toBe(true);
  });

  it('hides coordinates when showCoordinates is false', () => {
    const wrapper = mount(ChessBoard, {
      props: { showCoordinates: false }
    });
    expect(wrapper.find('.coordinate').exists()).toBe(false);
  });

  it('emits clickSquare event when square is clicked', async () => {
    const wrapper = mount(ChessBoard);
    const square = wrapper.findAll('.square')[0];
    await square.trigger('click');
    expect(wrapper.emitted('clickSquare')).toBeTruthy();
  });

  it('renders pieces with correct classes', () => {
    const wrapper = mount(ChessBoard, {
      props: {
        fen: 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'
      }
    });
    const whitePieces = wrapper.findAll('.white-piece');
    const blackPieces = wrapper.findAll('.black-piece');
    expect(whitePieces.length).toBeGreaterThan(0);
    expect(blackPieces.length).toBeGreaterThan(0);
  });

  it('renders king piece on board', () => {
    const wrapper = mount(ChessBoard, {
      props: {
        fen: 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'
      }
    });
    const pieces = wrapper.findAll('.piece');
    const kingSymbols = pieces.filter(p => p.text() === '♔' || p.text() === '♚');
    expect(kingSymbols.length).toBe(2);
  });
});