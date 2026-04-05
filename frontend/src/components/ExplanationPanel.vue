<template>
  <div class="explanation-panel" v-if="analysis">
    <div class="quality-badge" :class="analysis.explanation.moveQuality">
      {{ analysis.explanation.moveQuality }}
    </div>
    
    <div class="section">
      <h3>Why this move?</h3>
      <p>{{ analysis.explanation.whyItWasPlayed }}</p>
    </div>
    
    <div class="section" v-if="analysis.explanation.strategicIdeas.length">
      <h3>Strategic Ideas</h3>
      <div class="tags">
        <span 
          v-for="(idea, index) in analysis.explanation.strategicIdeas" 
          :key="index"
          class="tag"
        >
          {{ idea }}
        </span>
      </div>
    </div>
    
    <div class="section" v-if="analysis.explanation.alternatives.length">
      <h3>Alternatives to consider</h3>
      <div class="alternatives">
        <div 
          v-for="(alt, index) in analysis.explanation.alternatives" 
          :key="index"
          class="alternative"
        >
          <span class="alt-move">{{ alt.move }}</span>
          <span class="alt-reason">{{ alt.reason }}</span>
          <span class="alt-eval" :class="getEvalClass(alt.evaluation)">
            {{ formatEval(alt.evaluation) }}
          </span>
        </div>
      </div>
    </div>
    
    <div class="section" v-if="analysis.explanation.whatToConsider">
      <h3>What to consider</h3>
      <p class="consideration">{{ analysis.explanation.whatToConsider }}</p>
    </div>
    
    <div class="section evaluation" v-if="analysis.explanation.evaluation !== undefined">
      <span class="eval-label">Evaluation:</span>
      <span class="eval-value" :class="getEvalClass(analysis.explanation.evaluation)">
        {{ formatEval(analysis.explanation.evaluation) }}
      </span>
    </div>
  </div>
  
  <div class="explanation-panel empty" v-else>
    <p>Select a move to see the analysis</p>
  </div>
</template>

<script setup lang="ts">
import type { Analysis } from '@/types/chess';

interface Props {
  analysis: Analysis | null;
}

defineProps<Props>();

function formatEval(evaluation: number | undefined): string {
  if (evaluation === undefined) return 'N/A';
  if (evaluation > 0) return `+${Math.round(evaluation / 10)}`;
  if (evaluation < 0) return `${Math.round(evaluation / 10)}`;
  return '0';
}

function getEvalClass(evaluation: number): string {
  if (evaluation > 30) return 'good';
  if (evaluation < -30) return 'bad';
  return 'neutral';
}
</script>

<style scoped>
.explanation-panel {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.explanation-panel.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 200px;
  color: #888;
}

.quality-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 14px;
  font-weight: bold;
  margin-bottom: 16px;
  text-transform: capitalize;
}

.quality-badge.excellent { background: #4CAF50; color: #fff; }
.quality-badge.good { background: #8BC34A; color: #fff; }
.quality-badge.inaccuracy { background: #FFC107; color: #333; }
.quality-badge.mistake { background: #FF9800; color: #fff; }
.quality-badge.blunder { background: #f44336; color: #fff; }
.quality-badge.unknown { background: #9E9E9E; color: #fff; }

.section {
  margin-bottom: 16px;
}

.section h3 {
  font-size: 14px;
  color: #666;
  margin: 0 0 8px 0;
}

.section p {
  margin: 0;
  line-height: 1.5;
}

.tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.tag {
  background: #E3F2FD;
  color: #1976D2;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
}

.alternatives {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.alternative {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px;
  background: #f5f5f5;
  border-radius: 4px;
}

.alt-move {
  font-weight: bold;
  min-width: 40px;
}

.alt-reason {
  flex: 1;
  font-size: 13px;
  color: #666;
}

.alt-eval {
  font-size: 12px;
  padding: 2px 6px;
  border-radius: 4px;
}

.alt-eval.good { background: #C8E6C9; color: #2E7D32; }
.alt-eval.bad { background: #FFCDD2; color: #C62828; }
.alt-eval.neutral { background: #E0E0E0; color: #666; }

.consideration {
  font-style: italic;
  color: #555;
}

.evaluation {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px;
  background: #f5f5f5;
  border-radius: 4px;
}

.eval-label {
  font-weight: bold;
}

.eval-value {
  font-size: 18px;
  font-weight: bold;
}

.eval-value.good { color: #2E7D32; }
.eval-value.bad { color: #C62828; }
.eval-value.neutral { color: #666; }
</style>