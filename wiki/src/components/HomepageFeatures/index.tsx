import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Architecture Templates',
    icon: '🏗️',
    description: (
      <>
        Pick the right starting point: Monolith for rapid DX or Layered for clean boundaries.
        Both are HTMX-native and ship with the Swap Event System and real integration tests.
        <br/>
        <a href="/docs/templates/overview">Learn more →</a>
      </>
    ),
  },
  {
    title: 'HTMX Simplicity',
    icon: '🎯',
    description: (
      <>
        Build modern, interactive web applications without JavaScript frameworks.
        Server-rendered HTML with HTMX for dynamic updates, plus a simple UI event bus to manage front-end updates.
      </>
    ),
  },
  {
    title: 'DaisyUI + Tailwind',
    icon: '🎨',
    description: (
      <>
        Beautiful, accessible components out of the box. Built on ASP.NET Core
        with Entity Framework Core. Supports SQLite, SQL Server, and PostgreSQL.
      </>
    ),
  },
];

function Feature({title, icon, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <div className={styles.featureIcon}>{icon}</div>
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
