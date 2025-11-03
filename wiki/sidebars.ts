import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */
const sidebars: SidebarsConfig = {
  // Main documentation sidebar
  tutorialSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Templates',
      items: [
        'templates/overview',
        'templates/modular-monolith',
      ],
    },
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/installation',
        'getting-started/first-project',
        'getting-started/layered-solution',
      ],
    },
    {
      type: 'category',
      label: 'CLI Reference',
      items: [
        'cli/overview',
        'cli/new',
        'cli/generate-model',
        'cli/generate-controller',
        'cli/generate-relationship',
        'cli/generate-resource',
        'cli/generate-pattern',
        'cli/generate-auth',
        'cli/generate-htmx-shell',
        'cli/events',
        'cli/generate-test',
        'cli/generate-factory',
        'cli/seeders',
        'cli/database',
        'cli/utilities',
      ],
    },
    {
      type: 'category',
      label: 'Features',
      items: [
        'features/event-system',
        'features/relationships',
        'features/patterns',
        'features/testing-framework',
        'features/pagination',
        'features/search',
        'features/sorting',
        'features/filtering',
        'features/bulk-operations',
        'features/htmx-navigation',
      ],
    },
    {
      type: 'category',
      label: 'Deployment',
      items: [
        'deployment/docker',
      ],
    },
  ],
};

export default sidebars;
