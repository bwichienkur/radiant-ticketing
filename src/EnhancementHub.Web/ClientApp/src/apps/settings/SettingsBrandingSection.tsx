import { FormEvent, useEffect, useState } from 'react';
import { getUserAppearance, updateTenantBranding } from '../../api/spaClient';
import { SectionCard } from '../../components/ui';
import { useToast } from '../../components/ui/useToast';
import { applyTenantBranding } from '../../theme';

export function SettingsBrandingSection() {
  const toast = useToast();
  const [logoUrl, setLogoUrl] = useState('');
  const [accentColor, setAccentColor] = useState('#2563eb');
  const [productName, setProductName] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    void (async () => {
      try {
        const appearance = await getUserAppearance();
        setLogoUrl(appearance.branding.logoUrl ?? '');
        setAccentColor(appearance.branding.accentColor);
        setProductName(appearance.branding.productName ?? '');
        applyTenantBranding(
          appearance.branding.accentColor,
          appearance.branding.productName,
          appearance.branding.logoUrl,
        );
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    try {
      await updateTenantBranding({
        logoUrl: logoUrl.trim() || undefined,
        accentColor: accentColor.trim(),
        productName: productName.trim() || undefined,
      });
      applyTenantBranding(accentColor.trim(), productName.trim() || undefined, logoUrl.trim() || undefined);
      toast.success('Branding updated.');
    } catch {
      toast.danger('Failed to update branding.');
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return <SectionCard title="Tenant branding">Loading…</SectionCard>;
  }

  return (
    <SectionCard title="Tenant branding">
      <p className="text-muted small mb-3">Logo, accent color, and product name for this tenant.</p>
      <form onSubmit={(event) => void handleSubmit(event)} className="row g-3">
        <div className="col-md-6">
          <label className="form-label" htmlFor="branding-product-name">
            Product name
          </label>
          <input
            id="branding-product-name"
            className="form-control"
            value={productName}
            onChange={(event) => setProductName(event.target.value)}
          />
        </div>
        <div className="col-md-6">
          <label className="form-label" htmlFor="branding-accent">
            Accent color
          </label>
          <input
            id="branding-accent"
            type="color"
            className="form-control form-control-color"
            value={accentColor}
            onChange={(event) => setAccentColor(event.target.value)}
          />
        </div>
        <div className="col-12">
          <label className="form-label" htmlFor="branding-logo">
            Logo URL
          </label>
          <input
            id="branding-logo"
            className="form-control"
            value={logoUrl}
            onChange={(event) => setLogoUrl(event.target.value)}
            placeholder="https://…"
          />
        </div>
        <div className="col-12">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Saving…' : 'Save branding'}
          </button>
        </div>
      </form>
    </SectionCard>
  );
}
