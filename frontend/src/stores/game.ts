import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { Chess } from 'chess.js';
import type { Game, Analysis, MoveInfo } from '@/types/chess';
import { gamesApi, analysisApi } from '@/services/api';

export const useGameStore = defineStore('game', () => {
  const games = ref<Game[]>([]);
  const currentGame = ref<Game | null>(null);
  const currentPosition = ref<string>('rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1');
  const currentMoveIndex = ref(-1);
  const gameHistory = ref<MoveInfo[]>([]);
  const analyses = ref<Analysis[]>([]);
  const loading = ref(false);
  const analyzing = ref(false);
  const error = ref<string | null>(null);

  const chess = new Chess();
  const currentAnalysis = computed((): Analysis | null => {
    if (currentMoveIndex.value < 0 || !analyses.value.length) return null;
    const moveInfo = gameHistory.value[currentMoveIndex.value];
    if (!moveInfo) return null;
    return analyses.value.find(a => 
      a.moveNumber === moveInfo.moveNumber && a.side === moveInfo.side
    ) ?? null;
  });

  const lastMove = computed((): { from: string; to: string } | null => {
    if (currentMoveIndex.value < 0) return null;
    const moveInfo = gameHistory.value[currentMoveIndex.value];
    if (!moveInfo || !moveInfo.uci || moveInfo.uci.length < 4) return null;
    return {
      from: moveInfo.uci.substring(0, 2),
      to: moveInfo.uci.substring(2, 4)
    };
  });

  async function fetchGames() {
    loading.value = true;
    error.value = null;
    try {
      const response = await gamesApi.getAll();
      games.value = response.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to load games';
    } finally {
      loading.value = false;
    }
  }

  async function fetchGame(id: string) {
    loading.value = true;
    error.value = null;
    try {
      const response = await gamesApi.getById(id);
      currentGame.value = response.data;
      loadGameFromPgn(response.data.pgn);
      if (response.data.analyses) {
        analyses.value = response.data.analyses;
      }
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to load game';
    } finally {
      loading.value = false;
    }
  }

  function loadGameFromPgn(pgn: string) {
    chess.loadPgn(pgn);
    generateMoveHistory();
    chess.reset();
    currentPosition.value = chess.fen();
    currentMoveIndex.value = -1;
  }

  function generateMoveHistory() {
    gameHistory.value = [];
    chess.reset();
    const history = chess.history({ verbose: true });
    let moveNumber = 1;
    let side: 'w' | 'b' = 'w';
    
    history.forEach((move) => {
      chess.move(move);
      gameHistory.value.push({
        san: move.san,
        uci: move.from + move.to + (move.promotion || ''),
        fen: chess.fen(),
        moveNumber: moveNumber,
        side: side
      });
      side = side === 'w' ? 'b' : 'w';
      if (side === 'w') moveNumber++;
    });
  }

  async function analyzeGame() {
    if (!currentGame.value) return;
    analyzing.value = true;
    error.value = null;
    try {
      const response = await analysisApi.analyzeGame(currentGame.value.id);
      analyses.value = response.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Analysis failed';
    } finally {
      analyzing.value = false;
    }
  }

  function goToMove(index: number) {
    if (index < -1) index = -1;
    if (index >= gameHistory.value.length) index = gameHistory.value.length - 1;
    
    currentMoveIndex.value = index;
    
    if (index < 0) {
      chess.reset();
    } else {
      chess.load(gameHistory.value[index].fen);
    }
    currentPosition.value = chess.fen();
  }

  function nextMove() {
    goToMove(currentMoveIndex.value + 1);
  }

  function previousMove() {
    goToMove(currentMoveIndex.value - 1);
  }

  function goToStart() {
    goToMove(-1);
  }

  function goToEnd() {
    goToMove(gameHistory.value.length - 1);
  }

  function clearCurrentGame() {
    currentGame.value = null;
    currentPosition.value = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';
    currentMoveIndex.value = -1;
    gameHistory.value = [];
    analyses.value = [];
    chess.reset();
  }

  return {
    games,
    currentGame,
    currentPosition,
    currentMoveIndex,
    gameHistory,
    analyses,
    loading,
    analyzing,
    error,
    currentAnalysis,
    lastMove,
    fetchGames,
    fetchGame,
    loadGameFromPgn,
    analyzeGame,
    goToMove,
    nextMove,
    previousMove,
    goToStart,
    goToEnd,
    clearCurrentGame
  };
});