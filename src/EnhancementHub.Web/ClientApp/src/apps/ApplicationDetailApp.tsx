import { useCallback, useEffect, useState } from 'react';
import { getApplicationDetail } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { ErrorState, LoadingState, PageHeader, SectionCard } from '../components/ui';
import type { ApplicationDetailItem, ApplicationProfile } from '../types/spa';

interface ApplicationDetailAppProps {
  applicationId: string;
}

function formatGeneratedAt(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

export function ApplicationDetailApp({ applicationId }: ApplicationDetailAppProps) {
  const [application, setApplication] = useState<ApplicationDetailItem | null>(null);
  const [profiles, setProfiles] = useState<ApplicationProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const detail = await getApplicationDetail(applicationId);
      setApplication(detail.application);
      setProfiles(detail.profiles);
    } catch {
      setError('Failed to load application.');
      setApplication(null);
      setProfiles([]);
    } finally {
      setLoading(false);
    }
  }, [applicationId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (loading) {
    return <LoadingState label="Loading application…" />;
  }

  if (error || !application) {
    return (
      <ErrorState
        message={error ?? 'Application not found.'}
        onRetry={() => void reload()}
      />
    );
  }

  const profile = profiles[0] ?? null;

  return (
    <div aria-live="polite">
      <PageHeader
        title={application.name}
        description={application.businessDomain ?? 'Application profile'}
        actions={
          <>
            <SpaLink
              href={`/Spa/SystemMap?applicationId=${application.id}`}
              className="btn btn-outline-primary btn-sm"
            >
              System map
            </SpaLink>
            <SpaLink href="/Spa/OnboardingWizard" className="btn btn-outline-secondary btn-sm">
              Setup wizard
            </SpaLink>
          </>
        }
      />

      <div className="row g-4">
        <div className="col-lg-7">
          <SectionCard title="Overview" className="mb-4">
            <p>
              <strong>Purpose:</strong> {application.purpose ?? '—'}
            </p>
            <p>
              <strong>Description:</strong> {application.description ?? '—'}
            </p>
            <p className="mb-0">
              <strong>Risk-sensitive areas:</strong> {application.riskSensitiveAreas ?? '—'}
            </p>
          </SectionCard>

          {profile ? (
            <SectionCard title="Generated profile">
              <p>
                <strong>Key components:</strong>
              </p>
              <pre className="small bg-body-secondary rounded p-3">{profile.keyComponents ?? '—'}</pre>
              <p>
                <strong>Database usage:</strong> {profile.databaseUsage ?? '—'}
              </p>
              <p>
                <strong>Integrations:</strong> {profile.externalIntegrations ?? '—'}
              </p>
              <p className="mb-0 text-muted small">Generated {formatGeneratedAt(profile.generatedAt)}</p>
            </SectionCard>
          ) : null}
        </div>

        <div className="col-lg-5">
          <SectionCard title="Repositories">
            <p className="mb-0">
              {application.repositoryCount} repository(ies) registered.
            </p>
            <SpaLink href="/Spa/Repositories" className="btn btn-sm btn-outline-primary mt-3">
              View repositories
            </SpaLink>
          </SectionCard>
        </div>
      </div>
    </div>
  );
}
