import { chromium } from 'playwright';

const baseURL = 'http://127.0.0.1:5001';
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
await page.goto(`${baseURL}/Account/Login`);
await page.getByLabel('Work email').fill('admin@enhancementhub.dev');
await page.getByLabel('Password').fill('password123');
await page.getByRole('button', { name: 'Sign in' }).click();
await page.waitForTimeout(5000);
console.log('URL:', page.url());
console.log('Title:', await page.title());
const text = await page.locator('body').innerText();
console.log('Body snippet:', text.slice(0, 500));
await page.screenshot({ path: '/opt/cursor/artifacts/screenshots/debug-dashboard.png', fullPage: true });
await browser.close();
