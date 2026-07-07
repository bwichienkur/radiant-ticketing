import { describe, expect, it } from 'vitest';
import { resolveSpaPageMeta } from './spaPageMeta';

describe('resolveSpaPageMeta', () => {
  it('resolves dashboard routes', () => {
    expect(resolveSpaPageMeta('/')).toMatchObject({ title: 'Dashboard', section: 'Home' });
    expect(resolveSpaPageMeta('/Index')).toMatchObject({ title: 'Dashboard', section: 'Home' });
  });

  it('resolves portfolio hub and nested intelligence routes', () => {
    expect(resolveSpaPageMeta('/Spa/Portfolio')).toMatchObject({
      title: 'Portfolio',
      section: 'Portfolio',
    });
    expect(resolveSpaPageMeta('/Spa/Applications')).toMatchObject({ section: 'Portfolio' });
    expect(resolveSpaPageMeta('/Spa/SchemaDrift')).toMatchObject({ section: 'Portfolio' });
  });

  it('resolves request detail by prefix', () => {
    expect(resolveSpaPageMeta('/Spa/RequestDetail/abc-123')).toMatchObject({
      title: 'Request',
      section: 'Work',
    });
  });

  it('resolves settings and admin sections', () => {
    expect(resolveSpaPageMeta('/Spa/Settings/General')).toMatchObject({
      breadcrumb: 'General',
      section: 'Settings',
    });
    expect(resolveSpaPageMeta('/Spa/Admin/Jobs')).toMatchObject({ section: 'Settings' });
  });
});
