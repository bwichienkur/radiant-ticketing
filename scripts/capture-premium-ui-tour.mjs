import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import path from 'path';

const baseURL = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:5001';
const outDir = process.env.UI_SCREENSHOT_DIR ?? '/opt/cursor/artifacts/premium-ui-tour';

async function dismissOverlays(page) {
  await page.evaluate(() => {
    localStorage.setItem('eh-product-tour-seen', 'true');
    localStorage.setItem('eh-mock-ai-banner-dismissed', 'true');
  });
  for (const label of ['Skip tour', 'Done', 'Dismiss']) {
    const btn = page.getByRole('button', { name: new RegExp(label, 'i') });
    if (await btn.count()) {
      await btn.first().click({ timeout: 1500 }).catch(() => undefined);
    }
  }
  await page.waitForTimeout(400);
}

async function ensureLoggedIn(page) {
  await page.goto(`${baseURL}/`, { waitUntil: 'domcontentloaded' });
  if (!page.url().includes('/Account/Login')) {
    await dismissOverlays(page);
    return;
  }
  await page.getByLabel('Work email').fill('admin@enhancementhub.dev');
  await page.getByLabel('Password').fill('password123');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(/\/(Index)?(\?|$)/, { timeout: 30000 });
  await dismissOverlays(page);
}

async function capture(page, name, url, waitSelector = '#spa-root .page-header-title, #spa-root .eh-dashboard, .page-header-title') {
  await page.goto(`${baseURL}${url}`, { waitUntil: 'networkidle' });
  await dismissOverlays(page);
  await page.locator(waitSelector).first().waitFor({ state: 'visible', timeout: 30000 });
  await page.waitForTimeout(1500);
  const file = path.join(outDir, name);
  await page.screenshot({ path: file, fullPage: false });
  console.log(`saved ${file}`);
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await context.addInitScript(() => {
    localStorage.setItem('eh-product-tour-seen', 'true');
    localStorage.setItem('eh-mock-ai-banner-dismissed', 'true');
  });
  const page = await context.newPage();

  // Login (split hero + panel)
  await page.goto(`${baseURL}/Account/Login`, { waitUntil: 'networkidle' });
  await page.getByRole('button', { name: 'Sign in' }).waitFor();
  await page.screenshot({ path: path.join(outDir, '01-login.png') });
  console.log('saved 01-login.png');

  await ensureLoggedIn(page);

  // Dashboard with insight strip + omnibox
  await capture(page, '02-dashboard.png', '/');

  // Command palette open
  await page.goto(`${baseURL}/`, { waitUntil: 'networkidle' });
  await dismissOverlays(page);
  await page.keyboard.press('Control+k');
  await page.locator('.command-palette-modal').waitFor({ state: 'visible', timeout: 10000 });
  await page.waitForTimeout(600);
  await page.screenshot({ path: path.join(outDir, '03-command-palette.png') });
  console.log('saved 03-command-palette.png');
  await page.keyboard.press('Escape');

  // Workflows
  await capture(page, '04-request-list.png', '/Spa/RequestList');
  await capture(page, '05-create-request.png', '/Spa/CreateRequest');
  await capture(page, '06-approval-queue.png', '/Spa/ApprovalQueue');

  // Portfolio hub
  await capture(page, '07-portfolio.png', '/Spa/Portfolio');

  // Settings + Admin
  await capture(page, '08-settings.png', '/Spa/Settings/General');
  await capture(page, '09-admin-jobs.png', '/Spa/Admin/Jobs');

  // Request detail (first link if any)
  await page.goto(`${baseURL}/Spa/RequestList`, { waitUntil: 'networkidle' });
  await dismissOverlays(page);
  const viewBtn = page.getByRole('link', { name: 'View' }).first();
  if (await viewBtn.count()) {
    await viewBtn.click();
    await page.locator('.eh-request-detail .page-header-title').waitFor({ state: 'visible', timeout: 20000 });
    await page.waitForTimeout(1200);
    await page.screenshot({ path: path.join(outDir, '10-request-detail.png') });
    console.log('saved 10-request-detail.png');
  }

  // Dark mode dashboard
  await page.evaluate(() => {
    localStorage.setItem('eh-theme', 'dark');
    document.documentElement.setAttribute('data-bs-theme', 'dark');
  });
  await capture(page, '11-dashboard-dark.png', '/');

  await browser.close();
  console.log(`\nTour complete → ${outDir}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
