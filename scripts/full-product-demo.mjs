#!/usr/bin/env node
/**
 * Captures screenshots across all major EnhancementHub routes for product demo.
 */
import { chromium } from 'playwright';
import { mkdir, writeFile } from 'fs/promises';
import path from 'path';

const baseURL = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:5099';
const outDir = process.env.DEMO_OUTPUT_DIR ?? '/opt/cursor/artifacts/demo';
const DEMO_REQUEST_ID = '279c38dc-8da4-400b-828f-711726210eb6';
const DEMO_APP_ID = '33333333-3333-3333-3333-333333333333';

const routes = [
  { name: '01-login', url: '/Account/Login', public: true },
  { name: '02-signup', url: '/Account/Signup', public: true },
  { name: '03-pricing', url: '/Pricing', public: true },
  { name: '04-dashboard', url: '/' },
  { name: '05-request-list', url: '/Spa/RequestList' },
  { name: '06-create-request', url: '/Spa/CreateRequest' },
  { name: '07-request-detail', url: `/Spa/RequestDetail/${DEMO_REQUEST_ID}` },
  { name: '08-approval-queue', url: '/Spa/ApprovalQueue' },
  { name: '09-onboarding', url: '/Spa/OnboardingWizard' },
  { name: '10-system-map', url: '/Spa/SystemMap' },
  { name: '11-applications', url: '/Spa/Applications' },
  { name: '12-application-detail', url: `/Spa/Applications/${DEMO_APP_ID}` },
  { name: '13-schema-drift', url: '/Spa/SchemaDrift' },
  { name: '14-repositories', url: '/Spa/Repositories' },
  { name: '15-audit', url: '/Spa/Audit' },
  { name: '16-search', url: '/Spa/Search' },
  { name: '17-database-connections', url: '/Spa/DatabaseConnections' },
  { name: '18-documentation-export', url: '/Spa/Documentation/Export' },
  { name: '19-refactor-analyze', url: '/Spa/Refactor/Analyze' },
  { name: '20-refactor-plans', url: '/Spa/Refactor/Plans' },
  { name: '21-insights', url: '/Spa/Insights' },
  { name: '22-portfolio-health', url: '/Spa/PortfolioHealth' },
  { name: '23-notifications', url: '/Spa/Account/Notifications' },
  { name: '24-settings-general', url: '/Spa/Settings/General' },
  { name: '25-settings-auth', url: '/Spa/Settings/Authentication' },
  { name: '26-settings-apikeys', url: '/Spa/Settings/ApiKeys' },
  { name: '27-settings-teams', url: '/Spa/Settings/Teams' },
  { name: '28-settings-webhooks', url: '/Spa/Settings/Webhooks' },
  { name: '29-settings-branding', url: '/Spa/Settings/Branding' },
  { name: '30-admin-jobs', url: '/Spa/Admin/Jobs' },
  { name: '31-admin-compliance', url: '/Spa/Admin/Compliance' },
  { name: '32-admin-customfields', url: '/Spa/Admin/CustomFields' },
  { name: '33-admin-tenancy', url: '/Spa/Admin/Tenancy' },
  { name: '34-admin-observability', url: '/Spa/Admin/Observability' },
  { name: '35-admin-datascaling', url: '/Spa/Admin/DataScaling' },
  { name: '36-admin-retention', url: '/Spa/Admin/Retention' },
  { name: '37-admin-delivery', url: '/Spa/Admin/Delivery' },
  { name: '38-admin-aiprompts', url: '/Spa/Admin/AiPrompts' },
];

async function login(page) {
  await page.goto(`${baseURL}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.getByLabel('Work email').fill('admin@enhancementhub.dev');
  await page.getByLabel('Password').fill('password123');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(/\/(Index)?$/, { timeout: 30000 });
  await page.locator('#spa-root').waitFor({ state: 'visible', timeout: 20000 });
}

async function captureRoute(page, route) {
  await page.goto(`${baseURL}${route.url}`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1500);
  if (!route.public) {
    const spa = page.locator('#spa-root');
    if (await spa.count()) {
      await spa.waitFor({ state: 'visible', timeout: 20000 }).catch(() => undefined);
    } else {
      await page.locator('h1, h2, .page-header-title, .login-hero-title').first()
        .waitFor({ state: 'visible', timeout: 15000 }).catch(() => undefined);
    }
  }
  const title = await page.title();
  const heading = await page.locator('h1, .page-header-title, .login-hero-title').first()
    .textContent().catch(() => null);
  const file = path.join(outDir, `${route.name}.png`);
  await page.screenshot({ path: file, fullPage: false });
  return { route: route.name, url: route.url, title, heading: heading?.trim() ?? null, ok: true };
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await context.addInitScript(() => localStorage.setItem('eh-product-tour-seen', 'true'));
  const page = await context.newPage();
  const results = [];

  for (const route of routes.filter((r) => r.public)) {
    results.push(await captureRoute(page, route));
  }

  await login(page);

  for (const route of routes.filter((r) => !r.public)) {
    results.push(await captureRoute(page, route));
  }

  // Command palette
  await page.keyboard.press('Control+k');
  await page.waitForTimeout(800);
  await page.screenshot({ path: path.join(outDir, '39-command-palette.png') });
  results.push({ route: '39-command-palette', url: '⌘K', title: 'Command palette', heading: 'Command palette', ok: true });
  await page.keyboard.press('Escape');

  await browser.close();
  await writeFile(path.join(outDir, 'demo-manifest.json'), JSON.stringify(results, null, 2));
  console.log(JSON.stringify({ captured: results.length, outDir, results }, null, 2));
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
