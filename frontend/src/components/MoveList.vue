<template>
  <div class="move-list">
    <div class="move-header">
      <button @click="goToStart" :disabled="currentIndex < 0" class="nav-btn">⟪</button>
      <button @click="previousMove" :disabled="currentIndex < 0" class="nav-btn">◀</button>
      <span class="move-info">{{ moveInfo }}</span>
      <button @click="nextMove" :disabled="currentIndex >= moves.length - 1" class="nav-btn">▶</button>
      <button @click="goToEnd" :disabled="currentIndex >= moves.length - 1" class="nav-btn">⟫</button>
    </div>
    <div class="moves-container">
      <div 
        v-for="(moveGroup, index) in groupedMoves" 
        :key="index"
        class="move-group"
      >
        <span class="move-number">{{ index + 1 }}.</span>
        <button 
          v-for="(move, mIndex) in moveGroup" 
          :key="mIndex"
          class="move-btn"
          :class="{ 
            active: getMoveGlobalIndex(index, mIndex) === currentIndex,
            black: move.side === 'b'
          }"
          @click="selectMove(getMoveGlobalIndex(index, mIndex))"
        >
          {{ move.san }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { MoveInfo } from '@/types/chess';

interface Props {
  moves: MoveInfo[];
  currentIndex: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  select: [index: number];
}>();

const groupedMoves = computed(() => {
  const groups: { san: string; side: 'w' | 'b' }[][] = [];
  let currentGroup: { san: string; side: 'w' | 'b' }[] = [];
  
  props.moves.forEach((move) => {
    if (move.side === 'w') {
      if (currentGroup.length > 0) {
        groups.push(currentGroup);
      }
      currentGroup = [{ san: move.san, side: move.side }];
    } else {
      currentGroup.push({ san: move.san, side: move.side });
    }
  });
  
  if (currentGroup.length > 0) {
    groups.push(currentGroup);
  }
  
  return groups;
});

const moveInfo = computed(() => {
  if (props.currentIndex < 0) return 'Start';
  if (props.currentIndex >= props.moves.length) return 'End';
  
  const move = props.moves[props.currentIndex];
  return `Move ${move.moveNumber}.${move.side === 'w' ? 'White' : 'Black'}`;
});

function getMoveGlobalIndex(groupIndex: number, moveIndex: number): number {
  let count = groupIndex * 2;
  if (moveIndex === 0) {
    return count;
  }
  return count + 1;
}

function selectMove(index: number) {
  emit('select', index);
}

function goToStart() {
  emit('select', -1);
}

function previousMove() {
  if (props.currentIndex > 0) {
    emit('select', props.currentIndex - 1);
  }
}

function nextMove() {
  if (props.currentIndex < props.moves.length - 1) {
    emit('select', props.currentIndex + 1);
  }
}

function goToEnd() {
  emit('select', props.moves.length - 1);
}
</script>

<style scoped>
.move-list {
  background: #f5f5f5;
  border-radius: 8px;
  padding: 10px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.move-header {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 8px;
  border-bottom: 1px solid #ddd;
}

.nav-btn {
  background: #fff;
  border: 1px solid #ccc;
  border-radius: 4px;
  padding: 4px 8px;
  cursor: pointer;
  font-size: 16px;
}

.nav-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.nav-btn:hover:not(:disabled) {
  background: #e0e0e0;
}

.move-info {
  font-size: 14px;
  color: #666;
  min-width: 100px;
  text-align: center;
}

.moves-container {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.move-group {
  display: flex;
  gap: 4px;
  margin-bottom: 4px;
  align-items: center;
}

.move-number {
  font-size: 12px;
  color: #888;
  min-width: 24px;
}

.move-btn {
  background: #fff;
  border: 1px solid #ccc;
  border-radius: 4px;
  padding: 4px 8px;
  font-size: 13px;
  cursor: pointer;
  min-width: 40px;
}

.move-btn:hover {
  background: #e0e0e0;
}

.move-btn.active {
  background: #4CAF50;
  color: #fff;
  border-color: #4CAF50;
}

.move-btn.black {
  margin-left: 0;
}
</style>