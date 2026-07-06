import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { CommandPalette } from './CommandPalette';

const meta: Meta<typeof CommandPalette> = {
  title: 'UI/CommandPalette',
  component: CommandPalette,
};

export default meta;
type Story = StoryObj<typeof CommandPalette>;

export const Closed: Story = {
  render: () => (
    <MemoryRouter>
      <CommandPalette />
    </MemoryRouter>
  ),
};
