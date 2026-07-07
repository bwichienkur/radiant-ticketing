import { SpaLink } from '../SpaLink';
import { IconSparkle } from './SettingsIcons';
import { SettingsSummaryStrip } from './SettingsSummaryStrip';

export function SettingsPageHeader({ compact = false }: { compact?: boolean }) {
  return (
    <header className={`eh-settings-page-header ${compact ? 'eh-settings-page-header--compact' : ''}`.trim()}>
      <div className="eh-settings-page-header__main">
        <div className="eh-settings-page-header__copy">
          <h1 className="eh-settings-page-header__title">Settings</h1>
          <p className="eh-settings-page-header__description">
            Manage your workspace, security, integrations, AI configuration, compliance, and platform administration.
          </p>
        </div>
        {!compact ? (
          <div className="eh-settings-page-header__actions">
            <SpaLink href="/Spa/Settings/Teams" className="btn btn-primary eh-settings-action-btn">
              + Invite team
            </SpaLink>
            <a
              href="/web-api/swagger"
              className="btn btn-outline-secondary eh-settings-action-btn"
              target="_blank"
              rel="noreferrer"
            >
              API documentation
            </a>
            <SpaLink href="/Spa/Documentation/Export" className="btn btn-outline-secondary eh-settings-action-btn">
              Export settings
            </SpaLink>
            <SpaLink href="/Spa/PortfolioHealth" className="btn btn-outline-secondary eh-settings-action-btn">
              Workspace health
            </SpaLink>
          </div>
        ) : null}
      </div>
      {!compact ? <SettingsSummaryStrip /> : null}
    </header>
  );
}

export function SettingsAiFab({ onClick }: { onClick: () => void }) {
  return (
    <button type="button" className="eh-settings-ai-fab" onClick={onClick} aria-label="Open settings AI assistant">
      <IconSparkle />
      <span>AI Assistant</span>
    </button>
  );
}
