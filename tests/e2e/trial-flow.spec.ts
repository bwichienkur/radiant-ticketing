import { expect, test } from '@playwright/test';
import { dismissProductTour } from './helpers';

function uniqueTrialSlug() {
  return `trial-e2e-${Date.now().toString(36)}`;
}

test.describe('Self-serve trial flow', () => {
  test('signup to first request in under 30 minutes', async ({ page }) => {
    const slug = uniqueTrialSlug();
    const email = `${slug}@trial-e2e.test`;

    await dismissProductTour(page);
    await page.goto('/Account/Signup');
    await expect(page.getByRole('heading', { name: 'Start your trial' })).toBeVisible();

    await page.getByLabel('Organization name').fill('E2E Trial Org');
    await page.getByLabel('Workspace slug').fill(slug);
    await page.getByLabel('Your name').fill('E2E Trial Admin');
    await page.getByLabel('Work email').fill(email);
    await page.getByLabel('Password').fill('password123');
    await page.getByRole('button', { name: 'Create trial workspace' }).click();

    await expect(page).toHaveURL(/\/(Index)?$/, { timeout: 20_000 });
    await expect(page.locator('#spa-root')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();

    await page.goto('/Spa/CreateRequest');
    await expect(page.getByRole('heading', { name: 'Tell us what you need changed' })).toBeVisible({
      timeout: 15_000,
    });
    await page.getByRole('button', { name: 'Fill in the form manually' }).click();

    const requestTitle = `Trial smoke request ${slug}`;
    await page.getByLabel('Title').fill(requestTitle);
    await page.getByLabel('What problem are you trying to solve?').fill(
      'Automated trial flow validation for market readiness.',
    );
    await page.getByLabel('What does success look like?').fill(
      'Confirm signup through first request works end-to-end.',
    );
    await page.getByRole('button', { name: 'Submit request' }).click();

    await expect(page).toHaveURL(/\/Spa\/RequestDetail\//, { timeout: 20_000 });
    await expect(page.getByRole('heading', { name: requestTitle })).toBeVisible({ timeout: 15_000 });
  });
});
