import { describe, expect, it } from 'vitest';
import { SPA_PREFIXES } from './spaRoutes';

describe('spaRoutes', () => {
  it('includes admin and account notification routes', () => {
    expect(SPA_PREFIXES).toContain('/Spa/Admin');
    expect(SPA_PREFIXES).toContain('/Spa/Account/Notifications');
  });
});
