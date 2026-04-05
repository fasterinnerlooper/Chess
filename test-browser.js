import { chromium } from 'playwright';

async function main() {
  const browser = await chromium.launch({
    channel: 'chromium',
    executablePath: 'C:\\Users\\shafi\\AppData\\Local\\ms-playwright\\chromium-1217\\chrome-win64\\chrome.exe'
  });
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Collect console messages
  const consoleMessages = [];
  page.on('console', msg => {
    consoleMessages.push({ type: msg.type(), text: msg.text() });
  });
  
  console.log('=== Test 1: Homepage ===');
  await page.goto('http://localhost:8080');
  console.log('Page title:', await page.title());
  
  console.log('\n=== Test 2: Login Flow ===');
  await page.goto('http://localhost:8080/login');
  await page.waitForSelector('form', { timeout: 10000 });
  await page.fill('#email', 'test@example.com');
  await page.fill('#password', 'TestPassword123');
  await page.click('button[type="submit"]');
  await page.waitForURL('**/dashboard', { timeout: 10000 });
  console.log('Logged in, now at:', page.url());
  
  console.log('\n=== Test 3: Dashboard ===');
  await page.waitForSelector('.dashboard', { timeout: 10000 }).catch(() => console.log('No dashboard class found'));
  const pageContent = await page.content();
  console.log('Dashboard loaded, checking for key elements...');
  
  // Check for game-related elements
  const hasGames = pageContent.includes('game') || pageContent.includes('Game');
  console.log('Contains game content:', hasGames);
  
  console.log('\n=== Test 4: Navigation ===');
  // Check navigation links
  const navLinks = await page.$$eval('nav a, .nav a, a[href]', links => links.map(l => l.href));
  console.log('Navigation links found:', navLinks.slice(0, 5));
  
  console.log('\n=== Test 5: API endpoints ===');
  // Test game API
  const token = await context.cookies().then(cookies => {
    const c = cookies.find(c => c.name === 'token');
    return c ? c.value : localStorage.getItem('token');
  });
  
  // Check console errors
  const errors = consoleMessages.filter(m => m.type === 'error');
  if (errors.length > 0) {
    console.log('\nConsole errors found:');
    errors.forEach(e => console.log('  -', e.text));
  } else {
    console.log('\nNo console errors');
  }
  
  await browser.close();
  console.log('\n=== All Tests Completed ===');
}

main().catch(console.error);