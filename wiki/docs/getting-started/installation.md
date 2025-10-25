---
sidebar_position: 1
---

# Installation

Install Swap CLI and set up your development environment.

## Prerequisites

- **.NET 9.0 SDK or later** - [Download here](https://dotnet.microsoft.com/download)
- **Git** - For version control
- **Visual Studio 2022**, **VS Code**, or **Rider** - Recommended IDEs

### Verify .NET Installation

```bash
dotnet --version
# Should show 9.0.0 or later
```

## Install the CLI

### Global Installation

Install as a global .NET tool:

```bash
dotnet tool install -g Swap.CLI
```

Verify the installation:

```bash
swap --version
```

### Update the CLI

Update to the latest version:

```bash
dotnet tool update -g Swap.CLI
```

### Uninstall the CLI

```bash
dotnet tool uninstall -g Swap.CLI
```

## Local Development Installation

For contributors or local builds:

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/swap-cli.git
cd swap-cli
```

### 2. Build the CLI

```bash
cd tools/Swap.CLI
dotnet build
```

### 3. Install Locally

```bash
dotnet tool install --global --add-source ./bin/Debug Swap.CLI
```

Or use the convenience script:

```bash
# PowerShell
.\scripts\reinstall-cli.ps1
```

## Database Providers

Swap generates projects with SQLite by default. To use other providers:

### SQL Server

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Update connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyApp;Trusted_Connection=true;"
}
```

### PostgreSQL

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

Update connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=myapp;Username=postgres;Password=password"
}
```

## Troubleshooting

### CLI Not Found After Installation

Add the .NET tools directory to your PATH:

**Windows:**
```powershell
$env:Path += ";$env:USERPROFILE\.dotnet\tools"
```

**macOS/Linux:**
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Permission Denied on macOS/Linux

```bash
sudo dotnet tool install -g Swap.CLI
```

### Old Version Persisting

```bash
dotnet tool uninstall -g Swap.CLI
dotnet tool install -g Swap.CLI
```

## Next Steps

- [Your First Project](./first-project) - Build a simple CRUD app
- [CLI Reference](../cli/overview) - Complete command documentation
