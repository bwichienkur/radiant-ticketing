/**
 * Records EnhancementHub demo as a continuous browser session with distinct scenes.
 * Fixes: correct Razor routes, wait for content, in-page caption overlay, crossfade transitions.
 */
import { chromium } from 'playwright';
import { mkdir, writeFile, rm, copyFile } from 'node:fs/promises';
import { execSync } from 'node:child_process';
import path from 'node:path';

const BASE = 'http://localhost:5001';
const ARTIFACT_DIR = '/opt/cursor/artifacts/demo';
const WORKSPACE_OUT = '/workspace/artifacts/demo';
const REQUEST_ID = process.env.DEMO_REQUEST_ID || '279c38dc-8da4-400b-828f-711726210eb6';
const APP_ID = '33333333-3333-3333-3333-333333333333';

const SCENES = [
  {
    id: 'login',
    title: '1. Sign in',
    caption: 'Role-based access for submitters, approvers, and admins.',
    path: '/Account/Login',
    duration: 4000,
    setup: 'login-form',
  },
  {
    id: 'dashboard',
    title: '2. Dashboard',
    caption: 'Backlog health, action queue, and activity feed for operators.',
    path: '/',
    duration: 5000,
    wait: '.stat-card, .dashboard-stat, h1',
  },
  {
    id: 'submit',
    title: '3. Submit request',
    caption: 'Capture business intent, priority, and target application.',
    path: '/EnhancementRequests/Create',
    duration: 5000,
    wait: 'form, input[name], .form-label',
  },
  {
    id: 'requests',
    title: '4. Request triage',
    caption: 'Search, filter, and sort the enhancement backlog.',
    path: '/EnhancementRequests',
    duration: 4500,
    wait: 'table, .card-panel, h1',
  },
  {
    id: 'detail',
    title: '5. Request detail',
    caption: 'AI analysis, mission control metrics, and impact recommendations.',
    path: `/EnhancementRequests/Details/${REQUEST_ID}`,
    duration: 6000,
    wait: 'h1, .card-panel, #collaboration-panel',
  },
  {
    id: 'spa-detail',
    title: '6. React collaboration',
    caption: 'SignalR live comments and presence on the React SPA.',
    path: `/Spa/RequestDetail/${REQUEST_ID}`,
    duration: 6000,
    wait: '#spa-request-detail-root h1, #spa-request-detail-root .card-panel',
    spa: true,
  },
  {
    id: 'system-map',
    title: '7. System Map',
    caption: 'Code and database artifacts linked in a knowledge graph.',
    path: `/SystemMap/Index?applicationId=${APP_ID}`,
    duration: 5000,
    wait: '.card-panel, h1, .system-map',
  },
  {
    id: 'spa-map',
    title: '8. Cytoscape graph',
    caption: 'Interactive zoom, pan, and type-colored nodes.',
    path: '/Spa/SystemMap',
    duration: 7000,
    wait: '#spa-system-map-root',
    spa: true,
    action: 'click-graph',
  },
  {
    id: 'drift',
    title: '9. Schema drift',
    caption: 'Compare live database schema against code mappings.',
    path: '/SchemaDrift',
    duration: 4500,
    wait: 'h1, .card-panel, table',
  },
  {
    id: 'approval',
    title: '10. Approval queue',
    caption: 'Human approve, reject, or clarify before export.',
    path: '/EnhancementRequests/Approve',
    duration: 5000,
    wait: '.approval-queue, .card-panel, h1',
  },
  {
    id: 'spa-approval',
    title: '11. React approvals',
    caption: 'Mobile-friendly queue with keyboard navigation.',
    path: '/Spa/ApprovalQueue',
    duration: 5000,
    wait: '#spa-approval-queue-root',
    spa: true,
  },
  {
    id: 'onboarding',
    title: '12. Onboarding wizard',
    caption: 'Register app, connect code, database, and run discovery.',
    path: '/Spa/OnboardingWizard',
    duration: 6000,
    wait: '#spa-onboarding-wizard-root',
    spa: true,
  },
  {
    id: 'applications',
    title: '13. Applications',
    caption: 'Team-scoped portfolio with repos and intelligence.',
    path: '/Applications',
    duration: 4500,
    wait: 'table, .card-panel, h1',
  },
  {
    id: 'databases',
    title: '14. Database connections',
    caption: 'Read-only schema registration for drift and ERD.',
    path: '/DatabaseConnections',
    duration: 4500,
    wait: 'table, .card-panel, h1',
  },
  {
    id: 'admin-jobs',
    title: '15. Background jobs',
    caption: 'Hangfire indexing, discovery, and AI analysis queues.',
    path: '/Admin/Jobs',
    duration: 4500,
    wait: '.stat-card, table, h1',
  },
  {
    id: 'admin-tenancy',
    title: '16. Tenancy & billing',
    caption: 'Plans, Stripe checkout, and schema-per-tenant isolation.',
    path: '/Admin/Tenancy',
    duration: 5000,
    wait: '.card-panel, h1, table',
  },
  {
    id: 'admin-compliance',
    title: '17. Compliance',
    caption: 'SOC 2 readiness, retention, and audit export.',
    path: '/Admin/Compliance',
    duration: 4500,
    wait: '.card-panel, h1, table',
  },
  {
    id: 'close',
    title: '18. End-to-end flow',
    caption: 'Intake → grounded AI → human approval → ticket export.',
    path: '/',
    duration: 5000,
    wait: '.stat-card, h1',
  },
];

const OVERLAY_CSS = `
#eh-demo-overlay {
  position: fixed; left: 0; right: 0; bottom: 0; z-index: 99999;
  background: linear-gradient(transparent, rgba(0,0,0,0.88));
  color: #fff; padding: 20px 32px 28px; pointer-events: none;
  font-family: system-ui, sans-serif; transition: opacity 0.4s ease;
}
#eh-demo-overlay .scene-num {
  font-size: 13px; text-transform: uppercase; letter-spacing: 0.08em;
  color: #93c5fd; margin-bottom: 4px;
}
#eh-demo-overlay .scene-title { font-size: 26px; font-weight: 700; margin-bottom: 6px; }
#eh-demo-overlay .scene-caption { font-size: 16px; color: #e2e8f0; max-width: 900px; line-height: 1.4; }
#eh-demo-scene-badge {
  position: fixed; top: 16px; right: 16px; z-index: 99999;
  background: #2563eb; color: #fff; padding: 8px 14px; border-radius: 8px;
  font: 600 14px system-ui; pointer-events: none;
}
`;

async function injectOverlay(page) {
  await page.evaluate((css) => {
    if (!document.getElementById('eh-demo-overlay-style')) {
      const style = document.createElement('style');
      style.id = 'eh-demo-overlay-style';
      style.textContent = css;
      document.head.appendChild(style);
    }
    if (!document.getElementById('eh-demo-overlay')) {
      const el = document.createElement('div');
      el.id = 'eh-demo-overlay';
      el.innerHTML =
        '<div class="scene-num"></div><div class="scene-title"></div><div class="scene-caption"></div>';
      document.body.appendChild(el);
    }
    if (!document.getElementById('eh-demo-scene-badge')) {
      const badge = document.createElement('div');
      badge.id = 'eh-demo-scene-badge';
      document.body.appendChild(badge);
    }
  }, OVERLAY_CSS);
}

async function showSceneOverlay(page, scene, index) {
  await page.evaluate(
    ({ title, caption, index, total }) => {
      const overlay = document.getElementById('eh-demo-overlay');
      const badge = document.getElementById('eh-demo-scene-badge');
      if (overlay) {
        overlay.style.opacity = '1';
        overlay.querySelector('.scene-num').textContent = `Scene ${index + 1} of ${total}`;
        overlay.querySelector('.scene-title').textContent = title.replace(/^\d+\.\s*/, '');
        overlay.querySelector('.scene-caption').textContent = caption;
      }
      if (badge) {
        badge.textContent = `${index + 1}/${total}`;
      }
    },
    { title: scene.title, caption: scene.caption, index, total: SCENES.length },
  );
}

async function login(page) {
  if (!page.url().includes('/Account/Login')) {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  }
  await page.waitForSelector('#Email', { timeout: 15000 });
  await page.fill('#Email', 'admin@enhancementhub.dev');
  await page.fill('#Password', 'password123');
  await Promise.all([
    page.waitForURL((url) => !url.pathname.includes('/Account/Login'), { timeout: 15000 }),
    page.click('button[type="submit"]'),
  ]);
  await page.waitForTimeout(800);
}

async function waitForScene(page, scene) {
  if (scene.setup === 'login-form') {
    await page.waitForSelector('#Email', { timeout: 10000 });
    return;
  }

  const response = await page.goto(`${BASE}${scene.path}`, {
    waitUntil: 'domcontentloaded',
    timeout: 45000,
  });

  if (response && response.status() >= 400) {
    throw new Error(`Scene "${scene.id}" returned HTTP ${response.status()} for ${scene.path}`);
  }

  const bodyText = await page.textContent('body');
  if (
    bodyText?.includes('DeveloperExceptionPage') ||
    bodyText?.includes('was not found') ||
    bodyText?.includes('An error occurred')
  ) {
    throw new Error(`Scene "${scene.id}" shows an error page at ${scene.path}`);
  }

  if (scene.wait) {
    const selectors = scene.wait.split(',').map((s) => s.trim());
    for (const sel of selectors) {
      try {
        await page.waitForSelector(sel, { timeout: 8000 });
        break;
      } catch {
        /* try next */
      }
    }
  }

  if (scene.spa) {
    await page.waitForTimeout(2000);
    await page.waitForFunction(
      () => {
        const roots = [
          document.querySelector('#spa-request-detail-root'),
          document.querySelector('#spa-system-map-root'),
          document.querySelector('#spa-approval-queue-root'),
          document.querySelector('#spa-onboarding-wizard-root'),
        ].filter(Boolean);
        return roots.some((r) => r && r.textContent && r.textContent.length > 80);
      },
      { timeout: 12000 },
    ).catch(() => undefined);
  }

  if (scene.action === 'click-graph') {
    const graphBtn = page.getByRole('button', { name: 'Graph' });
    if (await graphBtn.count()) {
      await graphBtn.first().click();
      await page.waitForTimeout(3000);
    }
  }

  await page.waitForTimeout(600);
}

async function main() {
  await rm(ARTIFACT_DIR, { recursive: true, force: true });
  await mkdir(ARTIFACT_DIR, { recursive: true });
  await mkdir(WORKSPACE_OUT, { recursive: true });

  const browser = await chromium.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  });

  const context = await browser.newContext({
    viewport: { width: 1280, height: 720 },
    recordVideo: { dir: ARTIFACT_DIR, size: { width: 1280, height: 720 } },
    colorScheme: 'dark',
  });

  const page = await context.newPage();

  // Prefer dark theme for a polished demo look
  await page.addInitScript(() => {
    localStorage.setItem('eh-theme', 'dark');
    document.documentElement.setAttribute('data-bs-theme', 'dark');
  });

  // Scene 1: login form (before auth)
  const loginScene = SCENES[0];
  console.log(`[1/${SCENES.length}] ${loginScene.title}`);
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('#Email', { timeout: 15000 });
  await injectOverlay(page);
  await showSceneOverlay(page, loginScene, 0);
  await page.waitForTimeout(loginScene.duration);
  await login(page);
  await injectOverlay(page);

  for (let i = 1; i < SCENES.length; i++) {
    const scene = SCENES[i];
    console.log(`[${i + 1}/${SCENES.length}] ${scene.title} → ${scene.path}`);

    // Brief fade between scenes
    await page.evaluate(() => {
      const o = document.getElementById('eh-demo-overlay');
      if (o) o.style.opacity = '0';
    });
    await page.waitForTimeout(300);

    await waitForScene(page, scene);
    await injectOverlay(page);
    await showSceneOverlay(page, scene, i);

    // Gentle scroll to show page has content
    await page.evaluate(() => window.scrollTo({ top: 120, behavior: 'smooth' }));
    await page.waitForTimeout(400);
    await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'smooth' }));

    await page.waitForTimeout(scene.duration);
  }

  const video = page.video();
  await context.close();
  await browser.close();

  const rawWebm = await video.path();
  const rawPath = path.join(ARTIFACT_DIR, 'recording.webm');
  const { rename } = await import('node:fs/promises');
  await rename(rawWebm, rawPath).catch(async () => {
    await copyFile(rawWebm, rawPath);
  });

  const finalPath = path.join(ARTIFACT_DIR, 'enhancementhub-demo.mp4');
  execSync(
    `ffmpeg -y -i "${rawPath}" -c:v libx264 -pix_fmt yuv420p -preset fast -crf 23 -movflags +faststart "${finalPath}"`,
    { stdio: 'inherit' },
  );

  await copyFile(finalPath, path.join(WORKSPACE_OUT, 'enhancementhub-demo.mp4'));

  const srtLines = [];
  let offset = 0;
  SCENES.forEach((scene, i) => {
    const start = formatSrtTime(offset);
    offset += scene.duration / 1000;
    const end = formatSrtTime(offset);
    srtLines.push(`${i + 1}`, `${start} --> ${end}`, scene.title, scene.caption, '');
  });
  await writeFile(path.join(ARTIFACT_DIR, 'narration.srt'), srtLines.join('\n'));
  await copyFile(path.join(ARTIFACT_DIR, 'narration.srt'), path.join(WORKSPACE_OUT, 'narration.srt'));

  await writeFile(
    path.join(ARTIFACT_DIR, 'demo-manifest.json'),
    JSON.stringify({ video: finalPath, scenes: SCENES.length, durationSec: offset }, null, 2),
  );

  console.log(`\n✓ Demo video: ${finalPath}`);
  console.log(`✓ Workspace copy: ${path.join(WORKSPACE_OUT, 'enhancementhub-demo.mp4')}`);
}

function formatSrtTime(seconds) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')},000`;
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
