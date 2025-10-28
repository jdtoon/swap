---
sidebar_position: 7
---

# swap generate factory

Generate a Bogus-based test data factory for an entity model.

## Synopsis

```bash
swap generate factory <entity> [options]
swap g factory <entity> [options]
```

## Description

Reads `Models/<Entity>.cs`, detects properties, and creates `Tests/Factories/<Entity>Factory.cs` with intelligent defaults:
- Title/Name → `f.Lorem.Sentence()`
- Description/Body/Content → `f.Lorem.Paragraphs(2)`
- Email/Phone/Url → from `f.Internet`/`f.Phone`
- Price/Amount → `f.Random.Decimal(1, 1000)`
- Dates → `f.Date.Past()`

Navigation properties are skipped. Nullable types are supported.

## Arguments

- `<entity>`: Entity name (e.g., `Post`, `TodoItem`).

## Options

- `--force` Overwrite existing file without prompting
- `-p, --project <path>` Generate in a different project directory
- `-o, --output <path>` Output folder (default: `Tests/Factories/`)

## Examples

```bash
# From project root
swap g factory Post

# In a different project
swap g factory TodoItem -p testApps/SeedersDemo

# Overwrite if exists
swap g factory TodoItem --force
```

## Notes

If `Bogus` or `Swap.Testing` are not referenced in the project, the CLI will print the commands to install them.
