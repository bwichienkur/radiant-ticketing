#!/usr/bin/env node
/**
 * Starts EnhancementHub.Api, waits for readiness, runs k6 Horizon 3 profile, then stops the API.
 * Use K6_PROFILE=ci for a short CI-friendly run (default for this script).
 */
import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import { mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const port = process.env.LOAD_TEST_PORT ?? '5075';
const baseUrl = process.env.BASE_URL ?? `http://127.0.0.1:${port}`;
const apiProject = path.join(repoRoot, 'src/EnhancementHub.Api/EnhancementHub.Api.csproj');
const k6Script = path.join(repoRoot, 'tests/load/k6-horizon3.js');
const resultsDir = path.join(repoRoot, 'artifacts/load-test');
const profile = process.env.K6_PROFILE ?? 'ci';

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
      stdio: options.capture ? ['ignore', 'pipe', 'pipe'] : 'inherit',
      ...options,
    });

    let stdout = '';
    let stderr = '';
    if (options.capture) {
      child.stdout?.on('data', (chunk) => {
        stdout += chunk.toString();
        if (!options.quiet) {
          process.stdout.write(chunk);
        }
      });
      child.stderr?.on('data', (chunk) => {
        stderr += chunk.toString();
        if (!options.quiet) {
          process.stderr.write(chunk);
        }
      });
    }

    child.on('error', reject);
    child.on('exit', (code) => {
      if (code === 0) {
        resolve({ stdout, stderr });
      } else {
        const error = new Error(`${command} ${args.join(' ')} exited with code ${code}`);
        error.stdout = stdout;
        error.stderr = stderr;
        reject(error);
      }
    });
  });
}

let apiProcess;

async function main() {
  console.log('Building EnhancementHub.Api...');
  await run('dotnet', ['build', apiProject, '-c', 'Release', '--nologo'], { cwd: repoRoot });

  console.log(`Starting API on ${baseUrl}...`);
  apiProcess = spawn(
    'dotnet',
    ['run', '--project', apiProject, '-c', 'Release', '--no-build', '--urls', baseUrl, '--nologo'],
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

  apiProcess.stdout?.on('data', (chunk) => process.stdout.write(chunk));
  apiProcess.stderr?.on('data', (chunk) => process.stderr.write(chunk));

  await waitForReady(baseUrl);
  console.log(`API is ready. Running k6 Horizon 3 profile (${profile})...`);

  const { stdout } = await run(
    'k6',
    ['run', k6Script],
    {
      cwd: repoRoot,
      capture: true,
      env: {
        ...process.env,
        BASE_URL: baseUrl,
        K6_PROFILE: profile,
        TEST_USER: process.env.TEST_USER ?? 'admin@enhancementhub.dev',
        TEST_PASSWORD: process.env.TEST_PASSWORD ?? 'password123',
      },
    },
  );

  await mkdir(resultsDir, { recursive: true });
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const outputPath = path.join(resultsDir, `k6-horizon3-${profile}-${timestamp}.txt`);
  await writeFile(outputPath, stdout, 'utf8');
  console.log(`Saved k6 output to ${outputPath}`);
}

function stopApi() {
  if (apiProcess && !apiProcess.killed) {
    apiProcess.kill('SIGTERM');
  }
}

process.on('SIGINT', () => {
  stopApi();
  process.exit(130);
});

process.on('SIGTERM', () => {
  stopApi();
  process.exit(143);
});

try {
  await main();
  console.log('k6 Horizon 3 load test completed.');
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
} finally {
  stopApi();
}
