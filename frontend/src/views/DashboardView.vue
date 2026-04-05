<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <h1>My Games</h1>
      <button @click="showImporter = true" class="import-btn">Import Game</button>
    </header>
    
    <div v-if="loading" class="loading">Loading games...</div>
    
    <div v-else-if="error" class="error">{{ error }}</div>
    
    <div v-else-if="games.length === 0" class="empty">
      <p>No games yet. Import a PGN to get started!</p>
      <button @click="showImporter = true" class="import-btn">Import Your First Game</button>
    </div>
    
    <div v-else class="games-list">
      <div v-for="game in games" :key="game.id" class="game-card" @click="openGame(game.id)">
        <div class="game-header">
          <span class="players">{{ game.white }} vs {{ game.black }}</span>
          <span class="result">{{ game.result }}</span>
        </div>
        <div class="game-meta">
          <span v-if="game.event">{{ game.event }}</span>
          <span v-if="game.date">{{ formatDate(game.date) }}</span>
        </div>
      </div>
    </div>
    
    <GameImporter 
      v-if="showImporter" 
      @imported="handleImported"
      @close="showImporter = false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useGameStore } from '@/stores/game';
import GameImporter from '@/components/GameImporter.vue';

const router = useRouter();
const gameStore = useGameStore();
const loading = ref(true);
const error = ref('');
const games = ref<any[]>([]);
const showImporter = ref(false);

onMounted(async () => {
  try {
    await gameStore.fetchGames();
    games.value = gameStore.games;
  } catch (e: any) {
    error.value = e.message || 'Failed to load games';
  } finally {
    loading.value = false;
  }
});

function openGame(id: string) {
  router.push(`/game/${id}`);
}

function handleImported(id: string) {
  showImporter.value = false;
  router.push(`/game/${id}`);
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleDateString();
}
</script>

<style scoped>
.dashboard {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.dashboard-header h1 {
  margin: 0;
}

.import-btn {
  background: #4CAF50;
  color: #fff;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
}

.loading, .error, .empty {
  text-align: center;
  padding: 40px;
}

.error {
  color: #c62828;
}

.games-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.game-card {
  background: #f5f5f5;
  padding: 16px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.game-card:hover {
  background: #e8e8e8;
}

.game-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.players {
  font-weight: bold;
}

.result {
  background: #ddd;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 14px;
}

.game-meta {
  font-size: 14px;
  color: #666;
  display: flex;
  gap: 16px;
}
</style>