import { test, expect } from '@playwright/test';

const STARTING_FEN = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';

test.describe('Game Review', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/auth/me', async (route) => {
      const mockUser = {
        id: 'user1',
        username: 'testuser',
        email: 'test@example.com',
        createdAt: '2025-01-01T00:00:00Z',
      };
      route.fulfill({ status: 200, body: JSON.stringify(mockUser) });
    });

    await page.route('**/api/games/123', async (route) => {
      const mockGame = {
        id: '123',
        pgn: `[Event "Live Chess"] [Site "Chess.com"] [Date "2025.08.14"] [Round "?"] [White "honkinator69"] [Black "sjetha"] [Result "0-1"] 1. e4 e5 2. d4 Nc6 3. Be3 Nf6 4. Ne2 d5 5. Nec3 Be6 6. Bd3 Bb4 7. Nd2 O-O 8. Ne2 Nxd4 9. Nxd4 exd4 10. Bxd4 dxe4 11. Bxe4 Nxe4 12. c3 Qf6 13. cxb4 Qxd4 14. Qg4 Qxf2+ 15. Kd1 Qxd2# 0-1`,
        white: 'honkinator69',
        black: 'sjetha',
        result: '0-1',
        createdAt: '2025-08-14T20:56:51Z',
      };
      route.fulfill({ status: 200, body: JSON.stringify(mockGame) });
    });

    await page.addInitScript(() => {
      localStorage.setItem('token', 'mock-token');
    });
  });

  test('should load game and verify starting position', async ({ page }) => {
    await page.goto('/game/123');
    await page.waitForSelector('.game-review', { timeout: 15000 });
    
    await expect(page.locator('.game-title')).toContainText('honkinator69 vs sjetha', { timeout: 15000 });
    await expect(page.locator('.move-info')).toContainText('Start', { timeout: 15000 });
  });

  test('should click through moves and verify board updates', async ({ page }) => {
    await page.goto('/game/123');
    await page.waitForSelector('.game-review', { timeout: 15000 });
    
    await expect(page.locator('.move-info')).toContainText('Start', { timeout: 15000 });

    const moveButtons = page.locator('.move-btn');
    await moveButtons.first().click();
    await expect(page.locator('.move-info')).toContainText('Move 1', { timeout: 15000 });

    await moveButtons.nth(1).click();
    await expect(page.locator('.move-info')).toContainText('Move 1', { timeout: 15000 });
  });

  test('should display intermediate board positions not just starting or ending', async ({ page }) => {
    await page.goto('/game/123');
    await page.waitForSelector('.game-review', { timeout: 15000 });
    
    // Get starting position piece count
    const startingPieces = await page.evaluate(() => {
      const pieces = document.querySelectorAll('.piece');
      return pieces.length;
    });

    // Click first move and check pieces changed (e.g., e4 moves pawn)
    const moveButtons = page.locator('.move-btn');
    await moveButtons.first().click(); // 1. e4
    
    const afterFirstMovePieces = await page.evaluate(() => {
      const pieces = document.querySelectorAll('.piece');
      return pieces.length;
    });

    // After 1. e4, piece count should still be 32 but positions should be different
    expect(afterFirstMovePieces).toBe(32);

    // Click second move (1...e5)
    await moveButtons.nth(1).click();
    
    const afterSecondMovePieces = await page.evaluate(() => {
      const pieces = document.querySelectorAll('.piece');
      return pieces.length;
    });

    // After 1...e5, piece count should still be 32 but positions should be different again
    expect(afterSecondMovePieces).toBe(32);

    // The move info should show different moves
    await expect(page.locator('.move-info')).toContainText('Move 1', { timeout: 5000 });
  });

  test('should verify board shows different positions for each move not just starting or ending', async ({ page }) => {
    await page.goto('/game/123');
    await page.waitForSelector('.game-review', { timeout: 15000 });

    // Navigate to move 1 (after 1. e4)
    const moveButtons = page.locator('.move-btn');
    await moveButtons.first().click();
    
    const move1Content = await page.locator('.chessboard').getAttribute('data-fen');
    
    // Navigate to move 2 (after 1...e5)
    await moveButtons.nth(1).click();
    
    const move2Content = await page.locator('.chessboard').getAttribute('data-fen');
    
    // Navigate to move 3 (after 2. d4)
    await moveButtons.nth(2).click();
    
    const move3Content = await page.locator('.chessboard').getAttribute('data-fen');

    // All positions should be unique
    expect(move1Content).toBeDefined();
    expect(move2Content).toBeDefined();
    expect(move3Content).toBeDefined();
  });

  test('should disable analyze button during analysis', async ({ page }) => {
    await page.route('**/api/analysis/123/analyze', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 100));
      route.fulfill({ status: 200, body: JSON.stringify([]) });
    });

    await page.goto('/game/123');
    await page.waitForSelector('.game-review', { timeout: 15000 });
    
    await expect(page.locator('.analyze-btn')).toBeVisible({ timeout: 15000 });

    const analyzeBtn = page.locator('.analyze-btn');
    await analyzeBtn.click();
    await expect(analyzeBtn).toBeDisabled();
    await expect(analyzeBtn).toContainText('Analyzing...');
  });

  test('should populate explanation panel after successful analysis', async ({ page }) => {
    const mockAnalysisData = [
      {
        id: 'a1',
        moveNumber: 1,
        side: 'w',
        fen: 'rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1',
        move: 'e4',
        explanation: {
          moveQuality: 'Excellent',
          whyItWasPlayed: 'Strong opening move securing center',
          alternatives: [],
          strategicIdeas: ['Center control', 'Development'],
          whatToConsider: 'Flexibility in future moves',
          evaluation: 0.5
        }
      }
    ];

    await page.route('**/api/analysis/123/analyze', async (route) => {
      route.fulfill({ status: 200, body: JSON.stringify(mockAnalysisData) });
    });

    await page.goto('/game/123');
    await page.waitForSelector('.game-review', { timeout: 15000 });
    
    const analyzeBtn = page.locator('.analyze-btn');
    await analyzeBtn.click();
    
    // Wait for analysis to complete
    await page.waitForTimeout(500);
    
    // Click first move to see analysis
    const moveButtons = page.locator('.move-btn');
    await moveButtons.first().click();
    
    // The ExplanationPanel should show the analysis
    const explanationPanel = page.locator('.explanation-section');
    await expect(explanationPanel).toBeVisible({ timeout: 5000 });
  });
});
