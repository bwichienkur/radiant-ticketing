import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { analyzeRefactorBlastRadius, generateRefactorPlan, listApplications } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { ErrorState, FormField, LoadingState, PageHeader, useToast } from '../components/ui';
import type { ApplicationListItem, BlastRadiusResult } from '../types/spa';

export function RefactorAnalyzeApp() {
  const navigate = useNavigate();
  const toast = useToast();
  const [applications, setApplications] = useState<ApplicationListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [analyzing, setAnalyzing] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applicationId, setApplicationId] = useState('');
  const [target, setTarget] = useState('');
  const [result, setResult] = useState<BlastRadiusResult | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const apps = await listApplications();
        setApplications(apps);
        if (apps.length > 0) {
          setApplicationId(apps[0].id);
        }
      } catch {
        setError('Failed to load applications.');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  async function handleAnalyze(event: React.FormEvent) {
    event.preventDefault();
    if (!applicationId || !target.trim()) {
      return;
    }

    setAnalyzing(true);
    try {
      setResult(await analyzeRefactorBlastRadius(applicationId, target.trim()));
    } catch {
      toast.danger('Analysis failed', 'Could not analyze blast radius.');
    } finally {
      setAnalyzing(false);
    }
  }

  async function handleGeneratePlan() {
    if (!applicationId || !target.trim()) {
      return;
    }

    setGenerating(true);
    try {
      const plan = await generateRefactorPlan(applicationId, target.trim());
      toast.success('Plan generated', 'Refactor plan saved.');
      navigate(`/Spa/Refactor/Plans?planId=${plan.id}`);
    } catch {
      toast.danger('Plan generation failed', 'Could not generate refactor plan.');
    } finally {
      setGenerating(false);
    }
  }

  if (loading) {
    return <LoadingState label="Loading applications…" />;
  }

  if (error) {
    return <ErrorState message={error} />;
  }

  return (
    <div>
      <PageHeader
        title="Refactor blast-radius analysis"
        description="Estimate impact before large-scale code changes"
        actions={
          <SpaLink href="/Spa/Refactor/Plans" className="btn btn-outline-secondary">
            View plans
          </SpaLink>
        }
      />

      <form className="card-panel p-4 mb-4" onSubmit={(event) => void handleAnalyze(event)}>
        <div className="row g-3 col-lg-10">
          <div className="col-md-5">
            <FormField label="Application" id="applicationId">
              <select
                id="applicationId"
                className="form-select"
                value={applicationId}
                onChange={(event) => setApplicationId(event.target.value)}
                required
              >
                {applications.map((app) => (
                  <option key={app.id} value={app.id}>
                    {app.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>
          <div className="col-md-5">
            <FormField label="Target" id="target">
              <input
                id="target"
                className="form-control"
                value={target}
                onChange={(event) => setTarget(event.target.value)}
                placeholder="Table or entity name"
                required
              />
            </FormField>
          </div>
          <div className="col-md-2 d-flex align-items-end">
            <button type="submit" className="btn btn-primary w-100" disabled={analyzing}>
              {analyzing ? 'Analyzing…' : 'Analyze'}
            </button>
          </div>
        </div>
      </form>

      {result ? (
        <>
          <div className="card-panel mb-4">
            <div className="card-header eh-section-title px-3 py-3">
              Blast radius for <strong className="text-body">{result.targetName}</strong>
            </div>
            <ul className="list-group list-group-flush">
              {result.affectedItems.map((item) => (
                <li
                  key={`${item.name}-${item.depth}`}
                  className="list-group-item d-flex justify-content-between align-items-center"
                >
                  <span>
                    {item.name}{' '}
                    <span className="badge text-bg-secondary badge-status">{item.type}</span>
                  </span>
                  <span className="text-muted small">
                    {item.impact} (depth {item.depth})
                  </span>
                </li>
              ))}
            </ul>
          </div>
          <button
            type="button"
            className="btn btn-outline-primary"
            disabled={generating}
            onClick={() => void handleGeneratePlan()}
          >
            {generating ? 'Generating…' : 'Generate refactor plan'}
          </button>
        </>
      ) : null}
    </div>
  );
}
