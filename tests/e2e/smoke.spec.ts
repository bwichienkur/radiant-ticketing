import { expect, test } from '@playwright/test';
import { DEMO_REQUEST_ID, loginAsAdmin } from './helpers';

test.describe('EnhancementHub smoke', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('dashboard shows pipeline search and stats', async ({ page }) => {
    await expect(page.locator('#spa-dashboard-root')).toBeVisible();
    await expect(page.locator('.copilot-bar .fw-semibold')).toHaveText('Pipeline search', {
      timeout: 15_000,
    });
    await expect(page.getByText('not a generative AI chat')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  });

  test('pipeline search returns approval queue shortcut', async ({ page }) => {
    await page.getByPlaceholder(/high risk pending approval/i).fill('pending approval');
    await page.getByRole('button', { name: 'Search' }).click();
    await expect(page.locator('#copilot-results a').first()).toBeVisible({ timeout: 10_000 });
  });

  test('approval queue SPA loads pending items', async ({ page }) => {
    await page.goto('/Spa/ApprovalQueue');
    await expect(page.locator('#spa-approval-queue-root')).toBeVisible();
    await expect(page.getByText('Pending', { exact: true })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(/order cancellation|compliance/i).first()).toBeVisible({ timeout: 15_000 });
  });

  test('request detail SPA loads demo request', async ({ page }) => {
    await page.goto(`/Spa/RequestDetail/${DEMO_REQUEST_ID}`);
    await expect(page.locator('#spa-request-detail-root')).toBeVisible();
    await expect(page.getByRole('heading', { name: /order cancellation/i })).toBeVisible({
      timeout: 20_000,
    });
  });

  test('system map SPA loads applications', async ({ page }) => {
    await page.goto('/Spa/SystemMap');
    await expect(page.locator('#spa-system-map-root')).toBeVisible();
    await expect(page.getByLabel('Application')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: 'Rebuild graph' })).toBeVisible();
  });

  test('onboarding wizard SPA mounts', async ({ page }) => {
    await page.goto('/Spa/OnboardingWizard');
    await expect(page.locator('#spa-onboarding-wizard-root')).toBeVisible();
    await expect(page.getByRole('list', { name: 'Onboarding steps' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('listitem').filter({ hasText: 'Basics' })).toBeVisible();
  });

  test('create request SPA loads form', async ({ page }) => {
    await page.goto('/Spa/CreateRequest');
    await expect(page.locator('#spa-create-request-root')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'New Enhancement Request' })).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.getByLabel('Title')).toBeVisible();
  });

  test('request list SPA loads backlog', async ({ page }) => {
    await page.goto('/Spa/RequestList');
    await expect(page.getByRole('heading', { name: 'Enhancement Requests' })).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.getByRole('link', { name: 'New Request' }).first()).toBeVisible();
    await expect(page.getByText(/order cancellation|compliance/i).first()).toBeVisible({
      timeout: 15_000,
    });
  });
});
