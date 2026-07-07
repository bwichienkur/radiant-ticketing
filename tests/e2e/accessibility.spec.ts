import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';
import { DEMO_REQUEST_ID, loginAsAdmin } from './helpers';

async function expectNoSeriousViolations(page: import('@playwright/test').Page) {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  const serious = results.violations.filter(
    (violation) => violation.impact === 'serious' || violation.impact === 'critical',
  );

  expect(
    serious,
    serious.map((v) => `${v.id}: ${v.help} (${v.nodes.length} nodes)`).join('\n'),
  ).toEqual([]);
}

test.describe('Accessibility (axe)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('dashboard has no serious axe violations', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible({ timeout: 15_000 });
    await expectNoSeriousViolations(page);
  });

  test('approval queue has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/ApprovalQueue');
    await expect(page.getByText('Pending', { exact: true })).toBeVisible({ timeout: 15_000 });
    await expectNoSeriousViolations(page);
  });

  test('request detail has no serious axe violations', async ({ page }) => {
    await page.goto(`/Spa/RequestDetail/${DEMO_REQUEST_ID}`);
    await expect(page.getByRole('heading', { name: /order cancellation/i })).toBeVisible({
      timeout: 20_000,
    });
    await expectNoSeriousViolations(page);
  });

  test('create request has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/CreateRequest');
    await expect(page.getByRole('heading', { name: 'Tell us what you need changed' })).toBeVisible({
      timeout: 15_000,
    });
    await expectNoSeriousViolations(page);
  });

  test('system map has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/SystemMap');
    await expect(page.getByLabel('Application')).toBeVisible({ timeout: 15_000 });
    await expectNoSeriousViolations(page);
  });

  test('settings has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/Settings/General');
    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible({
      timeout: 15_000,
    });
    await expectNoSeriousViolations(page);
  });

  test('portfolio hub has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/Portfolio');
    await expect(page.getByRole('heading', { name: 'Portfolio' })).toBeVisible({ timeout: 15_000 });
    await expectNoSeriousViolations(page);
  });

  test('portfolio health has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/PortfolioHealth');
    await expect(page.getByRole('heading', { name: 'Portfolio health' })).toBeVisible({
      timeout: 15_000,
    });
    await expectNoSeriousViolations(page);
  });

  test('global search has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/Search');
    await expect(page.getByRole('heading', { name: 'Search' })).toBeVisible({ timeout: 15_000 });
    await expectNoSeriousViolations(page);
  });

  test('onboarding wizard has no serious axe violations', async ({ page }) => {
    await page.goto('/Spa/OnboardingWizard');
    await expect(page.getByRole('heading', { name: 'Set up a system for change requests' })).toBeVisible({
      timeout: 15_000,
    });
    await expectNoSeriousViolations(page);
  });
});
