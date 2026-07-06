import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { getRefactorPlan, listRefactorPlans } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader, ResponsiveDataList } from '../components/ui';
import type { RefactorPlanDetail, RefactorPlanSummary } from '../types/spa';

export function RefactorPlansApp() {
  const [searchParams, setSearchParams] = useSearchParams();
  const planId = searchParams.get('planId') ?? undefined;
  const [plans, setPlans] = useState<RefactorPlanSummary[]>([]);
  const [selectedPlan, setSelectedPlan] = useState<RefactorPlanDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const planList = await listRefactorPlans();
      setPlans(planList);
      if (planId) {
        setSelectedPlan(await getRefactorPlan(planId));
      } else {
        setSelectedPlan(null);
      }
    } catch {
      setError('Failed to load refactor plans.');
    } finally {
      setLoading(false);
    }
  }, [planId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  function selectPlan(id: string) {
    setSearchParams({ planId: id });
  }

  if (loading) {
    return <LoadingState label="Loading refactor plans…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div>
      <PageHeader
        title="Refactor plans"
        description="Saved blast-radius analyses and implementation plans"
        actions={
          <SpaLink href="/Spa/Refactor/Analyze" className="btn btn-primary">
            New analysis
          </SpaLink>
        }
      />

      {plans.length === 0 ? (
        <EmptyState
          title="No refactor plans yet"
          description="Run a blast-radius analysis to generate your first refactor plan."
          icon="document"
          action={
            <SpaLink href="/Spa/Refactor/Analyze" className="btn btn-primary">
              Start analysis
            </SpaLink>
          }
        />
      ) : (
        <ResponsiveDataList
          items={plans}
          getRowKey={(plan) => plan.id}
          columns={[
            {
              id: 'title',
              header: 'Title',
              cell: (plan) => (
                <button
                  type="button"
                  className="btn btn-link p-0 align-baseline"
                  onClick={() => selectPlan(plan.id)}
                >
                  {plan.title}
                </button>
              ),
            },
            { id: 'target', header: 'Target', cell: (plan) => plan.targetDescription },
            {
              id: 'risk',
              header: 'Risk',
              cell: (plan) => <span className="badge text-bg-warning badge-status">{plan.riskLevel}</span>,
            },
            { id: 'status', header: 'Status', cell: (plan) => plan.status },
            {
              id: 'created',
              header: 'Created',
              cell: (plan) => new Date(plan.createdAt).toLocaleString(),
            },
          ]}
          renderMobileCard={(plan) => (
            <>
              <button
                type="button"
                className="btn btn-link p-0 mobile-data-card-title text-start"
                onClick={() => selectPlan(plan.id)}
              >
                {plan.title}
              </button>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Risk</span>
                <span>{plan.riskLevel}</span>
              </div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Status</span>
                <span>{plan.status}</span>
              </div>
            </>
          )}
        />
      )}

      {selectedPlan ? (
        <div className="card-panel mt-4 p-4">
          <h2 className="eh-section-title mb-3">{selectedPlan.title}</h2>
          <pre className="mb-0 small">{selectedPlan.planMarkdown ?? 'No plan content.'}</pre>
        </div>
      ) : null}
    </div>
  );
}
