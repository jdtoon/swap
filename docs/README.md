# Documentation

This folder contains comprehensive documentation for the Swap CLI project.

## Overview

The `docs/` directory holds all project-level documentation including architecture guides, planning documents, and historical context.

## Core Documentation

### Architecture & Patterns

#### `CONTAINER-ARCHITECTURE.md` ⭐ **START HERE**
**The foundational HTMX pattern for Swap CLI.**

Explains the "container architecture" pattern:
- How HTMX swaps work with `hx-target` and `hx-swap`
- Full-page vs. partial rendering
- Layout coordination (`_Layout.cshtml`, `id="main-content"`)
- When to use `_ViewStart.cshtml` vs. `Layout = null`

**This is the most important document for understanding how Swap generates HTMX views.**

#### `DEVELOPER-EXPERIENCE.md`
**Best practices and DX philosophy.**

Covers:
- CLI-driven workflow
- Code generation philosophy
- Developer ergonomics
- Patterns that make Swap intuitive

### Project Planning

#### `VERSION-0.2.0-PLAN.md`
**Complete roadmap for v0.2.0 release.**

Comprehensive planning document including:
- Version 0.1.0 summary (what shipped)
- Version 0.2.0 goals (relationship generation)
- Priority order (Phase 1: Docs/Refactor, Phase 2: Relationships, Phase 3: Blog Template)
- Detailed scope for each phase
- Success criteria and timeline
- Known issues from 0.1.0

**Current focus:** Documentation audit, codebase refactor, relationship generation

#### `PROJECT-SYNOPSIS.md`
**High-level project overview.**

Summary of:
- What Swap CLI is
- Target audience
- Key features
- Technology stack

#### `THE-PRODUCT.md`
**Product vision and positioning.**

Defines:
- Product goals
- Competitive landscape
- Unique value proposition
- Target use cases

## Archive

### `archive/`
Historical documentation preserved for reference:

#### `PATTERNS-LIBRARY.md`
**30+ proven HTMX patterns from production.**

Collection of real-world patterns:
- Modal editing
- Inline editing
- Pagination
- Filtering/sorting
- Bulk actions
- Toast notifications

**Status:** Some patterns integrated into generators, others serve as reference.

#### `TEST-RESULTS-SUMMARY.md`
Historical test run summaries from earlier development phases.

## Documentation Strategy

### Structure
```
docs/
├── CONTAINER-ARCHITECTURE.md  ← Foundational HTMX pattern
├── DEVELOPER-EXPERIENCE.md    ← DX philosophy
├── PROJECT-SYNOPSIS.md         ← Project overview
├── THE-PRODUCT.md              ← Product vision
├── VERSION-0.2.0-PLAN.md       ← Current roadmap
└── archive/                    ← Historical docs
    ├── PATTERNS-LIBRARY.md
    └── TEST-RESULTS-SUMMARY.md
```

### Living Documents
These documents are actively maintained:
- ✅ `VERSION-0.2.0-PLAN.md` - Updated as we progress
- ✅ `CONTAINER-ARCHITECTURE.md` - Core pattern reference
- ✅ `DEVELOPER-EXPERIENCE.md` - Evolving best practices

### Reference Documents
These documents are historical but valuable:
- 📚 `PROJECT-SYNOPSIS.md` - Original vision
- 📚 `THE-PRODUCT.md` - Product positioning
- 📚 `archive/PATTERNS-LIBRARY.md` - Pattern catalog

## Documentation Hierarchy

For complete project documentation:

1. **Getting Started** → [README.md](../README.md)
2. **Architecture** → `CONTAINER-ARCHITECTURE.md` (this folder)
3. **API Reference** → [tools/Swap.CLI/README.md](../tools/Swap.CLI/README.md)
4. **Framework Packages:**
   - [framework/Swap.Htmx/README.md](../framework/Swap.Htmx/README.md)
   - [framework/Swap.Patterns/README.md](../framework/Swap.Patterns/README.md)
   - [framework/Swap.Testing/README.md](../framework/Swap.Testing/README.md)
5. **Contributing** → [CONTRIBUTING.md](../CONTRIBUTING.md)
6. **Changelog** → [CHANGELOG.md](../CHANGELOG.md)
7. **Wiki** → [wiki/](../wiki/) (Docusaurus site)

## Reading Order for New Contributors

1. **Start here:** [README.md](../README.md) - Quick start and feature overview
2. **Understand HTMX:** `CONTAINER-ARCHITECTURE.md` - How Swap uses HTMX
3. **Learn philosophy:** `DEVELOPER-EXPERIENCE.md` - Why Swap is designed this way
4. **Check roadmap:** `VERSION-0.2.0-PLAN.md` - What we're building
5. **Dive into code:** [CONTRIBUTING.md](../CONTRIBUTING.md) - Development workflow

## Notes

- **Wiki vs. Docs:** The `wiki/` folder contains the Docusaurus site (user-facing). This `docs/` folder is for project-level documentation (architecture, planning).
- **Active maintenance:** Documents in this folder are kept in sync with codebase changes.
- **Archived content:** Historical docs moved to `archive/` when superseded but kept for reference.

---

**Related Documentation:**
- [wiki/README.md](../wiki/README.md) - User-facing documentation site
- [README.md](../README.md) - Main project README
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Development guide
