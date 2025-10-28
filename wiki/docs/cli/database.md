---
sidebar_position: 6
---

# Database Commands

Database workflow commands to streamline development with Entity Framework Core.

## Overview

The `swap database` (or `swap db`) command group provides convenient wrappers around common EF Core operations:

```bash
swap db info        # Show database configuration
swap db migrate     # Create/apply migrations
swap db seed        # Run database seeders
swap db reset       # Drop and recreate database
```

## `swap db info`

Display database configuration and migration status.

### Usage

```bash
swap db info
```

### Output

Shows a table with:
- **Project** - Project name
- **Provider** - Database provider (SQLite, SQL Server, PostgreSQL)
- **Connection** - Connection string (sensitive parts masked)
- **Migrations** - Number of migrations
- **Last Migration** - Name of the most recent migration
- **Seeders** - Seeder configuration status

### Example

```bash
PS C:\MyApp> swap db info

╭────────────────┬─────────────────────────────────────╮
│ Project        │ MyApp                               │
│ Provider       │ SQLite                              │
│ Connection     │ Data Source=MyApp.db                │
│ Migrations     │ 3                                   │
│ Last Migration │ AddProductTable                     │
│ Seeders        │ Configured                          │
╰────────────────┴─────────────────────────────────────╯
```

## `swap db migrate [name] [--apply]`

Create and/or apply Entity Framework Core migrations.

### Usage

```bash
# Create a new migration
swap db migrate AddProductTable

# Create and apply immediately
swap db migrate AddProductTable --apply

# Apply all pending migrations (no name argument)
swap db migrate --apply
```

### Options

- `name` - Optional migration name. If omitted with `--apply`, applies pending migrations.
- `--apply` or `-a` - Apply the migration after creating it (or apply pending if no name given)

### Examples

```bash
# Create migration after adding a new entity
swap db migrate AddCustomerEntity

# Create and apply in one step
swap db migrate AddOrdersTable --apply

# Just apply what's pending
swap db migrate --apply
```

### What It Does

**Without `--apply`:**
1. Runs `dotnet ef migrations add <name>`
2. Creates migration files in `Migrations/`

**With `--apply`:**
1. Creates the migration (if name provided)
2. Runs `dotnet ef database update`
3. Applies changes to the database

## `swap db seed [options]`

Run database seeders via application startup.

### Usage

```bash
# Run with default settings
swap db seed

# Customize seeding parameters
swap db seed --count 100 --locale en_GB --if-empty
```

### Options

- `--count` or `-c` - Number of records per seeder (default: 50)
- `--locale` or `-l` - Bogus locale for fake data generation (default: "en")
- `--if-empty` - Only seed tables that are empty

### Examples

```bash
# Seed with defaults (50 records, "en" locale)
swap db seed

# Generate 200 records with British English locale
swap db seed --count 200 --locale en_GB

# Only seed if tables are empty (safe for repeated runs)
swap db seed --if-empty

# Combine options
swap db seed --count 100 --locale de --if-empty
```

### How It Works

Sets environment variables and runs your application:

```bash
SEED_COUNT=100 SEED_LOCALE=en_GB SEED_IFEMPTY=true dotnet run
```

Your application's seeding logic (in `Data/Seeders/SeedRunner.cs`) reads these variables and seeds accordingly.

## `swap db reset [--force]`

Drop and recreate the database for a fresh start. **Destroys all data!**

### Usage

```bash
# With confirmation prompt
swap db reset

# Skip confirmation (use in scripts)
swap db reset --force
```

### Options

- `--force` or `-f` - Skip the confirmation prompt

### Warning

⚠️ This command:
1. Drops the entire database (`dotnet ef database drop --force`)
2. Recreates it from migrations (`dotnet ef database update`)
3. **Deletes all existing data**

Use with caution! Great for development, dangerous in production.

### Examples

```bash
# Interactive - asks for confirmation
swap db reset

# Non-interactive - use in automation
swap db reset --force
```

## Common Workflows

### Starting a New Feature

```bash
# 1. Generate the resource
swap g r Product --fields "Name:string Price:decimal Stock:int"

# 2. Create and apply migration
swap db migrate AddProduct --apply

# 3. Generate seeder
swap g s Product

# 4. Seed the database
swap db seed --count 50
```

### Resetting Development Data

```bash
# Drop everything and start fresh
swap db reset --force

# Seed with fresh data
swap db seed --count 100
```

### Checking Migration Status

```bash
# See what's configured
swap db info

# Apply any pending migrations
swap db migrate --apply
```

## Tips

- Use `swap db info` frequently to check your database state
- Always create meaningful migration names: `AddUserAuthentication` not `Update1`
- Use `--apply` to save a step during rapid development
- Use `swap db reset` liberally in development - embrace fresh starts
- Use `--if-empty` with seeders to avoid duplicate data
- Combine with `swap list` to see which entities have seeders configured
