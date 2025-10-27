# Seeders

Generate realistic database seeders using Bogus.

## Usage

- Single entity:
  - `swap g seed <entity> [--count 50] [--locale en] [--if-empty] [--append]`
- All entities declared in your `AppDbContext`:
  - `swap g seed all [--count 50] [--locale en] [--if-empty] [--append]`

Examples:

```bash
# Generate a seeder for Post with 100 records, English locale, only if empty
swap g seed Post --count 100 --locale en --if-empty

# Generate seeders for all DbSets in AppDbContext
swap g seed all --count 200 --locale en_GB --if-empty
```

## What it generates

- `Data/Seeders/<Entity>Seeder.cs` with Bogus rules per field
- Ensures `Data/Seeders/SeedRunner.cs` exists and registers your seeder calls
- Automatically adds `Bogus` to your project if missing
- Program hook (Development) runs `SeedRunner.RunAsync(...)` on startup

## Options

- `--count` (default: 50): Number of records to generate
- `--locale` (default: `en`): Bogus locale (e.g., `en`, `en_GB`, `de`)
- `--if-empty`: Only seed when the table is empty (idempotent)
- `--append`: Append without clearing existing records (default behavior)

## Development startup seeding

In Development, seeding is controlled by environment variables:

- `SEED_COUNT` (default: `50`)
- `SEED_LOCALE` (default: `en`)
- `SEED_IFEMPTY` (default: `true`)

These are read in `Program.cs` and passed to `SeedRunner.RunAsync`.

## Field heuristics

The generator produces sensible defaults based on property names and types:

- string: Internet.Email/Url/UserName, Name/Title/Description, Phone/Address/City/State/Country/Zip, Slug/Image, fallback Lorem
- numeric: Int/Long/Float/Double/Decimal with reasonable ranges; finance amounts for price/total
- bool/date: Weighted probabilities (e.g., IsActive ~70% true), dates within the last 3 years
- foreign keys: `FooId` will pick from existing `db.Foos` ids; ensure related entities are seeded first
- nullable: ~20% chance to be null for `?` types

## Determinism

Use the `--locale` option and (optionally in future) a fixed random seed to reproduce data. Locale affects string formats and names. A `--deterministic-seed` option is planned.

## Tips

- For foreign keys (e.g., `AuthorId` on `Post`), generate and seed the related entity first:
  - `swap g r Author --fields "Name:string Email:string"`
  - `swap g seed Author --count 25 --if-empty`
  - then `swap g seed Post --count 100 --if-empty`
- If you don’t have npm/libman locally, you can generate projects with `swap new <name> --skip-setup` and add migrations manually.

## Troubleshooting

- Build fails with npm errors: the project template runs `npm run build:css` during build. Install Node.js/npm or temporarily comment out the `BuildCSS` target in your `.csproj` for CI/tests.
- No DbSet for foreign key: ensure the related entity exists (e.g., `Author` for `AuthorId`) or adjust the model.
