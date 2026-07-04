import puppeteer from 'puppeteer';
import { mkdir } from 'node:fs/promises';

const outDir = '/opt/cursor/artifacts/screenshots';
await mkdir(outDir, { recursive: true });

const browser = await puppeteer.launch({
  headless: 'new',
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
});

const page = await browser.newPage();
await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 2 });

const shots = [
  { url: 'http://localhost:5001/Account/Login', file: '01-login.png', wait: 500 },
  { url: 'http://localhost:5001/', file: '02-dashboard.png', login: true, wait: 1500 },
  { url: 'http://localhost:5001/SystemMap?applicationId=33333333-3333-3333-3333-333333333333', file: '03-system-map.png', login: true, wait: 1500 },
  { url: 'http://localhost:5001/DatabaseConnections', file: '04-databases.png', login: true, wait: 1000 },
  { url: 'http://localhost:5001/SchemaDrift?connectionId=55555555-5555-5555-5555-555555555555', file: '05-drift.png', login: true, wait: 1000 },
];

async function ensureLoggedIn() {
  await page.goto('http://localhost:5001/Account/Login', { waitUntil: 'networkidle2' });
  if (page.url().includes('/Account/Login')) {
    await page.type('input[name="Email"]', 'admin@enhancementhub.dev');
    await page.type('input[name="Password"]', 'password123');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle2' }),
      page.click('button[type="submit"]'),
    ]);
  }
}

let loggedIn = false;

for (const shot of shots) {
  if (shot.login && !loggedIn) {
    await ensureLoggedIn();
    loggedIn = true;
  }

  if (!shot.login) {
    await page.goto(shot.url, { waitUntil: 'networkidle2' });
  } else if (shot.url !== 'http://localhost:5001/') {
    await page.goto(shot.url, { waitUntil: 'networkidle2' });
  } else if (loggedIn) {
    await page.goto(shot.url, { waitUntil: 'networkidle2' });
  }

  await new Promise((r) => setTimeout(r, shot.wait));
  const path = `${outDir}/${shot.file}`;
  await page.screenshot({ path, fullPage: false });
  console.log(`Saved ${path}`);
}

await browser.close();
