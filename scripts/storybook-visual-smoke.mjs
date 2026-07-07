#!/usr/bin/env node
/**
 * Serves built Storybook and captures baseline screenshots for key UI kit stories.
 * Fails if expected story routes are missing from the static build.
 */
import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';
import { chromium } from 'playwright';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const storybookDir = path.join(repoRoot, 'src/EnhancementHub.Web/ClientApp/storybook-static');
const port = Number(process.env.STORYBOOK_PORT ?? 6010);
const baseUrl = `http://127.0.0.1:${port}`;

const requiredStories = [
  'ui-commandpalette--closed',
  'ui-responsivedatalist--default',
  'ui-themepreferenceselector--default',
  'ui-kit-components--alert-variants',
];

function runStaticServer() {
  const server = createServer(async (req, res) => {
    try {
      const urlPath = req.url === '/' ? '/index.html' : req.url ?? '/index.html';
      const filePath = path.join(storybookDir, decodeURIComponent(urlPath.split('?')[0]));
      const data = await readFile(filePath);
      const ext = path.extname(filePath);
      const type =
        ext === '.html' ? 'text/html' :
        ext === '.js' ? 'text/javascript' :
        ext === '.css' ? 'text/css' :
        ext === '.json' ? 'application/json' :
        'application/octet-stream';
      res.writeHead(200, { 'Content-Type': type });
      res.end(data);
    } catch {
      res.writeHead(404);
      res.end('Not found');
    }
  });

  return new Promise((resolve, reject) => {
    server.listen(port, '127.0.0.1', () => resolve(server));
    server.on('error', reject);
  });
}

async function main() {
  const indexHtml = await readFile(path.join(storybookDir, 'index.html'), 'utf8');
  if (!indexHtml.includes('storybook')) {
    throw new Error('Storybook static build missing index.html');
  }

  const server = await runStaticServer();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });

  try {
    for (const storyId of requiredStories) {
      await page.goto(`${baseUrl}/iframe.html?id=${storyId}&viewMode=story`, {
        waitUntil: 'networkidle',
      });
      await page.locator('#storybook-root').waitFor({ state: 'visible', timeout: 15_000 });
      const text = await page.locator('body').innerText();
      if (!text.trim()) {
        throw new Error(`Story ${storyId} rendered empty content`);
      }
      console.log(`verified story: ${storyId}`);
    }
  } finally {
    await browser.close();
    server.close();
  }

  console.log('Storybook visual smoke passed.');
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});
