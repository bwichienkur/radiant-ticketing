import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { getUserAppearance, updateThemePreference } from '../api/spaClient';
import type { ThemePreference } from '../theme';
import { applyThemePreference } from '../theme';
import { SegmentedControl } from './ui/SegmentedControl';

const OPTIONS: ThemePreference[] = ['System', 'Light', 'Dark'];

export function ThemePreferenceSelector() {
  const [preference, setPreference] = useState<ThemePreference>('Dark');
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

  const content = (
    <div className="theme-preference-selector d-flex align-items-center gap-2" aria-label="Theme preference">
      <SegmentedControl
        ariaLabel="Choose theme preference"
        value={preference}
        onChange={(value) => void onChange(value)}
        options={OPTIONS.map((option) => ({ value: option, label: option }))}
      />
      {saving ? <span className="visually-hidden">Saving theme…</span> : null}
    </div>
  );

  const slot = document.getElementById('eh-topbar-theme-slot');
  if (slot) {
    return createPortal(content, slot);
  }

  return content;
}
