#!/usr/bin/env node
/**
 * Fails when a new MediatR handler in Application uses IEnhancementHubDbContext
 * unless the file path is listed in docs/ef-handler-allowlist.txt.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const applicationRoot = path.join(repoRoot, 'src/EnhancementHub.Application');
const allowlistPath = path.join(repoRoot, 'docs/ef-handler-allowlist.txt');

const allowlist = new Set(
  fs
    .readFileSync(allowlistPath, 'utf8')
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith('#')),
);

function walk(dir, files = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      walk(fullPath, files);
    } else if (entry.name.endsWith('.cs')) {
      files.push(fullPath);
    }
  }
  return files;
}

const violations = [];

for (const file of walk(applicationRoot)) {
  const relative = path.relative(repoRoot, file).replaceAll('\\', '/');
  const content = fs.readFileSync(file, 'utf8');
  const isHandler =
    content.includes('IRequestHandler<') || content.includes(': IRequestHandler');
  const usesDbContext = content.includes('IEnhancementHubDbContext');

  if (!isHandler || !usesDbContext) {
    continue;
  }

  if (!allowlist.has(relative)) {
    violations.push(relative);
  }
}

if (violations.length > 0) {
  console.error('Handlers using IEnhancementHubDbContext must be allowlisted or migrated:');
  for (const file of violations.sort()) {
    console.error(`  - ${file}`);
  }
  process.exit(1);
}

console.log(`EF handler allowlist check passed (${allowlist.size} allowlisted handlers).`);
