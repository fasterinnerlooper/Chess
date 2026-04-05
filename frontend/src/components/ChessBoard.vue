<template>
  <div class="chessboard" :class="{ 'flipped': orientation === 'black' }">
    <div class="board">
      <div 
        v-for="(row, rowIndex) in board" 
        :key="rowIndex" 
        class="row"
      >
        <div 
          v-for="(square, colIndex) in row" 
          :key="colIndex"
          class="square"
          :class="getSquareClass(rowIndex, colIndex)"
          @click="onSquareClick(rowIndex, colIndex)"
        >
          <span v-if="square" class="piece" :class="getPieceClass(square)">
            {{ getPieceSymbol(square) }}
          </span>
          <span v-if="showCoordinates" class="coordinate">
            {{ getCoordinate(rowIndex, colIndex) }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { Chess } from 'chess.js';

interface Props {
  fen?: string;
  lastMove?: { from: string; to: string } | null;
  orientation?: 'white' | 'black';
  interactive?: boolean;
  showCoordinates?: boolean;
  highlightLastMove?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  fen: 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1',
  lastMove: null,
  orientation: 'white',
  interactive: false,
  showCoordinates: true,
  highlightLastMove: true
});

const emit = defineEmits<{
  move: [move: string];
  clickSquare: [square: string];
}>();

const chess = ref(new Chess(props.fen));

const board = computed(() => {
  const boardArray: (string | null)[][] = Array(8).fill(null).map(() => Array(8).fill(null));
  
  const board = chess.value.board();
  for (let row = 0; row < 8; row++) {
    for (let col = 0; col < 8; col++) {
      const piece = board[row][col];
      if (piece) {
        const color = piece.color === 'w' ? 'w' : 'b';
        const type = piece.type;
        boardArray[row][col] = `${color}${type}`;
      }
    }
  }
  
  return boardArray;
});

function getSquareClass(row: number, col: number): string {
  const isDark = (row + col) % 2 === 1;
  const squareName = getSquareName(row, col);
  
  let classes = isDark ? 'dark' : 'light';
  
  if (props.highlightLastMove && props.lastMove) {
    if (props.lastMove.from === squareName || props.lastMove.to === squareName) {
      classes += ' last-move';
    }
  }
  
  const position = chess.value.get(squareName as any);
  if (position) {
    if (chess.value.isCheck() && position.type === 'k') {
      classes += ' check';
    }
  }
  
  return classes;
}

function getPieceClass(piece: string): string {
  return piece[0] === 'w' ? 'white-piece' : 'black-piece';
}

function getPieceSymbol(piece: string): string {
  const symbols: Record<string, string> = {
    'wk': '♔', 'wq': '♕', 'wr': '♖', 'wb': '♗', 'wn': '♘', 'wp': '♙',
    'bk': '♚', 'bq': '♛', 'br': '♜', 'bb': '♝', 'bn': '♞', 'bp': '♟'
  };
  return symbols[piece] || '';
}

function getSquareName(row: number, col: number): string {
  const files = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];
  const ranks = props.orientation === 'white' ? ['8', '7', '6', '5', '4', '3', '2', '1'] : ['1', '2', '3', '4', '5', '6', '7', '8'];
  return files[col] + ranks[row];
}

function getCoordinate(_row: number, _col: number): string {
  return '';
}

function onSquareClick(row: number, col: number) {
  const squareName = getSquareName(row, col);
  emit('clickSquare', squareName);
}

watch(() => props.fen, (newFen) => {
  chess.value.load(newFen);
});

onMounted(() => {
  // Component is ready, lastMove will come from props
});
</script>

<style scoped>
.chessboard {
  display: inline-block;
  user-select: none;
}

.board {
  display: flex;
  flex-direction: column;
  border: 3px solid #333;
}

.row {
  display: flex;
}

.square {
  width: 60px;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  cursor: pointer;
}

.square.light {
  background-color: #f0d9b5;
}

.square.dark {
  background-color: #b58863;
}

.square.last-move {
  background-color: #ffff99 !important;
}

.square.check {
  background-color: #ff6b6b !important;
}

.piece {
  font-size: 48px;
  line-height: 1;
  cursor: grab;
}

.white-piece {
  color: #fff;
  text-shadow: 1px 1px 2px rgba(0,0,0,0.5);
}

.black-piece {
  color: #000;
  text-shadow: 1px 1px 2px rgba(255,255,255,0.3);
}

.coordinate {
  position: absolute;
  font-size: 10px;
  color: rgba(0,0,0,0.5);
  bottom: 2px;
  right: 2px;
}

.chessboard.flipped {
  transform: rotate(180deg);
}

.chessboard.flipped .piece {
  transform: rotate(180deg);
}
</style>