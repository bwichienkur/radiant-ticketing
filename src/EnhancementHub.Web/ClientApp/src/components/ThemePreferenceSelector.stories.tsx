import type { Meta, StoryObj } from '@storybook/react';
import { ThemePreferenceSelector } from './ThemePreferenceSelector';

const meta: Meta<typeof ThemePreferenceSelector> = {
  title: 'UI/ThemePreferenceSelector',
  component: ThemePreferenceSelector,
  parameters: {
    layout: 'centered',
  },
};

export default meta;
type Story = StoryObj<typeof ThemePreferenceSelector>;

export const Default: Story = {
  render: () => (
    <>
      <div id="eh-topbar-theme-slot" />
      <ThemePreferenceSelector />
    </>
  ),
};
