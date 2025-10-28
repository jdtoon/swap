---
sidebar_position: 6
---

# swap generate test

Generate an integration test class scaffold for a controller using the Swap.Testing library.

## Synopsis

```bash
swap generate test <controller> [options]
swap g test <controller> [options]
```

## Description

Creates a test class with common HTMX scenarios covered:
- Index returns partial for HTMX request
- Create/Edit forms render with correct hx-* attributes
- POST/PUT/DELETE paths return partials
- Snapshot example (`AssertMatchesSnapshotAsync`)

Files are created under `Tests/` by default.

## Arguments

- `<controller>`: Controller name with or without the `Controller` suffix (e.g., `TodoItem` or `TodoItemController`).

## Options

- `-f, --force` Overwrite existing file without prompting
- `-p, --project <path>` Generate in a different project directory (default: current directory)
- `-o, --output <path>` Output folder (default: `Tests/`)

## Examples

```bash
# Basic usage
swap g test TodoItem

# Force overwrite
swap g test TodoItem --force

# Specify project and output
swap g test Post -p testApps/SeedersDemo -o Tests
```

## See also

- [Swap.Testing: API and examples](../features/testing-framework)
