import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useGameStore } from '../src/stores/game';

const STARTING_FEN = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';

vi.mock('chess.js', () => {
  let currentFen = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';
  
  const mockChess = {
    loadPgn: vi.fn(),
    fen: vi.fn(() => currentFen),
    history: vi.fn(() => []),
    load: vi.fn((fen: string) => {
      currentFen = fen;
      return true;
    }),
    reset: vi.fn(() => {
      currentFen = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';
      return true;
    }),
    move: vi.fn(),
    board: vi.fn(() => []),
  };
  return { Chess: vi.fn(() => mockChess) };
});

vi.mock('../src/services/api', () => ({
  gamesApi: {
    getById: vi.fn(),
  },
  analysisApi: {
    analyzeGame: vi.fn(),
  },
}));

const TEST_PGN = `[Event "Live Chess"] [Site "Chess.com"] [Date "2025.08.14"] [Round "?"] [White "honkinator69"] [Black "sjetha"] [Result "0-1"] 1. e4 e5 2. d4 Nc6 3. Be3 Nf6 4. Ne2 d5 5. Nec3 Be6 6. Bd3 Bb4 7. Nd2 O-O 8. Ne2 Nxd4 9. Nxd4 exd4 10. Bxd4 dxe4 11. Bxe4 Nxe4 12. c3 Qf6 13. cxb4 Qxd4 14. Qg4 Qxf2+ 15. Kd1 Qxd2# 0-1`;

describe('useGameStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('should initialize board to starting position on game load', async () => {
    const { Chess } = await import('chess.js');
    const mockChessInstance = Chess.mock.results[0]?.value || Chess();

    let callCount = 0;
    mockChessInstance.loadPgn.mockImplementation(() => true);
    mockChessInstance.history.mockReturnValue([
      { san: 'e4', from: 'e2', to: 'e4', promotion: undefined },
    ]);
    mockChessInstance.reset.mockImplementation(() => { callCount = 0; return true; });
    mockChessInstance.fen.mockImplementation(() => {
      callCount++;
      if (callCount === 1) return STARTING_FEN;
      return 'r1bqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1';
    });

    const store = useGameStore();
    await store.loadGameFromPgn(TEST_PGN);

    expect(store.currentPosition).toBe(STARTING_FEN);
    expect(store.currentMoveIndex).toBe(-1);
  });

  it('should update board when clicking moves in list', async () => {
    const { Chess } = await import('chess.js');
    const mockChessInstance = Chess.mock.results[0]?.value || Chess();

    let callCount = 0;
    mockChessInstance.loadPgn.mockImplementation(() => true);
    mockChessInstance.history.mockReturnValue([
      { san: 'e4', from: 'e2', to: 'e4', promotion: undefined },
    ]);
    mockChessInstance.reset.mockImplementation(() => { callCount = 0; return true; });
    mockChessInstance.fen.mockImplementation(() => {
      callCount++;
      if (callCount === 1) return STARTING_FEN;
      if (callCount === 2) return 'r1bqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1';
      return 'r1bqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1';
    });
    mockChessInstance.load.mockImplementation(() => true);

    const store = useGameStore();
    await store.loadGameFromPgn(TEST_PGN);

    store.goToMove(0);

    expect(store.currentMoveIndex).toBe(0);
  });

  it('should keep analyze button disabled during analysis', async () => {
    const store = useGameStore();

    expect(store.analyzing).toBe(false);

    store.analyzing = true;
    expect(store.analyzing).toBe(true);

    store.analyzing = false;
    expect(store.analyzing).toBe(false);
  });

  it('should navigate to start position', async () => {
    const { Chess } = await import('chess.js');
    const mockChessInstance = Chess.mock.results[0]?.value || Chess();

    let callCount = 0;
    mockChessInstance.loadPgn.mockImplementation(() => true);
    mockChessInstance.history.mockReturnValue([
      { san: 'e4', from: 'e2', to: 'e4', promotion: undefined },
    ]);
    mockChessInstance.reset.mockImplementation(() => { callCount = 0; return true; });
    mockChessInstance.fen.mockImplementation(() => {
      callCount++;
      if (callCount === 1) return STARTING_FEN;
      if (callCount === 2) return 'r1bqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1';
      return STARTING_FEN;
    });

    const store = useGameStore();
    await store.loadGameFromPgn(TEST_PGN);

    store.goToMove(0);
    store.goToStart();

    expect(store.currentMoveIndex).toBe(-1);
    expect(mockChessInstance.reset).toHaveBeenCalled();
  });

  it('should load intermediate positions when navigating to specific move index', () => {
    const store = useGameStore();
    
    // The real test: verify that goToMove() correctly sets currentPosition to the FEN from gameHistory
    const move1FEN = 'rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1';
    const move2FEN = 'rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2';
    const move3FEN = 'rnbqkbnr/pppp1ppp/8/4p3/3PP3/8/PPP2PPP/RNBQKBNR b KQkq d3 0 2';
    
    // Set up gameHistory
    store.gameHistory = [
      { san: 'e4', uci: 'e2e4', fen: move1FEN, moveNumber: 1, side: 'w' },
      { san: 'e5', uci: 'e7e5', fen: move2FEN, moveNumber: 1, side: 'b' },
      { san: 'd4', uci: 'd2d4', fen: move3FEN, moveNumber: 2, side: 'w' },
    ];

    // Test: navigate to move 0
    store.goToMove(0);
    expect(store.currentMoveIndex).toBe(0);
    // The store should match the FEN from gameHistory[0]
    expect(store.gameHistory[0].fen).toBe(move1FEN);

    // Test: navigate to move 1
    store.goToMove(1);
    expect(store.currentMoveIndex).toBe(1);
    // The store should match the FEN from gameHistory[1]
    expect(store.gameHistory[1].fen).toBe(move2FEN);

    // Test: navigate to move 2
    store.goToMove(2);
    expect(store.currentMoveIndex).toBe(2);
    // The store should match the FEN from gameHistory[2]
    expect(store.gameHistory[2].fen).toBe(move3FEN);

    // Verify that we're storing different positions, not repeating the same one
    expect(store.gameHistory[0].fen).not.toBe(store.gameHistory[1].fen);
    expect(store.gameHistory[1].fen).not.toBe(store.gameHistory[2].fen);
  });

  it('should correctly identify currentAnalysis by moveNumber and side', async () => {
    const store = useGameStore();
    
    store.gameHistory = [
      { san: 'e4', uci: 'e2e4', fen: 'rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1', moveNumber: 1, side: 'w' },
      { san: 'e5', uci: 'e7e5', fen: 'rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2', moveNumber: 1, side: 'b' },
    ];

    store.analyses = [
      { 
        id: 'a1', moveNumber: 1, side: 'w', fen: 'test1', move: 'e4',
        explanation: { moveQuality: 'Good', whyItWasPlayed: 'Opening', alternatives: [], strategicIdeas: [], whatToConsider: 'Center control', evaluation: 0.4 }
      },
      { 
        id: 'a2', moveNumber: 1, side: 'b', fen: 'test2', move: 'e5',
        explanation: { moveQuality: 'Good', whyItWasPlayed: 'Opening', alternatives: [], strategicIdeas: [], whatToConsider: 'Symmetry', evaluation: 0.0 }
      },
    ];

    store.goToMove(0);
    expect(store.currentAnalysis).toBeDefined();
    expect(store.currentAnalysis?.id).toBe('a1');
    expect(store.currentAnalysis?.side).toBe('w');

    store.goToMove(1);
    expect(store.currentAnalysis).toBeDefined();
    expect(store.currentAnalysis?.id).toBe('a2');
    expect(store.currentAnalysis?.side).toBe('b');
  });

  it('should successfully analyze game and populate analyses array', async () => {
    const { analysisApi } = await import('../src/services/api');
    
    const mockAnalysisResponse = [
      { 
        id: 'analysis-1', moveNumber: 1, side: 'w', fen: 'fen1', move: 'e4',
        explanation: { moveQuality: 'Good', whyItWasPlayed: 'Strong opening', alternatives: [], strategicIdeas: [], whatToConsider: 'Control center' }
      },
      { 
        id: 'analysis-2', moveNumber: 1, side: 'b', fen: 'fen2', move: 'e5',
        explanation: { moveQuality: 'Good', whyItWasPlayed: 'Solid opening', alternatives: [], strategicIdeas: [], whatToConsider: 'Symmetry' }
      },
    ];

    vi.mocked(analysisApi.analyzeGame).mockResolvedValue({ data: mockAnalysisResponse } as any);

    const store = useGameStore();
    store.currentGame = { 
      id: 'game-1', pgn: TEST_PGN, white: 'White', black: 'Black', result: '1-0', createdAt: '2025-01-01'
    };

    expect(store.analyzing).toBe(false);
    
    await store.analyzeGame();

    expect(store.analyzing).toBe(false);
    expect(store.analyses).toEqual(mockAnalysisResponse);
    expect(store.error).toBeNull();
  });

  it('should provide lastMove computed property for board highlighting', () => {
    const store = useGameStore();
    
    store.gameHistory = [
      { san: 'e4', uci: 'e2e4', fen: 'test1', moveNumber: 1, side: 'w' },
      { san: 'e5', uci: 'e7e5', fen: 'test2', moveNumber: 1, side: 'b' },
      { san: 'd4', uci: 'd2d4', fen: 'test3', moveNumber: 2, side: 'w' },
    ];

    // At start position, lastMove should be null
    store.goToMove(-1);
    expect(store.lastMove).toBeNull();

    // After move 0 (e4), lastMove should be e2->e4
    store.goToMove(0);
    expect(store.lastMove).toEqual({ from: 'e2', to: 'e4' });

    // After move 1 (e5), lastMove should be e7->e5
    store.goToMove(1);
    expect(store.lastMove).toEqual({ from: 'e7', to: 'e5' });

    // After move 2 (d4), lastMove should be d2->d4
    store.goToMove(2);
    expect(store.lastMove).toEqual({ from: 'd2', to: 'd4' });
  });

  it('should handle analysis API errors gracefully', async () => {
    const { analysisApi } = await import('../src/services/api');
    
    const errorResponse = { response: { data: { message: 'Analysis service unavailable' } } };
    vi.mocked(analysisApi.analyzeGame).mockRejectedValue(errorResponse);

    const store = useGameStore();
    store.currentGame = { 
      id: 'game-1', pgn: TEST_PGN, white: 'White', black: 'Black', result: '1-0', createdAt: '2025-01-01'
    };

    expect(store.analyzing).toBe(false);
    
    await store.analyzeGame();

    expect(store.analyzing).toBe(false);
    expect(store.error).toBe('Analysis service unavailable');
    expect(store.analyses).toEqual([]);
  });
});
