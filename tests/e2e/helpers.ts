import { expect, type Page } from '@playwright/test';

export const DEMO_REQUEST_ID = '279c38dc-8da4-400b-828f-711726210eb6';
export const DEMO_APPLICATION_ID = '33333333-3333-3333-3333-333333333333';
export const ADMIN_EMAIL = 'admin@enhancementhub.dev';
export const ADMIN_PASSWORD = 'password123';

export async function dismissProductTour(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('eh-product-tour-seen', 'true');
  });
}

export async function loginAsAdmin(page: Page) {
  await dismissProductTour(page);
  await page.goto('/Account/Login');
  await page.getByLabel('Work email').fill(ADMIN_EMAIL);
  await page.getByLabel('Password').fill(ADMIN_PASSWORD);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/(Index)?$/);
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
}
