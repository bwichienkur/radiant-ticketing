import { expect, test } from '@playwright/test';
import { DEMO_APPLICATION_ID, loginAsAdmin } from './helpers';

const MOBILE_LIST_PAGES = [
  { path: '/Spa/Applications', heading: 'Applications' },
  { path: '/Spa/Audit', heading: 'Audit log' },
  { path: '/Spa/Repositories', heading: 'Repositories' },
  { path: '/Spa/DatabaseConnections', heading: 'Database connections' },
  { path: '/Spa/Refactor/Plans', heading: 'Refactor plans' },
] as const;

test.describe('Mobile data lists', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  for (const { path, heading } of MOBILE_LIST_PAGES) {
    test(`${heading} shows content at 375px width`, async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 812 });
      await page.goto(path);
      await expect(page.getByRole('heading', { name: heading })).toBeVisible({ timeout: 15_000 });

      const mobileCards = page.locator('.cards-mobile-only .mobile-data-card');
      const emptyState = page.locator('.eh-empty-state');
      const detailContent = page.locator('.mobile-data-card, .eh-empty-state, table tbody tr');

      await expect(detailContent.first()).toBeVisible({ timeout: 15_000 });

      const cardCount = await mobileCards.count();
      const hasEmptyState = await emptyState.isVisible().catch(() => false);
      expect(cardCount > 0 || hasEmptyState).toBeTruthy();
    });
  }

  test('application detail renders on mobile without full reload', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto(`/Spa/Applications/${DEMO_APPLICATION_ID}`);
    await expect(page.getByRole('heading', { name: /radiant commerce platform/i })).toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByText('Overview')).toBeVisible();
  });
});
