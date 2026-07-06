import { useEffect, useState } from 'react';
import { getUserAppearance, updateThemePreference } from '../api/spaClient';
import type { ThemePreference } from '../theme';
import { applyThemePreference } from '../theme';

const OPTIONS: ThemePreference[] = ['System', 'Light', 'Dark'];

export function ThemePreferenceSelector() {
  const [preference, setPreference] = useState<ThemePreference>('System');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    void (async () => {
      try {
        const appearance = await getUserAppearance();
        const pref = appearance.themePreference as ThemePreference;
        setPreference(pref);
        applyThemePreference(pref);
      } catch {
        applyThemePreference('System');
      }
    })();
  }, []);

  async function onChange(next: ThemePreference) {
    setPreference(next);
    applyThemePreference(next);
    setSaving(true);
    try {
      await updateThemePreference(next);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="theme-preference-selector d-flex align-items-center gap-2" aria-label="Theme preference">
      <span className="small text-muted">Theme</span>
      <select
        className="form-select form-select-sm"
        value={preference}
        disabled={saving}
        onChange={(event) => void onChange(event.target.value as ThemePreference)}
        aria-label="Choose theme preference"
      >
        {OPTIONS.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </div>
  );
}
