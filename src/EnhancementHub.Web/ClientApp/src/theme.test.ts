import { describe, expect, it } from 'vitest';
import { readStoredThemePreference } from './theme';

describe('theme preference', () => {
  it('defaults to system when no stored preference exists', () => {
    localStorage.removeItem('eh-theme');
    expect(readStoredThemePreference()).toBe('System');
  });
});
