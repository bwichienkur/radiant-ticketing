import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import path from 'path';

const baseURL = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:5001';
const outDir = process.env.UI_SCREENSHOT_DIR ?? '/opt/cursor/artifacts/screenshots';

async function dismissTour(page) {
  await page.evaluate(() => localStorage.setItem('eh-product-tour-seen', 'true'));
  const skip = page.locator('[data-tour-skip], button:has-text("Skip tour")');
  if (await skip.count()) {
    await skip.first().click({ timeout: 2000 }).catch(() => undefined);
  }
  const dismiss = page.locator('[data-tour-dismiss], button:has-text("Done")');
  if (await dismiss.count()) {
    await dismiss.first().click({ timeout: 2000 }).catch(() => undefined);
  }
  await page.waitForTimeout(300);
}

async function ensureLoggedIn(page) {
  await page.goto(`${baseURL}/`, { waitUntil: 'domcontentloaded' });
  if (!page.url().includes('/Account/Login')) {
    await dismissTour(page);
    return;
  }

  await page.getByLabel('Work email').fill('admin@enhancementhub.dev');
  await page.getByLabel('Password').fill('password123');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(/\/(Index)?$/, { timeout: 20000 });
  await dismissTour(page);
}

async function capture(page, name, url, options = {}) {
  const { spa = true } = options;
  await page.goto(`${baseURL}${url}`, { waitUntil: 'networkidle' });
  await dismissTour(page);
  if (spa) {
    await page.locator('#spa-root .eh-spa-root, #spa-root .page-header-title').first().waitFor({ state: 'visible', timeout: 30000 });
  } else {
    await page.locator('.page-header-title, h1').first().waitFor({ state: 'visible', timeout: 30000 });
  }
  await page.waitForTimeout(1200);
  const file = path.join(outDir, name);
  await page.screenshot({ path: file, fullPage: false });
  console.log(`saved ${file}`);
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await context.addInitScript(() => localStorage.setItem('eh-product-tour-seen', 'true'));
  const page = await context.newPage();

  await page.goto(`${baseURL}/Account/Login`, { waitUntil: 'networkidle' });
  await page.getByRole('button', { name: 'Sign in' }).waitFor();
  await page.screenshot({ path: path.join(outDir, '01-login.png') });
  console.log('saved login');

  await ensureLoggedIn(page);
  await capture(page, '02-dashboard.png', '/');
  await capture(page, '03-request-list.png', '/Spa/RequestList');
  await capture(page, '04-request-list-pending.png', '/Spa/RequestList?status=PendingApproval');
  await capture(page, '05-approval-queue.png', '/Spa/ApprovalQueue');
  await capture(page, '06-admin-settings.png', '/Admin/Settings', { spa: false });
  await capture(page, '07-admin-delivery.png', '/Admin/Delivery', { spa: false });

  await page.goto(`${baseURL}/Spa/RequestList`, { waitUntil: 'networkidle' });
  await dismissTour(page);
  await page.waitForTimeout(2000);
  const checkbox = page.locator('tbody .form-check-input').first();
  if (await checkbox.count()) {
    await checkbox.check();
    await page.waitForTimeout(600);
    await page.screenshot({ path: path.join(outDir, '08-bulk-toolbar.png'), fullPage: false });
    console.log('saved bulk toolbar');
  }

  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
