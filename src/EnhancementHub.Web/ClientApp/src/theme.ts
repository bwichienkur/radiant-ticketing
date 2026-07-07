export type ThemePreference = 'System' | 'Light' | 'Dark';

const STORAGE_THEME = 'eh-theme';

export function resolveThemePreference(preference: ThemePreference): 'light' | 'dark' {
  if (preference === 'Light') {
    return 'light';
  }

  if (preference === 'Dark') {
    return 'dark';
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function applyThemePreference(preference: ThemePreference) {
  const resolved = resolveThemePreference(preference);
  document.documentElement.setAttribute('data-bs-theme', resolved);
  localStorage.setItem(STORAGE_THEME, preference);

  document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
    button.setAttribute('aria-pressed', resolved === 'dark' ? 'true' : 'false');
    button.setAttribute(
      'title',
      preference === 'System'
        ? 'Theme: system'
        : resolved === 'dark'
          ? 'Switch to light mode'
          : 'Switch to dark mode',
    );
  });
}

export function applyTenantBranding(accentColor: string, productName?: string | null, logoUrl?: string | null) {
  document.documentElement.style.setProperty('--eh-accent', accentColor);
  document.documentElement.style.setProperty('--bs-primary', accentColor);

  const brandText = document.querySelector('.sidebar-brand-text');
  if (brandText && productName) {
    brandText.textContent = productName;
  }

  const brandMark = document.querySelector('.sidebar-brand-mark');
  if (brandMark && logoUrl) {
    brandMark.innerHTML = `<img src="${logoUrl}" alt="" class="sidebar-brand-logo" />`;
  }
}

export function readStoredThemePreference(): ThemePreference {
  const stored = localStorage.getItem(STORAGE_THEME);
  if (stored === 'Light' || stored === 'Dark' || stored === 'System') {
    return stored;
  }

  return 'Dark';
}
