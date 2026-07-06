import { chromium } from 'playwright';

const baseURL = 'http://127.0.0.1:5001';
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
page.on('console', (msg) => console.log('CONSOLE', msg.type(), msg.text()));
page.on('pageerror', (err) => console.log('PAGEERROR', err.message));

await page.addInitScript(() => localStorage.setItem('eh-product-tour-seen', 'true'));
await page.goto(`${baseURL}/Account/Login`);
await page.getByLabel('Work email').fill('admin@enhancementhub.dev');
await page.getByLabel('Password').fill('password123');
await page.getByRole('button', { name: 'Sign in' }).click();
await page.waitForTimeout(5000);
const html = await page.locator('#spa-root').innerHTML();
console.log('spa-root html length:', html.length);
console.log('spa-root snippet:', html.slice(0, 300));
await page.screenshot({ path: '/opt/cursor/artifacts/screenshots/debug-dashboard2.png', fullPage: true });
await browser.close();
