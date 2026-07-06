#!/usr/bin/env node
/**
 * Starts EnhancementHub.Web, runs Playwright axe suite (serious/critical violations fail CI).
 */
import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const port = process.env.E2E_PORT ?? '5098';
const baseUrl = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${port}`;
const webProject = path.join(repoRoot, 'src/EnhancementHub.Web/EnhancementHub.Web.csproj');
const e2eDir = path.join(repoRoot, 'tests/e2e');

async function waitForReady(url, attempts = 90) {
  for (let i = 0; i < attempts; i += 1) {
    try {
      const response = await fetch(`${url}/health/ready`);
      if (response.ok) {
        return;
      }
    } catch {
      // server still starting
    }
    await delay(1000);
  }

  throw new Error(`Timed out waiting for ${url}/health/ready`);
}

function run(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      stdio: 'inherit',
      ...options,
    });
    child.on('error', reject);
    child.on('exit', (code) => {
      if (code === 0) {
        resolve();
      } else {
        reject(new Error(`${command} ${args.join(' ')} exited with code ${code}`));
      }
    });
  });
}

let webProcess;

async function main() {
  await run('dotnet', ['build', webProject, '-c', 'Release', '--nologo'], { cwd: repoRoot });

  webProcess = spawn(
    'dotnet',
    ['run', '--project', webProject, '-c', 'Release', '--no-build', '--urls', baseUrl, '--nologo'],
    {
      cwd: repoRoot,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        DOTNET_ENVIRONMENT: 'Development',
      },
      stdio: ['ignore', 'pipe', 'pipe'],
    },
  );

  webProcess.stdout?.on('data', (chunk) => process.stdout.write(chunk));
  webProcess.stderr?.on('data', (chunk) => process.stderr.write(chunk));

  await waitForReady(baseUrl);

  await run(
    'npx',
    ['playwright', 'test', 'accessibility.spec.ts', '--config', 'playwright.config.ts'],
    {
      cwd: e2eDir,
      env: {
        ...process.env,
        E2E_BASE_URL: baseUrl,
      },
    },
  );
}

function stopWeb() {
  if (webProcess && !webProcess.killed) {
    webProcess.kill('SIGTERM');
  }
}

process.on('SIGINT', () => {
  stopWeb();
  process.exit(130);
});

process.on('SIGTERM', () => {
  stopWeb();
  process.exit(143);
});

try {
  await main();
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
} finally {
  stopWeb();
}
