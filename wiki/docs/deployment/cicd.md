---
sidebar_position: 4
---

# CI/CD Pipelines

Guidance for building and deploying Swap apps in CI/CD.

> Detailed examples (GitHub Actions, Azure DevOps, GitLab CI) are coming soon. This page exists to satisfy internal links.

## GitHub Actions (Docs Build)

```yaml
name: Build Docs
on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  docs:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: wiki
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
      - run: npm ci
      - run: npm run build
```

## GitHub Actions (App + Docker)

```yaml
name: Build & Push Docker
on:
  push:
    branches: [ main ]

jobs:
  docker:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ghcr.io/${{ github.repository }}/myapp:latest
```
