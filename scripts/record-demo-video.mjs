/**
 * Records EnhancementHub product demo video with narrated captions per use case.
 * Output: /opt/cursor/artifacts/demo/enhancementhub-demo.mp4
 */
import { chromium } from 'playwright';
import { mkdir, writeFile, rm } from 'node:fs/promises';
import { execSync } from 'node:child_process';
import path from 'node:path';

const BASE = 'http://localhost:5001';
const ARTIFACT_DIR = '/opt/cursor/artifacts/demo';
const SEGMENTS_DIR = path.join(ARTIFACT_DIR, 'segments');
const REQUEST_ID = process.env.DEMO_REQUEST_ID || '279c38dc-8da4-400b-828f-711726210eb6';
const APP_ID = '33333333-3333-3333-3333-333333333333';

const SCENES = [
  {
    id: 'login',
    title: 'Sign in',
    caption: 'Role-based access for submitters, approvers, and admins. SSO via Entra ID supported.',
    url: `${BASE}/Account/Login`,
    setup: 'show-login',
    duration: 4,
  },
  {
    id: 'dashboard',
    title: 'Dashboard control room',
    caption:
      'Backlog health at a glance: pending analysis, high-risk approvals, sparklines, and the action queue for your role.',
    url: `${BASE}/`,
    duration: 5,
  },
  {
    id: 'submit',
    title: 'Submit enhancement request',
    caption:
      'Capture business intent, priority, and target application — the foundation for grounded AI impact analysis.',
    url: `${BASE}/EnhancementRequests/Create`,
    duration: 5,
  },
  {
    id: 'requests',
    title: 'Request triage',
    caption: 'Search, filter, and sort the enhancement backlog. Risk badges highlight what needs attention first.',
    url: `${BASE}/EnhancementRequests`,
    duration: 4,
  },
  {
    id: 'detail',
    title: 'Request detail & AI analysis',
    caption:
      'Mission control shows risk, confidence, affected apps/repos, and DB recommendations from your indexed estate.',
    url: `${BASE}/EnhancementRequests/Details?id=${REQUEST_ID}`,
    duration: 6,
  },
  {
    id: 'spa-detail',
    title: 'React detail + real-time collaboration',
    caption: 'SignalR powers live comments, viewer presence, and analysis refresh on the React SPA hot path.',
    url: `${BASE}/Spa/RequestDetail/${REQUEST_ID}`,
    duration: 6,
  },
  {
    id: 'system-map',
    title: 'System Map',
    caption: 'Graph links controllers, entities, services, and tables — architects see blast radius before approving.',
    url: `${BASE}/SystemMap/Index?applicationId=${APP_ID}`,
    duration: 5,
  },
  {
    id: 'spa-map',
    title: 'Interactive Cytoscape graph',
    caption: 'Zoom, pan, and select nodes by type. Graph/list toggle with a 400-node performance cap.',
    url: `${BASE}/Spa/SystemMap`,
    duration: 6,
  },
  {
    id: 'drift',
    title: 'Schema drift detection',
    caption: 'Scheduled scans compare live database schema against code mappings to catch mismatches early.',
    url: `${BASE}/SchemaDrift`,
    duration: 4,
  },
  {
    id: 'approval',
    title: 'Approval queue',
    caption: 'Human gate: approve, reject, or request clarification. Audit log records every decision.',
    url: `${BASE}/EnhancementRequests/Approve`,
    duration: 5,
  },
  {
    id: 'spa-approval',
    title: 'React approval queue',
    caption: 'Mobile-friendly queue with J/K keyboard navigation and quick decision actions.',
    url: `${BASE}/Spa/ApprovalQueue`,
    duration: 5,
  },
  {
    id: 'onboarding',
    title: 'Onboarding wizard',
    caption: 'Register apps via local path, ZIP, GitHub App, or Git — then connect DB and run discovery.',
    url: `${BASE}/Spa/OnboardingWizard`,
    duration: 6,
  },
  {
    id: 'applications',
    title: 'Application portfolio',
    caption: 'Team-scoped applications with linked repositories, DB connections, and intelligence profiles.',
    url: `${BASE}/Applications`,
    duration: 4,
  },
  {
    id: 'databases',
    title: 'Database connections',
    caption: 'Register read-only connections for schema scan, ERD export, and drift detection.',
    url: `${BASE}/DatabaseConnections`,
    duration: 4,
  },
  {
    id: 'admin-jobs',
    title: 'Background jobs',
    caption: 'Hangfire orchestrates indexing, discovery, and AI analysis with retry and admin visibility.',
    url: `${BASE}/Admin/Jobs`,
    duration: 4,
  },
  {
    id: 'admin-tenancy',
    title: 'Tenancy & billing',
    caption: 'Multi-tenant plans, Stripe checkout, trial enforcement, and schema-per-tenant isolation.',
    url: `${BASE}/Admin/Tenancy`,
    duration: 5,
  },
  {
    id: 'admin-compliance',
    title: 'Compliance & governance',
    caption: 'SOC 2 readiness, retention policies, audit export, and security whitepaper for procurement.',
    url: `${BASE}/Admin/Compliance`,
    duration: 4,
  },
  {
    id: 'close',
    title: 'Intake → analysis → approval → export',
    caption:
      'EnhancementHub deploys in your environment. Business request to Jira-ready ticket — with full audit trail.',
    url: `${BASE}/`,
    duration: 5,
  },
];

function esc(text) {
  return text.replace(/\\/g, '\\\\').replace(/:/g, '\\:').replace(/'/g, "\\'").replace(/\n/g, ' ');
}

async function login(page) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'admin@enhancementhub.dev');
  await page.fill('input[name="Password"]', 'password123');
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }), page.click('button[type="submit"]')]);
}

async function prepareScene(page, scene) {
  if (scene.setup === 'show-login') {
    await page.goto(scene.url, { waitUntil: 'networkidle' });
    return;
  }

  await page.goto(scene.url, { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForTimeout(1500);

  if (scene.id === 'spa-map') {
    const graphBtn = page.getByRole('button', { name: 'Graph' });
    if (await graphBtn.count()) {
      await graphBtn.first().click();
      await page.waitForTimeout(2500);
    }
  }
}

function renderSegment(imagePath, segmentPath, scene) {
  const title = esc(scene.title);
  const caption = esc(scene.caption);
  const vf = [
    'scale=1280:720:force_original_aspect_ratio=decrease',
    'pad=1280:720:(ow-iw)/2:(oh-ih)/2:color=0x0f172a',
    `drawbox=x=0:y=ih-140:w=iw:h=140:color=black@0.75:t=fill`,
    `drawtext=text='${title}':fontsize=28:fontcolor=white:x=40:y=h-120`,
    `drawtext=text='${caption}':fontsize=18:fontcolor=0xE2E8F0:x=40:y=h-75:line_spacing=4`,
  ].join(',');

  execSync(
    `ffmpeg -y -loop 1 -i "${imagePath}" -t ${scene.duration} -vf "${vf}" -c:v libx264 -pix_fmt yuv420p -r 30 "${segmentPath}"`,
    { stdio: 'pipe' },
  );
}

async function main() {
  await rm(ARTIFACT_DIR, { recursive: true, force: true });
  await mkdir(SEGMENTS_DIR, { recursive: true });

  const browser = await chromium.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  await login(page);

  const segmentPaths = [];

  for (let i = 0; i < SCENES.length; i++) {
    const scene = SCENES[i];
    console.log(`[${i + 1}/${SCENES.length}] ${scene.title}`);
    await prepareScene(page, scene);

    const imagePath = path.join(SEGMENTS_DIR, `${String(i).padStart(2, '0')}-${scene.id}.png`);
    await page.screenshot({ path: imagePath });

    const segmentPath = path.join(SEGMENTS_DIR, `${String(i).padStart(2, '0')}-${scene.id}.mp4`);
    renderSegment(imagePath, segmentPath, scene);
    segmentPaths.push(segmentPath);
  }

  await browser.close();

  const listPath = path.join(ARTIFACT_DIR, 'concat.txt');
  await writeFile(listPath, segmentPaths.map((p) => `file '${p}'`).join('\n'));

  const finalPath = path.join(ARTIFACT_DIR, 'enhancementhub-demo.mp4');
  execSync(`ffmpeg -y -f concat -safe 0 -i "${listPath}" -c copy "${finalPath}"`, { stdio: 'inherit' });

  const srtLines = [];
  let offset = 0;
  SCENES.forEach((scene, i) => {
    const start = formatSrtTime(offset);
    offset += scene.duration;
    const end = formatSrtTime(offset);
    srtLines.push(`${i + 1}`, `${start} --> ${end}`, scene.title, scene.caption, '');
  });
  await writeFile(path.join(ARTIFACT_DIR, 'narration.srt'), srtLines.join('\n'));

  await writeFile(
    path.join(ARTIFACT_DIR, 'demo-manifest.json'),
    JSON.stringify(
      {
        title: 'EnhancementHub Product Demo',
        video: finalPath,
        durationSeconds: SCENES.reduce((s, x) => s + x.duration, 0),
        requestId: REQUEST_ID,
        scenes: SCENES,
      },
      null,
      2,
    ),
  );

  console.log(`\n✓ Demo video: ${finalPath}`);
  console.log(`  Duration: ~${SCENES.reduce((s, x) => s + x.duration, 0)}s`);
}

function formatSrtTime(seconds) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  const ms = 0;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')},${String(ms).padStart(3, '0')}`;
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
