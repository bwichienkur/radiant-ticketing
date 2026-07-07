import { SpaLink } from '../components/SpaLink';
import { PageHeader } from '../components/ui';

const PORTFOLIO_LINKS = [
  {
    href: '/Spa/Applications',
    title: 'Applications',
    description: 'Registered systems, teams, and linked repositories.',
  },
  {
    href: '/Spa/SystemMap',
    title: 'System map',
    description: 'Interactive dependency graph across your portfolio.',
  },
  {
    href: '/Spa/Repositories',
    title: 'Repositories',
    description: 'Index status, branches, and re-index actions.',
  },
  {
    href: '/Spa/DatabaseConnections',
    title: 'Databases',
    description: 'Connections, schema scans, and ERD views.',
  },
  {
    href: '/Spa/SchemaDrift',
    title: 'Schema drift',
    description: 'Compare live databases to indexed EF mappings.',
  },
  {
    href: '/Spa/Documentation/Export',
    title: 'Documentation export',
    description: 'Generate portfolio documentation packages.',
  },
  {
    href: '/Spa/Refactor/Analyze',
    title: 'Refactor analysis',
    description: 'Blast-radius analysis before large changes.',
  },
  {
    href: '/Spa/Refactor/Plans',
    title: 'Refactor plans',
    description: 'Track planned structural improvements.',
  },
] as const;

export function PortfolioHubApp() {
  return (
    <div>
      <PageHeader
        title="Portfolio"
        description="System intelligence for your application estate — map, index, and govern technical context."
      />

      <div className="eh-hub-grid">
        {PORTFOLIO_LINKS.map((link) => (
          <SpaLink key={link.href} href={link.href} className="eh-hub-card">
            <h2 className="eh-hub-card-title">{link.title}</h2>
            <p className="eh-hub-card-description">{link.description}</p>
            <span className="eh-hub-card-cta">Open →</span>
          </SpaLink>
        ))}
      </div>
    </div>
  );
}
