// Validates core operator pages include accessibility landmarks checked in CI.
const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '../..');
const checks = [
  {
    file: 'src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml',
    mustContain: ['skip-link', '#main-content'],
  },
  {
    file: 'src/EnhancementHub.Web/Pages/Onboarding/_OnboardingProgress.cshtml',
    mustContain: ['aria-current'],
  },
  {
    file: 'src/EnhancementHub.Web/ClientApp/src/apps/OnboardingWizardApp.tsx',
    mustContain: ["aria-current={isCurrent ? 'step' : undefined}"],
  },
  {
    file: 'src/EnhancementHub.Web/ClientApp/src/components/CommandPalette.tsx',
    mustContain: ['role="dialog"', 'aria-label="Command palette"', 'role="listbox"'],
  },
  {
    file: 'src/EnhancementHub.Web/ClientApp/src/components/SystemMapGraph.tsx',
    mustContain: ['tabIndex={0}', 'onKeyDown'],
  },
  {
    file: 'src/EnhancementHub.Web/Pages/Admin/Jobs.cshtml',
    mustContain: ['scope="col"'],
  },
];

let failed = false;

for (const check of checks) {
  const fullPath = path.join(repoRoot, check.file);
  const content = fs.readFileSync(fullPath, 'utf8');
  for (const needle of check.mustContain) {
    if (!content.includes(needle)) {
      console.error(`Missing "${needle}" in ${check.file}`);
      failed = true;
    }
  }
}

if (failed) {
  process.exit(1);
}

console.log('Accessibility static checks passed.');
