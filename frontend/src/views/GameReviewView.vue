<template>
  <div class="game-review">
    <div class="game-toolbar">
      <button @click="goBack" class="back-btn">← Back</button>
      <div class="game-title" v-if="currentGame">
        {{ currentGame.white }} vs {{ currentGame.black }}
        <span class="result">{{ currentGame.result }}</span>
      </div>
      <button @click="analyzeGame" :disabled="analyzing" class="analyze-btn">
        {{ analyzing ? 'Analyzing...' : 'Analyze' }}
      </button>
    </div>
    
    <div v-if="loading" class="loading">Loading game...</div>
    
    <div v-if="gameStore.error" class="error-message">
      {{ gameStore.error }}
    </div>
    
    <div v-else class="game-container">
      <div class="board-section">
        <ChessBoard 
          :fen="currentPosition" 
          :last-move="currentLastMove"
          orientation="white"
          :highlight-last-move="true"
        />
      </div>
      
      <div class="sidebar">
        <div class="moves-section">
          <MoveList 
            :moves="gameHistory"
            :current-index="currentMoveIndex"
            @select="goToMove"
          />
        </div>
        
        <div class="explanation-section">
          <ExplanationPanel :analysis="currentAnalysis" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useGameStore } from '@/stores/game';
import ChessBoard from '@/components/ChessBoard.vue';
import MoveList from '@/components/MoveList.vue';
import ExplanationPanel from '@/components/ExplanationPanel.vue';

const route = useRoute();
const router = useRouter();
const gameStore = useGameStore();

const currentGame = computed(() => gameStore.currentGame);
const currentPosition = computed(() => gameStore.currentPosition);
const gameHistory = computed(() => gameStore.gameHistory);
const currentMoveIndex = computed(() => gameStore.currentMoveIndex);
const currentAnalysis = computed(() => gameStore.currentAnalysis);
const currentLastMove = computed(() => gameStore.lastMove);
const loading = computed(() => gameStore.loading);
const analyzing = computed(() => gameStore.analyzing);

onMounted(async () => {
  const gameId = route.params.id as string;
  await gameStore.fetchGame(gameId);
});

function goToMove(index: number) {
  gameStore.goToMove(index);
}

async function analyzeGame() {
  await gameStore.analyzeGame();
}

function goBack() {
  router.push('/dashboard');
}
</script>

<style scoped>
.game-review {
  height: 100vh;
  display: flex;
  flex-direction: column;
}

.game-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  background: #fff;
  border-bottom: 1px solid #ddd;
}

.back-btn {
  background: none;
  border: none;
  color: #4CAF50;
  cursor: pointer;
  font-size: 16px;
}

.game-title {
  font-size: 18px;
  font-weight: bold;
}

.result {
  margin-left: 8px;
  background: #ddd;
  padding: 2px 8px;
  border-radius: 4px;
}

.analyze-btn {
  background: #4CAF50;
  color: #fff;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
}

.analyze-btn:disabled {
  opacity: 0.6;
}

.loading {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.error-message {
  background: #ffcccc;
  border: 1px solid #ff6666;
  color: #cc0000;
  padding: 12px 16px;
  margin: 8px 16px;
  border-radius: 4px;
  font-size: 14px;
}

.game-container {
  flex: 1;
  display: flex;
  overflow: hidden;
}

.board-section {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: #f5f5f5;
}

.sidebar {
  width: 400px;
  display: flex;
  flex-direction: column;
  border-left: 1px solid #ddd;
  background: #fff;
}

.moves-section {
  height: 40%;
  overflow: hidden;
}

.explanation-section {
  flex: 1;
  padding: 16px;
  overflow-y: auto;
}

@media (max-width: 900px) {
  .game-container {
    flex-direction: column;
  }
  
  .sidebar {
    width: 100%;
    height: 50%;
  }
}
</style>