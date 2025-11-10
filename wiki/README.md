# Swap Documentation

User-facing documentation for Swap, built with [Docusaurus](https://docusaurus.io/).

**Live Site:** https://jdtoon.github.io/swap/

## Structure

```
wiki/
├── docs/                    # Documentation content (Markdown)
│   ├── intro.md            # Homepage
│   ├── templates/          # Template guides
│   ├── getting-started/    # Installation and quickstart
│   ├── cli/                # CLI command reference
│   ├── features/           # Feature deep-dives
│   └── deployment/         # Deployment guides
├── src/                    # React components and custom pages
├── static/                 # Static assets (images, icons)
├── docusaurus.config.ts    # Site configuration
├── sidebars.ts            # Sidebar navigation
└── package.json           # Dependencies and scripts
```

## Local Development

### Prerequisites

- Node.js 18+ (LTS recommended)
- npm or yarn

### Installation

```bash
cd wiki
npm install
```

### Development Server

```bash
npm start
```

Available at **http://localhost:3000/swap/** with hot reload.

### Build for Production

```bash
npm run build
```

Generates static content in `build/` directory.

### Test Production Build

```bash
npm run serve
```

## Deployment

The site deploys to GitHub Pages at `https://jdtoon.github.io/swap/`.

**Manual deploy:**
```bash
npm run deploy
```

This builds and pushes to `gh-pages` branch.

## Writing Documentation

### Guidelines

- **Clear and concise** — target all skill levels
- **Code examples** — include working snippets with syntax highlighting
- **CLI commands** — show exact syntax with expected output
- **HTMX focus** — emphasize HTMX-first approach
- **Screenshots** — add visuals for UI features (store in `static/img/`)

### Markdown Features

Docusaurus supports enhanced markdown:

**Admonitions:**
```markdown
:::tip
This is a helpful tip!
:::

:::warning
This is a warning!
:::

:::danger
This is dangerous!
:::
```

**Code blocks with highlighting:**
```markdown
```csharp {2-4}
public class Example
{
    // These lines will be highlighted
    public string Name { get; set; }
}
``` 
```

**Tabs:**
```markdown
import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

<Tabs>
  <TabItem value="sqlite" label="SQLite">
    Code for SQLite
  </TabItem>
  <TabItem value="postgres" label="PostgreSQL">
    Code for PostgreSQL
  </TabItem>
</Tabs>
```

## File Organization

### Homepage (`intro.md`)
Main landing page. Keep it concise with quick links to key areas.

### Getting Started
Installation, prerequisites, and first project tutorials.

### CLI Reference
One page per command with syntax, options, and examples.

### Features
Deep dives into event system, testing, modularity, etc.

### Templates
Template comparisons, guides, and migration paths.

### Deployment
Docker setup and best practices.

## Sidebar Navigation

Edit `sidebars.ts` to change navigation structure:

```typescript
const sidebars: SidebarsConfig = {
  tutorialSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Getting Started',
      items: ['getting-started/installation'],
    },
  ],
};
```

## Search

Docusaurus includes built-in search powered by Lunr.js. All markdown content is automatically indexed.

## Dark Mode

Dark mode toggle is enabled by default. No configuration needed.

---

**Questions?** Open an issue on [GitHub](https://github.com/jdtoon/swap/issues)

Use `--dry-run` to preview generated files!

:::- Use relative links: `[Installation](./getting-started/installation)````bash



:::warning- For same directory: `[Overview](./overview)`

This command will overwrite existing files.

:::- For parent directory: `[Home](../intro)`npm start├── docusaurus.config.ts       # Site configuration



:::danger

Destructive operation - backup your database first!

:::## Deployment```

```



**Code blocks with syntax highlighting:**

````markdownThe site is configured to deploy to GitHub Pages:├── sidebars.ts                # Sidebar navigation│   │   ├── installation.md

```csharp

public class Product

{

    public int Id { get; set; }```bashThe site will be available at `http://localhost:3000/swap-cli/`

    public string Name { get; set; } = string.Empty;

}npm run deploy

```

```````└── package.json



**Tabs:**

```markdown

import Tabs from '@theme/Tabs';This builds and deploys to the `gh-pages` branch.### Build for Production

import TabItem from '@theme/TabItem';



<Tabs>

  <TabItem value="ps" label="PowerShell">## Content Updates```│   │   └── first-project.md```bash

    ```powershell

    swap new MyApp

    ```

  </TabItem>### Adding New Pages```bash

  <TabItem value="bash" label="Bash">

    ```bash

    swap new MyApp

    ```1. Create markdown file in `docs/`npm run build

  </TabItem>

</Tabs>2. Add front matter with `id`, `title`, `sidebar_position`

```

3. Update `sidebars.ts` if needed```

### File Organization

4. Test locally with `npm start`

**Front Matter (required for all docs):**

```markdown## Development│   └── cli/                   # CLI command referenceyarn start

---

id: generate-controller### Updating Navigation

title: Generate Controller

sidebar_position: 3This generates static content into the `build` directory.

---

Edit `sidebars.ts` to change sidebar structure.

# Generate Controller



Content here...

```### Changing Site Config



**Linking between pages:**### Serve Production Build Locally

- Same directory: `[Overview](./overview)`

- Parent directory: `[Home](../intro)`Edit `docusaurus.config.ts` for site-wide settings.

- Specific folder: `[Installation](../getting-started/installation)`

### Prerequisites│       ├── overview.md```

**Linking to code:**

- Use relative links to repository:## Versioning

  ```markdown

  See [NewCommand.cs](https://github.com/jdtoon/swap/blob/main/tools/Swap.CLI/Commands/NewCommand.cs)```bash

  ```

To create a new version:

### Adding New Pages

npm run serve

1. **Create markdown file** in appropriate `docs/` subfolder

2. **Add front matter** with `id`, `title`, `sidebar_position````bash

3. **Update `sidebars.ts`** if creating new section

4. **Test locally** with `npm start`npm run docusaurus docs:version 1.0.0```

5. **Commit and push** - Changes go live on deployment

```

Example new page:

```markdown- Node.js 18+│       ├── new.md

---

id: new-featureThis creates a snapshot in `versioned_docs/`.

title: New Feature Guide

sidebar_position: 5## Documentation Guidelines

---

## Search

# New Feature Guide

- npm or yarn

Introduction to the new feature...

```Docusaurus includes built-in search. For production, consider integrating Algolia DocSearch.



### Updating Navigation### Writing Documentation



Edit `sidebars.ts` to modify sidebar structure:## Contributing



```typescript│       ├── generate-model.mdThis command starts a local development server and opens up a browser window. Most changes are reflected live without having to restart the server.

const sidebars = {

  docsSidebar: [When adding features to Swap CLI:

    'intro',

    {- Use clear, concise language

      type: 'category',

      label: 'Getting Started',1. Update relevant documentation pages

      items: [

        'getting-started/installation',2. Add examples and code samples- Include code examples with proper syntax highlighting### Install Dependencies

        'getting-started/first-project',

      ],3. Update sidebar navigation if adding new pages

    },

    // Add new categories here4. Test documentation locally- Add command examples for CLI commands

  ],

};5. Include documentation updates in PRs

```

- Focus on HTMX patterns and integration│       └── generate-controller.md

## Content Checklist

## Resources

When adding new CLI features, update these pages:



- [ ] `cli/overview.md` - Add to command list

- [ ] `cli/<command>.md` - Create dedicated command page- [Docusaurus Documentation](https://docusaurus.io/docs)

- [ ] `intro.md` - Update feature highlights

- [ ] Root `README.md` - Update CLI reference section- [Markdown Guide](https://www.markdownguide.org/)### File Organization```bash

- [ ] `sidebars.ts` - Add to navigation

- [ ] Add examples and screenshots- [MDX Documentation](https://mdxjs.com/)



## Synchronization with README



The wiki should stay synchronized with:- `intro.md` - Main landing page for documentationnpm install├── src/                       # React components and pages## Build

- `README.md` (root) - CLI reference and quick start

- `tools/Swap.CLI/README.md` - Detailed command documentation- `getting-started/` - Installation and tutorials

- `framework/*/README.md` - Package-specific documentation

- `CHANGELOG.md` - Feature announcements- `cli/` - Command reference documentation```



**Process:**

1. Update root documentation first (README, etc.)

2. Extract relevant content to wiki pages### Links├── static/                    # Static assets

3. Add wiki-specific enhancements (screenshots, tutorials)

4. Cross-link between wiki and repository docs



## Maintenance- Use relative links: `[Installation](./getting-started/installation)`### Start Development Server



### Regular Updates- For same directory: `[Overview](./overview)`



- **On feature release:** Update command reference pages- For parent directory: `[Home](../intro)`├── docusaurus.config.ts       # Site configuration```bash

- **On version bump:** Update installation instructions

- **On pattern addition:** Update patterns documentation

- **On breaking changes:** Add migration guides

## Deployment```bash

### Quality Checks



Before deploying:

- [ ] All links work (no 404s)The site is configured to deploy to GitHub Pages:npm start├── sidebars.ts                # Sidebar navigationyarn build

- [ ] Code examples compile

- [ ] Commands run successfully

- [ ] Screenshots are up-to-date

- [ ] Search results are relevant```bash```

- [ ] Mobile view works

npm run deploy

### Build Checks

```└── package.json```

```bash

# Check for broken links

npm run build

This builds and deploys to the `gh-pages` branch.The site will be available at `http://localhost:3000/netmx/`

# Validate TypeScript

npm run typecheck

```

# Lint markdown (if configured)

npm run lint### Build for Production

```

This command generates static content into the `build` directory and can be served using any static contents hosting service.

## Troubleshooting

```bash

### Build Errors

npm run build## Development

**"Module not found":**

```bash```

rm -rf node_modules package-lock.json

npm install## Deployment

```

Output will be in the `build/` directory.

**"Duplicate routes":**

- Check for duplicate `id` in front matter### Prerequisites

- Ensure unique filenames

### Serve Production Build

**"Broken links":**

- Use relative links, not absoluteUsing SSH:

- Check link syntax: `[text](./path)`

```bash

### Development Issues

npm run serve- Node.js 18+

**Hot reload not working:**

- Stop server (Ctrl+C)```

- Clear cache: `npm run clear`

- Restart: `npm start`- npm or yarn```bash



**Search not working:**## Deployment

- Rebuild search index: `npm run build`

- Check Algolia configuration (if using)USE_SSH=true yarn deploy



**Styles not applying:**### GitHub Pages

- Clear browser cache

- Check `src/css/custom.css`### Install Dependencies```

- Restart dev server

The site is configured for deployment to GitHub Pages:

## Resources



- **Docusaurus Docs:** https://docusaurus.io/docs

- **Markdown Guide:** https://docusaurus.io/docs/markdown-features```bash

- **React Components:** https://docusaurus.io/docs/creating-pages

- **Styling:** https://docusaurus.io/docs/styling-layoutnpm run deploy```bashNot using SSH:



## Notes```



- **User-facing only:** This wiki is for end users, not framework developersnpm install

- **Keep updated:** Sync with CLI changes and releases

- **Test before deploying:** Always build locally firstThis builds the site and pushes to the `gh-pages` branch.

- **SEO optimized:** Use descriptive titles and meta descriptions

- **Accessible:** Follow accessibility best practices``````bash



---### Manual Deployment



**Related Documentation:**GIT_USER=<Your GitHub username> yarn deploy

- [README.md](../README.md) - Main project README

- [tools/Swap.CLI/README.md](../tools/Swap.CLI/README.md) - CLI internalsBuild and copy the `build/` directory to your web server.

- [docs/](../docs/) - Project-level documentation

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
