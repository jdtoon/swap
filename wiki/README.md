# Swap CLI Documentation# Swap CLI Documentation# NetMX Documentation Wiki# Website



This directory contains the complete documentation for Swap CLI, built with [Docusaurus](https://docusaurus.io/).



## StructureThis directory contains the complete documentation for Swap CLI, built with [Docusaurus](https://docusaurus.io/).



```

wiki/

├── docs/                       # Documentation content## StructureThis directory contains the complete documentation for NetMX CLI, built with [Docusaurus](https://docusaurus.io/).This website is built using [Docusaurus](https://docusaurus.io/), a modern static website generator.

│   ├── intro.md               # Homepage

│   ├── getting-started/       # Installation and first project guides

│   │   ├── installation.md

│   │   └── first-project.md```

│   └── cli/                   # CLI command reference

│       ├── overview.mdwiki/

│       ├── new.md

│       ├── generate-model.md├── docs/                       # Documentation content## Structure## Installation

│       ├── generate-controller.md

│       └── generate-resource.md│   ├── intro.md               # Homepage

├── src/                       # React components and pages

├── static/                    # Static assets│   ├── getting-started/       # Installation and first project guides

├── docusaurus.config.ts       # Site configuration

├── sidebars.ts                # Sidebar navigation│   │   ├── installation.md

└── package.json

```│   │   └── first-project.md``````bash



## Development│   └── cli/                   # CLI command reference



### Prerequisites│       ├── overview.mdwiki/yarn



- Node.js 18+│       ├── new.md

- npm or yarn

│       ├── generate-model.md├── docs/                       # Documentation content```

### Install Dependencies

│       ├── generate-controller.md

```bash

npm install│       └── generate-resource.md│   ├── intro.md               # Homepage

```

├── src/                       # React components and pages

### Start Development Server

├── static/                    # Static assets│   ├── getting-started/       # Installation and first project guides## Local Development

```bash

npm start├── docusaurus.config.ts       # Site configuration

```

├── sidebars.ts                # Sidebar navigation│   │   ├── installation.md

The site will be available at `http://localhost:3000/swap-cli/`

└── package.json

### Build for Production

```│   │   └── first-project.md```bash

```bash

npm run build

```

## Development│   └── cli/                   # CLI command referenceyarn start

This generates static content into the `build` directory.



### Serve Production Build Locally

### Prerequisites│       ├── overview.md```

```bash

npm run serve

```

- Node.js 18+│       ├── new.md

## Documentation Guidelines

- npm or yarn

### Writing Documentation

│       ├── generate-model.mdThis command starts a local development server and opens up a browser window. Most changes are reflected live without having to restart the server.

- Use clear, concise language

- Include code examples with proper syntax highlighting### Install Dependencies

- Add command examples for CLI commands

- Focus on HTMX patterns and integration│       └── generate-controller.md



### File Organization```bash



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
