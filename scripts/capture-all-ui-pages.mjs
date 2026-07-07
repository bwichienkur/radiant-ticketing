import { chromium } from 'playwright';
import { mkdir, writeFile } from 'fs/promises';
import path from 'path';

const baseURL = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:5001';
const outDir = process.env.UI_SCREENSHOT_DIR ?? '/opt/cursor/artifacts/all-ui-pages';

const STATIC_PAGES = [
  { file: '01-login', url: '/Account/Login', auth: false, wait: 'button:has-text("Sign in")' },
  { file: '02-signup', url: '/Account/Signup', auth: false, wait: 'form, .page-header-title, h1' },
  { file: '03-pricing', url: '/Pricing', auth: false, wait: 'h1, .page-header-title' },
  { file: '04-dashboard', url: '/', auth: true, wait: '.eh-dashboard, .page-header-title' },
  { file: '05-request-list', url: '/Spa/RequestList', auth: true, wait: '.eh-request-list, .page-header-title' },
  { file: '06-create-request', url: '/Spa/CreateRequest', auth: true, wait: '.page-header-title' },
  { file: '07-approval-queue', url: '/Spa/ApprovalQueue', auth: true, wait: '.page-header-title' },
  { file: '08-onboarding-wizard', url: '/Spa/OnboardingWizard', auth: true, wait: '.page-header-title, .eh-onboarding' },
  { file: '09-applications', url: '/Spa/Applications', auth: true, wait: '.page-header-title' },
  { file: '10-system-map', url: '/Spa/SystemMap', auth: true, wait: '.page-header-title, #system-map-canvas, .empty-state' },
  { file: '11-repositories', url: '/Spa/Repositories', auth: true, wait: '.page-header-title' },
  { file: '12-database-connections', url: '/Spa/DatabaseConnections', auth: true, wait: '.page-header-title' },
  { file: '13-database-register', url: '/Spa/DatabaseConnections/Register', auth: true, wait: '.page-header-title, form' },
  { file: '14-schema-drift', url: '/Spa/SchemaDrift', auth: true, wait: '.page-header-title' },
  { file: '15-audit', url: '/Spa/Audit', auth: true, wait: '.page-header-title' },
  { file: '16-search', url: '/Spa/Search', auth: true, wait: '.page-header-title' },
  { file: '17-insights', url: '/Spa/Insights', auth: true, wait: '.page-header-title' },
  { file: '18-portfolio-health', url: '/Spa/PortfolioHealth', auth: true, wait: '.page-header-title' },
  { file: '19-portfolio-hub', url: '/Spa/Portfolio', auth: true, wait: '.page-header-title' },
  { file: '20-documentation-export', url: '/Spa/Documentation/Export', auth: true, wait: '.page-header-title' },
  { file: '21-refactor-analyze', url: '/Spa/Refactor/Analyze', auth: true, wait: '.page-header-title' },
  { file: '22-refactor-plans', url: '/Spa/Refactor/Plans', auth: true, wait: '.page-header-title' },
  { file: '23-notifications', url: '/Spa/Account/Notifications', auth: true, wait: '.page-header-title' },
  { file: '24-settings-general', url: '/Spa/Settings/General', auth: true, wait: '.eh-settings, .page-header-title' },
  { file: '25-settings-auth', url: '/Spa/Settings/Authentication', auth: true, wait: '.eh-settings' },
  { file: '26-settings-api-keys', url: '/Spa/Settings/ApiKeys', auth: true, wait: '.eh-settings' },
  { file: '27-settings-teams', url: '/Spa/Settings/Teams', auth: true, wait: '.eh-settings' },
  { file: '28-settings-webhooks', url: '/Spa/Settings/Webhooks', auth: true, wait: '.eh-settings' },
  { file: '29-settings-branding', url: '/Spa/Settings/Branding', auth: true, wait: '.eh-settings' },
  { file: '30-admin-jobs', url: '/Spa/Admin/Jobs', auth: true, wait: '.eh-admin, .page-header-title' },
  { file: '31-admin-compliance', url: '/Spa/Admin/Compliance', auth: true, wait: '.eh-admin' },
  { file: '32-admin-custom-fields', url: '/Spa/Admin/CustomFields', auth: true, wait: '.eh-admin' },
  { file: '33-admin-tenancy', url: '/Spa/Admin/Tenancy', auth: true, wait: '.eh-admin' },
  { file: '34-admin-observability', url: '/Spa/Admin/Observability', auth: true, wait: '.eh-admin' },
  { file: '35-admin-data-scaling', url: '/Spa/Admin/DataScaling', auth: true, wait: '.eh-admin' },
  { file: '36-admin-retention', url: '/Spa/Admin/Retention', auth: true, wait: '.eh-admin' },
  { file: '37-admin-delivery', url: '/Spa/Admin/Delivery', auth: true, wait: '.eh-admin' },
  { file: '38-admin-ai-prompts', url: '/Spa/Admin/AiPrompts', auth: true, wait: '.eh-admin' },
];

async function dismissOverlays(page) {
  await page.evaluate(() => {
    localStorage.setItem('eh-product-tour-seen', 'true');
    localStorage.setItem('eh-mock-ai-banner-dismissed', 'true');
    localStorage.setItem('eh-theme', 'Dark');
    document.documentElement.setAttribute('data-bs-theme', 'dark');
  });
  for (const label of ['Skip tour', 'Done', 'Dismiss', 'Close']) {
    const btn = page.getByRole('button', { name: new RegExp(label, 'i') });
    if (await btn.count()) {
      await btn.first().click({ timeout: 1200 }).catch(() => undefined);
    }
  }
  await page.waitForTimeout(300);
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

async function capture(page, file, url, waitSelector, { skipWait = false } = {}) {
  await page.goto(`${baseURL}${url}`, { waitUntil: 'networkidle', timeout: 45000 });
  await dismissOverlays(page);
  if (!skipWait && waitSelector) {
    await page.locator(waitSelector).first().waitFor({ state: 'visible', timeout: 25000 }).catch(() => undefined);
  }
  await page.waitForTimeout(1200);
  const outfile = path.join(outDir, `${file}.png`);
  await page.screenshot({ path: outfile, fullPage: false });
  console.log(`saved ${file}.png → ${url}`);
  return outfile;
}

async function discoverLinks(page, pattern) {
  return page.evaluate((reSource) => {
    const re = new RegExp(reSource);
    return [...document.querySelectorAll('a[href]')]
      .map((a) => a.getAttribute('href') ?? '')
      .filter((href) => re.test(href));
  }, pattern);
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await context.addInitScript(() => {
    localStorage.setItem('eh-product-tour-seen', 'true');
    localStorage.setItem('eh-mock-ai-banner-dismissed', 'true');
    localStorage.setItem('eh-theme', 'Dark');
  });
  const page = await context.newPage();
  const manifest = [];

  for (const entry of STATIC_PAGES) {
    try {
      if (entry.auth) {
        await ensureLoggedIn(page);
      }
      await capture(page, entry.file, entry.url, entry.wait);
      manifest.push({ file: `${entry.file}.png`, url: entry.url, status: 'ok' });
    } catch (err) {
      console.error(`FAILED ${entry.file}:`, err.message);
      manifest.push({ file: `${entry.file}.png`, url: entry.url, status: 'failed', error: err.message });
    }
  }

  await ensureLoggedIn(page);

  const dynamic = [];

  // Request detail
  await page.goto(`${baseURL}/Spa/RequestList`, { waitUntil: 'networkidle' });
  await dismissOverlays(page);
  const requestLinks = await discoverLinks(page, '^/Spa/RequestDetail/');
  if (requestLinks[0]) {
    dynamic.push({ file: '39-request-detail', url: requestLinks[0], wait: '.eh-request-detail, .page-header-title' });
  }

  // Application detail
  await page.goto(`${baseURL}/Spa/Applications`, { waitUntil: 'networkidle' });
  await dismissOverlays(page);
  const appLinks = await discoverLinks(page, '^/Spa/Applications/[0-9a-f-]{36}');
  if (appLinks[0]) {
    dynamic.push({ file: '40-application-detail', url: appLinks[0], wait: '.page-header-title' });
  }

  // Database connection detail + ERD
  await page.goto(`${baseURL}/Spa/DatabaseConnections`, { waitUntil: 'networkidle' });
  await dismissOverlays(page);
  const dbLinks = await discoverLinks(page, '^/Spa/DatabaseConnections/[0-9a-f-]{36}$');
  if (dbLinks[0]) {
    dynamic.push({ file: '41-database-detail', url: dbLinks[0], wait: '.page-header-title' });
    dynamic.push({ file: '42-database-erd', url: `${dbLinks[0]}/erd`, wait: '.page-header-title, canvas, .empty-state' });
  }

  // Onboarding session
  await page.goto(`${baseURL}/`, { waitUntil: 'networkidle' });
  await dismissOverlays(page);
  const onboardingLinks = await discoverLinks(page, '^/Spa/OnboardingWizard/');
  if (onboardingLinks[0]) {
    dynamic.push({ file: '43-onboarding-session', url: onboardingLinks[0], wait: '.page-header-title' });
  }

  for (const entry of dynamic) {
    try {
      await capture(page, entry.file, entry.url, entry.wait);
      manifest.push({ file: `${entry.file}.png`, url: entry.url, status: 'ok' });
    } catch (err) {
      console.error(`FAILED ${entry.file}:`, err.message);
      manifest.push({ file: `${entry.file}.png`, url: entry.url, status: 'failed', error: err.message });
    }
  }

  await writeFile(path.join(outDir, 'manifest.json'), JSON.stringify(manifest, null, 2));
  await browser.close();
  const ok = manifest.filter((m) => m.status === 'ok').length;
  console.log(`\nCaptured ${ok}/${manifest.length} pages → ${outDir}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
