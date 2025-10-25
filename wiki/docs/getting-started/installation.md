---
sidebar_position: 1
---

# Installation

This guide will help you install Swap CLI and set up your development environment.

## Prerequisites

Before installing Swap CLI, ensure you have:

- **.NET 9.0 SDK or later** - [Download here](https://dotnet.microsoft.com/download)
- **Node.js 18+ (optional)** - Required only if building HTMX/frontend components
- **Git** - For version control
- **Visual Studio 2022**, **VS Code**, or **Rider** - Recommended IDEs

### Verify .NET Installation

```bash
dotnet --version
# Should show 9.0.0 or later
```

## Install the CLI

### Global Installation (Recommended)

Install the CLI as a global .NET tool:

```bash
dotnet tool install -g NetMX.CLI
```

Verify the installation:

```bash
swap --version
```

### Update the CLI

To update to the latest version:

```bash
dotnet tool update -g NetMX.CLI
```

### Uninstall the CLI

```bash
dotnet tool uninstall -g NetMX.CLI
```

## Local Development Installation

If you're contributing to NetMX or want to use a local build:

### 1. Clone the Repository

```bash
git clone https://github.com/toonjd/netmx.git
cd netmx
```

### 2. Build the CLI

```bash
cd tools/NetMX.CLI
dotnet build
```

### 3. Install Locally

```bash
dotnet tool install --global --add-source ./bin/Debug NetMX.CLI
```

Or use the convenience script:

```bash
# From repository root
.\scripts\reinstall-cli.ps1
```

## Database Providers

NetMX supports multiple database providers. Install the Entity Framework Core tools:

```bash
dotnet tool install -g dotnet-ef
```

Verify installation:

```bash
dotnet ef --version
```

## IDE Extensions (Optional)

### Visual Studio Code

Recommended extensions:

- **C# Dev Kit** - Official Microsoft C# support
- **C#** - IntelliSense and debugging
- **.NET Core Test Explorer** - Run tests from the editor
- **GitHub Copilot** - AI-powered code completion

### Visual Studio 2022

- Install the latest version with ASP.NET and web development workload
- Entity Framework Core Power Tools (optional, for advanced EF Core features)

## Troubleshooting

### Command not found

If `swap` command is not recognized after installation:

**Windows (PowerShell):**
```powershell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

**Linux/macOS:**
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

Add the above to your shell profile (`~/.bashrc`, `~/.zshrc`, etc.) to persist.

### Permission Issues

**Windows:**
- Run PowerShell as Administrator
- Or use: `dotnet tool install -g NetMX.CLI --tool-path C:\Tools`

**Linux/macOS:**
```bash
sudo dotnet tool install -g NetMX.CLI
```

### Version Conflicts

If you have multiple .NET SDK versions:

```bash
# List installed SDKs
dotnet --list-sdks

# Create global.json to pin version
dotnet new globaljson --sdk-version 9.0.100
```

## Next Steps

Now that you have the CLI installed, let's create your first project:

- [Your First Project](./first-project) - Build a simple application
- [CLI Overview](../cli/overview) - Learn about available commands
