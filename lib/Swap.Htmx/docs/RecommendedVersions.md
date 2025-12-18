# Recommended Client Versions

This project’s templates, demos, and documentation assume the following pinned client-side versions.

These values matter because subtle client-version differences can change HTMX event behavior and extension semantics.

## Canonical versions

- **htmx**: `2.0.8`
- **htmx SSE extension** (`htmx-ext-sse`): `2.2.4`
- **htmx WebSocket extension** (`htmx-ext-ws`): `2.0.4`

## Where these versions are used

- **Templates**: `templates/content/**/libman*.json`
- **Demos**: `demo/**/libman.json` and layout files referencing CDN scripts
- **Docs**: See [ClientAssets.md](ClientAssets.md) for the two supported strategies (CDN vs LibMan).

## Updating versions

If you update any of these versions:

1. Update this doc first.
2. Update templates and demos.
3. Update docs snippets.
4. Verify with a full test run (`dotnet test -c Release`).
