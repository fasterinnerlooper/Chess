<template>
  <div class="game-importer">
    <div class="import-area" 
      @dragover.prevent="isDragging = true"
      @dragleave="isDragging = false"
      @drop.prevent="handleDrop"
      :class="{ dragging: isDragging }"
    >
      <div v-if="!pgnText">
        <p>Drag and drop a PGN file here</p>
        <p class="or">or</p>
        <textarea 
          v-model="pgnInput" 
          placeholder="Paste PGN here..."
          @input="handleInput"
        ></textarea>
      </div>
      <div v-else class="preview">
        <h3>Game Preview</h3>
        <div class="game-info">
          <p><strong>White:</strong> {{ parsedGame?.White || 'Unknown' }}</p>
          <p><strong>Black:</strong> {{ parsedGame?.Black || 'Unknown' }}</p>
          <p><strong>Result:</strong> {{ parsedGame?.Result || '*' }}</p>
          <p><strong>Moves:</strong> {{ parsedGame?.Moves?.length || 0 }}</p>
        </div>
        <button @click="clearGame" class="clear-btn">Clear</button>
      </div>
    </div>
    
    <div class="actions" v-if="pgnText && parsedGame">
      <button @click="importGame" :disabled="importing" class="import-btn">
        {{ importing ? 'Importing...' : 'Import Game' }}
      </button>
    </div>
    
    <div v-if="error" class="error">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { gamesApi } from '@/services/api';

interface Props {
  parsedGame?: {
    White: string;
    Black: string;
    Result: string;
    Moves?: { length: number }[];
  };
}

defineProps<Props>();

interface ParsedGame {
  White: string;
  Black: string;
  Result: string;
  Moves: { length: number }[];
}

const emit = defineEmits<{
  imported: [gameId: string];
}>();

const router = useRouter();
const pgnInput = ref('');
const pgnText = ref('');
const isDragging = ref(false);
const importing = ref(false);
const error = ref('');
const parsedGame = ref<ParsedGame | null>(null);

function parsePgn(pgn: string): ParsedGame | null {
  try {
    const result: ParsedGame = {
      White: 'Unknown',
      Black: 'Unknown',
      Result: '*',
      Moves: []
    };
    
    const headerRegex = /\[(\w+)\s+"([^"]+)"\]/g;
    let match;
    while ((match = headerRegex.exec(pgn)) !== null) {
      const tag = match[1].toLowerCase();
      const value = match[2];
      if (tag === 'white') result.White = value;
      else if (tag === 'black') result.Black = value;
      else if (tag === 'result') result.Result = value;
    }
    
    const movesSection = pgn.replace(/\[.*?\]/g, '').trim();
    const moveMatches = movesSection.match(/\d+\.\s*\S+/g);
    if (moveMatches) {
      result.Moves = new Array(moveMatches.length);
    }
    
    return result;
  } catch {
    return null;
  }
}

function handleInput() {
  if (pgnInput.value.trim()) {
    pgnText.value = pgnInput.value;
    parsedGame.value = parsePgn(pgnInput.value);
  } else {
    pgnText.value = '';
    parsedGame.value = null;
  }
}

function handleDrop(e: DragEvent) {
  isDragging.value = false;
  const file = e.dataTransfer?.files[0];
  if (file && file.name.endsWith('.pgn')) {
    const reader = new FileReader();
    reader.onload = (event) => {
      pgnText.value = event.target?.result as string || '';
      pgnInput.value = pgnText.value;
    };
    reader.readAsText(file);
  }
}

function clearGame() {
  pgnText.value = '';
  pgnInput.value = '';
  error.value = '';
}

async function importGame() {
  if (!pgnText.value) return;
  
  importing.value = true;
  error.value = '';
  
  try {
    const response = await gamesApi.import(pgnText.value);
    emit('imported', response.data.id);
    router.push(`/game/${response.data.id}`);
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Failed to import game';
  } finally {
    importing.value = false;
  }
}
</script>

<style scoped>
.game-importer {
  max-width: 600px;
  margin: 0 auto;
}

.import-area {
  border: 2px dashed #ccc;
  border-radius: 8px;
  padding: 24px;
  text-align: center;
  transition: all 0.2s;
}

.import-area.dragging {
  border-color: #4CAF50;
  background: #f5f5f5;
}

.import-area textarea {
  width: 100%;
  min-height: 150px;
  margin-top: 16px;
  padding: 8px;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-family: monospace;
  resize: vertical;
}

.or {
  margin: 16px 0;
  color: #888;
}

.preview {
  text-align: left;
}

.preview h3 {
  margin-top: 0;
}

.game-info {
  background: #f5f5f5;
  padding: 12px;
  border-radius: 4px;
  margin: 12px 0;
}

.game-info p {
  margin: 4px 0;
}

.clear-btn {
  background: #f44336;
  color: #fff;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  cursor: pointer;
}

.actions {
  margin-top: 16px;
  text-align: center;
}

.import-btn {
  background: #4CAF50;
  color: #fff;
  border: none;
  padding: 12px 24px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 16px;
}

.import-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  margin-top: 16px;
  padding: 12px;
  background: #ffebee;
  color: #c62828;
  border-radius: 4px;
}
</style>