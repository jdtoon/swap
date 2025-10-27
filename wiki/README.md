# Swap CLI Documentation# Swap CLI Documentation



This directory contains the complete documentation for Swap CLI, built with [Docusaurus](https://docusaurus.io/).This directory contains the complete documentation for Swap CLI, built with [Docusaurus](https://docusaurus.io/).



## Structure## Structure



```

wiki/

├── docs/                       # Documentation content```

│   ├── intro.md               # Homepage

│   ├── getting-started/       # Installation and first project guideswiki/

│   │   ├── installation.md

│   │   └── first-project.md├── docs/                       # Documentation content## StructureThis directory contains the complete documentation for NetMX CLI, built with [Docusaurus](https://docusaurus.io/).This website is built using [Docusaurus](https://docusaurus.io/), a modern static website generator.

│   └── cli/                   # CLI command reference

│       ├── overview.md│   ├── intro.md               # Homepage

│       ├── new.md

│       ├── generate-model.md│   ├── getting-started/       # Installation and first project guides

│       ├── generate-controller.md

│       └── generate-resource.md│   │   ├── installation.md

├── src/                       # React components and pages

├── static/                    # Static assets│   │   └── first-project.md```

├── docusaurus.config.ts       # Site configuration

├── sidebars.ts                # Sidebar navigation│   └── cli/                   # CLI command reference

└── package.json

```│       ├── overview.mdwiki/



## Development│       ├── new.md



### Prerequisites│       ├── generate-model.md├── docs/                       # Documentation content## Structure## Installation



- Node.js 18+│       ├── generate-controller.md

- npm or yarn

│       └── generate-resource.md│   ├── intro.md               # Homepage

### Install Dependencies

├── src/                       # React components and pages

```bash

npm install├── static/                    # Static assets│   ├── getting-started/       # Installation and first project guides

```

├── docusaurus.config.ts       # Site configuration

### Start Development Server

├── sidebars.ts                # Sidebar navigation│   │   ├── installation.md

```bash

npm start└── package.json

```

```│   │   └── first-project.md``````bash

The site will be available at `http://localhost:3000/swap-cli/`



### Build for Production

## Development│   └── cli/                   # CLI command reference

```bash

npm run build

```

### Prerequisites│       ├── overview.mdwiki/yarn

This generates static content into the `build` directory.



### Serve Production Build Locally

- Node.js 18+│       ├── new.md

```bash

npm run serve- npm or yarn

```

│       ├── generate-model.md├── docs/                       # Documentation content```

## Documentation Guidelines

### Install Dependencies

### Writing Documentation

│       ├── generate-controller.md

- Use clear, concise language

- Include code examples with proper syntax highlighting```bash

- Add command examples for CLI commands

- Focus on HTMX patterns and integrationnpm install│       └── generate-resource.md│   ├── intro.md               # Homepage



### File Organization```



- `intro.md` - Main landing page for documentation├── src/                       # React components and pages

- `getting-started/` - Installation and tutorials

- `cli/` - Command reference documentation### Start Development Server



### Links├── static/                    # Static assets│   ├── getting-started/       # Installation and first project guides## Local Development



- Use relative links: `[Installation](./getting-started/installation)````bash

- For same directory: `[Overview](./overview)`

- For parent directory: `[Home](../intro)`npm start├── docusaurus.config.ts       # Site configuration



## Deployment```



The site is configured to deploy to GitHub Pages:├── sidebars.ts                # Sidebar navigation│   │   ├── installation.md



```bashThe site will be available at `http://localhost:3000/swap-cli/`

npm run deploy

```└── package.json



This builds and deploys to the `gh-pages` branch.### Build for Production



## Content Updates```│   │   └── first-project.md```bash



### Adding New Pages```bash



1. Create markdown file in `docs/`npm run build

2. Add front matter with `id`, `title`, `sidebar_position`

3. Update `sidebars.ts` if needed```

4. Test locally with `npm start`

## Development│   └── cli/                   # CLI command referenceyarn start

### Updating Navigation

This generates static content into the `build` directory.

Edit `sidebars.ts` to change sidebar structure.



### Changing Site Config

### Serve Production Build Locally

Edit `docusaurus.config.ts` for site-wide settings.

### Prerequisites│       ├── overview.md```

## Versioning

```bash

To create a new version:

npm run serve

```bash

npm run docusaurus docs:version 1.0.0```

```

- Node.js 18+│       ├── new.md

This creates a snapshot in `versioned_docs/`.

## Documentation Guidelines

## Search

- npm or yarn

Docusaurus includes built-in search. For production, consider integrating Algolia DocSearch.

### Writing Documentation

## Contributing

│       ├── generate-model.mdThis command starts a local development server and opens up a browser window. Most changes are reflected live without having to restart the server.

When adding features to Swap CLI:

- Use clear, concise language

1. Update relevant documentation pages

2. Add examples and code samples- Include code examples with proper syntax highlighting### Install Dependencies

3. Update sidebar navigation if adding new pages

4. Test documentation locally- Add command examples for CLI commands

5. Include documentation updates in PRs

- Focus on HTMX patterns and integration│       └── generate-controller.md

## Resources



- [Docusaurus Documentation](https://docusaurus.io/docs)

- [Markdown Guide](https://www.markdownguide.org/)### File Organization```bash

- [MDX Documentation](https://mdxjs.com/)



- `intro.md` - Main landing page for documentationnpm install├── src/                       # React components and pages## Build

- `getting-started/` - Installation and tutorials

- `cli/` - Command reference documentation```



### Links├── static/                    # Static assets



- Use relative links: `[Installation](./getting-started/installation)`### Start Development Server

- For same directory: `[Overview](./overview)`

- For parent directory: `[Home](../intro)`├── docusaurus.config.ts       # Site configuration```bash



## Deployment```bash



The site is configured to deploy to GitHub Pages:npm start├── sidebars.ts                # Sidebar navigationyarn build



```bash```

npm run deploy

```└── package.json```



This builds and deploys to the `gh-pages` branch.The site will be available at `http://localhost:3000/netmx/`


```

### Build for Production

This command generates static content into the `build` directory and can be served using any static contents hosting service.

```bash

npm run build## Development

```

## Deployment

Output will be in the `build/` directory.

### Prerequisites

### Serve Production Build

Using SSH:

```bash

npm run serve- Node.js 18+

```

- npm or yarn```bash

## Deployment

USE_SSH=true yarn deploy

### GitHub Pages

### Install Dependencies```

The site is configured for deployment to GitHub Pages:



```bash

npm run deploy```bashNot using SSH:

```

npm install

This builds the site and pushes to the `gh-pages` branch.

``````bash

### Manual Deployment

GIT_USER=<Your GitHub username> yarn deploy

Build and copy the `build/` directory to your web server.

### Start Development Server```

## Writing Documentation



### File Structure

```bashIf you are using GitHub pages for hosting, this command is a convenient way to build the website and push to the `gh-pages` branch.

- Each markdown file becomes a page

- Use frontmatter for metadata:npm start

```

```md

---The site will be available at `http://localhost:3000/netmx/`

sidebar_position: 1

title: Page Title### Build for Production

---

```bash

# Contentnpm run build

``````



### Code BlocksOutput will be in the `build/` directory.



Support for many languages with syntax highlighting:### Serve Production Build



````md```bash

```bashnpm run serve

swap new MyApp```

```

## Deployment

```csharp

public class Product### GitHub Pages

{

    public int Id { get; set; }The site is configured for deployment to GitHub Pages:

}

``````bash

````npm run deploy

```

### Admonitions

This builds the site and pushes to the `gh-pages` branch.

```md

:::tip### Manual Deployment

Helpful tip here

:::Build and copy the `build/` directory to your web server.



:::warning## Writing Documentation

Important warning

:::### File Structure



:::danger- Each markdown file becomes a page

Critical information- Use frontmatter for metadata:

:::

``````md

---

### Internal Linkssidebar_position: 1

title: Page Title

```md---

[Link Text](./other-page)

[Link to Section](./page#section-id)# Content

``````



## Maintenance### Code Blocks



### Adding New PagesSupport for many languages with syntax highlighting:



1. Create markdown file in appropriate directory````md

2. Add frontmatter with `sidebar_position````bash

3. Update `sidebars.ts` if neededswap new MyApp

4. Test locally with `npm start````



### Updating Navigation```csharp

public class Product

Edit `sidebars.ts` to change sidebar structure.{

    public int Id { get; set; }

### Changing Site Config}

```

Edit `docusaurus.config.ts` for site-wide settings.````



## Versioning### Admonitions



To create a new version:```md

:::tip

```bashHelpful tip here

npm run docusaurus docs:version 1.0.0:::

```

:::warning

This creates a snapshot in `versioned_docs/`.Important warning

:::

## Search

:::danger

Docusaurus includes built-in search. For production, consider integrating Algolia DocSearch.Critical information

:::

## Contributing```



When adding features to Swap CLI:### Internal Links



1. Update relevant documentation pages```md

2. Add examples and code samples[Link Text](./other-page)

3. Update sidebar navigation if adding new pages[Link to Section](./page#section-id)

4. Test documentation locally```

5. Include documentation updates in PRs

## Maintenance

## Resources

### Adding New Pages

- [Docusaurus Documentation](https://docusaurus.io/docs)

- [Markdown Guide](https://www.markdownguide.org/)1. Create markdown file in appropriate directory

- [MDX Documentation](https://mdxjs.com/)2. Add frontmatter with `sidebar_position`

3. Update `sidebars.ts` if needed
4. Test locally with `npm start`

### Updating Navigation

Edit `sidebars.ts` to change sidebar structure.

### Changing Site Config

Edit `docusaurus.config.ts` for site-wide settings.

## Versioning

To create a new version:

```bash
npm run docusaurus docs:version 1.0.0
```

This creates a snapshot in `versioned_docs/`.

## Search

Docusaurus includes built-in search. For production, consider integrating Algolia DocSearch.

## Contributing

When adding features to NetMX CLI:

1. Update relevant documentation pages
2. Add examples and code samples
3. Update sidebar navigation if adding new pages
4. Test documentation locally
5. Include documentation updates in PRs

## Resources

- [Docusaurus Documentation](https://docusaurus.io/docs)
- [Markdown Guide](https://www.markdownguide.org/)
- [MDX Documentation](https://mdxjs.com/)
