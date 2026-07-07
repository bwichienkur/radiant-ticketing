import type { Meta, StoryObj } from '@storybook/react';
import { ResponsiveDataList } from './ui/ResponsiveDataList';

type DemoItem = { id: string; name: string; domain: string; repos: number };

const meta: Meta<typeof ResponsiveDataList<DemoItem>> = {
  title: 'UI/ResponsiveDataList',
  component: ResponsiveDataList,
};

export default meta;
type Story = StoryObj<typeof ResponsiveDataList<DemoItem>>;

export const Default: Story = {
  args: {
    items: [
      { id: '1', name: 'Radiant Commerce', domain: 'E-commerce', repos: 3 },
      { id: '2', name: 'Billing API', domain: 'Finance', repos: 1 },
    ],
    getRowKey: (item) => item.id,
    columns: [
      { id: 'name', header: 'Name', cell: (item) => item.name },
      { id: 'domain', header: 'Domain', cell: (item) => item.domain },
      { id: 'repos', header: 'Repos', cell: (item) => item.repos },
    ],
    renderMobileCard: (item) => (
      <>
        <div className="mobile-data-card-title">{item.name}</div>
        <div className="mobile-data-card-row">
          <span>{item.domain}</span>
        </div>
      </>
    ),
  },
};
