<template>
  <div class="home">
    <div class="hero">
      <h1>Welcome to Chesster</h1>
      <p class="tagline">Learn chess through Chernev-style analysis</p>
      <div class="cta">
        <router-link to="/register" class="btn primary">Get Started</router-link>
        <router-link to="/login" class="btn secondary">Sign In</router-link>
      </div>
    </div>
    
    <div class="features">
      <div class="feature">
        <h3>🎯 Chernev-Style Analysis</h3>
        <p>Understand WHY moves are good or bad, not just what was played</p>
      </div>
      <div class="feature">
        <h3>♟️ Stockfish Integration</h3>
        <p>Professional engine evaluation with detailed explanations</p>
      </div>
      <div class="feature">
        <h3>📊 Game Import</h3>
        <p>Import PGN files from Chess.com, Lichess, and other platforms</p>
      </div>
      <div class="feature">
        <h3>🔐 Secure Login</h3>
        <p>Google, GitHub, or email/password authentication</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.home {
  max-width: 800px;
  margin: 0 auto;
  padding: 40px 20px;
}

.hero {
  text-align: center;
  margin-bottom: 60px;
}

.hero h1 {
  font-size: 48px;
  margin-bottom: 16px;
  color: #333;
}

.tagline {
  font-size: 20px;
  color: #666;
  margin-bottom: 32px;
}

.cta {
  display: flex;
  gap: 16px;
  justify-content: center;
}

.btn {
  padding: 12px 32px;
  border-radius: 8px;
  text-decoration: none;
  font-weight: bold;
  transition: all 0.2s;
}

.btn.primary {
  background: #4CAF50;
  color: #fff;
}

.btn.primary:hover {
  background: #45a049;
}

.btn.secondary {
  background: #fff;
  color: #333;
  border: 2px solid #ddd;
}

.btn.secondary:hover {
  border-color: #4CAF50;
  color: #4CAF50;
}

.features {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 24px;
}

.feature {
  background: #f5f5f5;
  padding: 24px;
  border-radius: 8px;
  text-align: center;
}

.feature h3 {
  margin-top: 0;
  font-size: 18px;
}

.feature p {
  color: #666;
  margin: 0;
}
</style>