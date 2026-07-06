import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import {
  AlertBanner,
  ConfirmDialog,
  EmptyState,
  ErrorState,
  FormField,
  ListToolbar,
  LoadingState,
  PageHeader,
  Pagination,
  SectionCard,
  StatusBadge,
} from './index';

const meta: Meta = {
  title: 'UI Kit/Components',
};

export default meta;

type Story = StoryObj;

export const PageHeaderDefault: Story = {
  render: () => (
    <PageHeader
      title="Enhancement requests"
      description="Triage, search, and open request details"
      actions={<button type="button" className="btn btn-primary">New request</button>}
    />
  ),
};

export const StatusBadges: Story = {
  render: () => (
    <div className="d-flex flex-wrap gap-2">
      <StatusBadge status="PendingApproval" />
      <StatusBadge risk="High" />
      <StatusBadge risk="Critical" />
    </div>
  ),
};

export const EmptyStateInbox: Story = {
  render: () => (
    <EmptyState
      title="No requests yet"
      description="Submit your first change request to see progress here."
      icon="inbox"
      action={<button type="button" className="btn btn-primary">Submit a request</button>}
    />
  ),
};

export const ErrorStateWithRetry: Story = {
  render: () => <ErrorState message="Failed to load enhancement requests." onRetry={() => undefined} />,
};

export const LoadingStateDefault: Story = {
  render: () => <LoadingState label="Loading requests…" />,
};

export const AlertVariants: Story = {
  render: () => (
    <div className="d-flex flex-column gap-2">
      <AlertBanner variant="info" title="Heads up">
        Analysis is still running.
      </AlertBanner>
      <AlertBanner variant="success" title="Saved">
        Your changes were recorded.
      </AlertBanner>
      <AlertBanner variant="warning" title="Policy">
        This request needs admin review.
      </AlertBanner>
      <AlertBanner variant="danger" title="Failed">
        Could not submit the action.
      </AlertBanner>
    </div>
  ),
};

export const FormFieldExample: Story = {
  render: () => (
    <FormField id="story-title" label="Request title" hint="Use a short, descriptive name.">
      <input id="story-title" className="form-control" placeholder="Add export to audit log" />
    </FormField>
  ),
};

export const SectionCardExample: Story = {
  render: () => (
    <SectionCard title="Business context">
      <p className="mb-0 text-muted">Users need CSV export for compliance reporting.</p>
    </SectionCard>
  ),
};

export const ListToolbarExample: Story = {
  render: () => <ListToolbar count={42} noun="request" filterSummary="pending approval, high risk" />,
};

export const PaginationExample: Story = {
  render: () => (
    <Pagination page={2} pageSize={25} totalCount={120} onPageChange={() => undefined} onPageSizeChange={() => undefined} />
  ),
};

export const ConfirmDialogDanger: Story = {
  render: () => (
    <ConfirmDialog
      open
      title="Decline this request?"
      message="The requester will be notified."
      confirmLabel="Decline request"
      variant="danger"
      onConfirm={() => undefined}
      onCancel={() => undefined}
    />
  ),
};

export const SpaLinkInRouter: Story = {
  render: () => (
    <MemoryRouter initialEntries={['/Spa/RequestList']}>
      <p className="small text-muted mb-2">SpaLink renders router links for in-app routes.</p>
      <a href="/Spa/CreateRequest" className="btn btn-sm btn-outline-primary">
        Classic anchor (full reload off-shell)
      </a>
    </MemoryRouter>
  ),
};
