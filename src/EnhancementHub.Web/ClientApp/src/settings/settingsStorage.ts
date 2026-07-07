const RECENT_KEY = 'eh-settings-recent';
const FAVORITES_KEY = 'eh-settings-favorites';
const MAX_RECENT = 6;

export function readRecentSettings(): string[] {
  try {
    const raw = localStorage.getItem(RECENT_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

export function pushRecentSetting(sectionId: string) {
  const recent = readRecentSettings().filter((id) => id !== sectionId);
  recent.unshift(sectionId);
  localStorage.setItem(RECENT_KEY, JSON.stringify(recent.slice(0, MAX_RECENT)));
}

export function readFavoriteSettings(): string[] {
  try {
    const raw = localStorage.getItem(FAVORITES_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

export function toggleFavoriteSetting(sectionId: string): string[] {
  const favorites = readFavoriteSettings();
  const next = favorites.includes(sectionId)
    ? favorites.filter((id) => id !== sectionId)
    : [...favorites, sectionId];
  localStorage.setItem(FAVORITES_KEY, JSON.stringify(next));
  return next;
}

export function isFavoriteSetting(sectionId: string): boolean {
  return readFavoriteSettings().includes(sectionId);
}
