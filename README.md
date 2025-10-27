# Swap CLI

[![Build Status](https://github.com/toonjd/swap/actions/workflows/ci-build.yml/badge.svg)](https://github.com/toonjd/swap/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)

**Generate production-ready ASP.NET Core + HTMX applications with beautiful DaisyUI components.**

Swap CLI is a code generator that creates complete, modern web applications using ASP.NET Core MVC, HTMX for interactivity, DaisyUI for UI components, and Entity Framework Core for data access. Generate full CRUD operations with pagination, search, sorting, filtering, and modal-based editing in seconds.

## 🌟 Why Swap?

- **⚡ Production-Ready Code** - Generate complete CRUD with modals, pagination, sorting, filtering, and search
- **🎯 HTMX Simplicity** - Modern, interactive web apps without JavaScript frameworks
- **� DaisyUI + Tailwind** - Beautiful, accessible components out of the box
- **🗄️ Entity Framework Core** - Full database integration with migrations support
- **💻 Developer Experience** - CLI-driven workflow, no manual boilerplate
- **📦 Proven Patterns** - Every pattern extracted from real production applications

## 🚀 Quick Start

### Installation

```bash
# Install the Swap CLI tool
dotnet tool install --global Swap.CLI --prerelease

# Verify installation
swap --version
```

### Create Your First Project

```bash
# Create a new ASP.NET Core + HTMX application
swap new MyApp
cd MyApp

# Apply migrations and run
dotnet ef database update
dotnet run
```

Visit `http://localhost:5000` - Your HTMX-powered application is running! 🎉

### Generate Your First CRUD

```bash
# Generate a complete CRUD controller with all features
swap generate controller Product --fields "Name:string Price:decimal InStock:bool:f"

# Short alias
swap g c Product --fields "Name:string Price:decimal InStock:bool:f"

# Update database
dotnet ef migrations add AddProduct
dotnet ef database update
```

Visit `http://localhost:5000/Product` - Full CRUD with pagination, search, sorting, and filtering! 🚀

**No manual file creation. No boilerplate. Just CLI commands and business logic.**

## 🎯 What You Get

### Complete Feature Set

Every generated controller includes:

- ✅ **CRUD Operations** - Create, Read, Update, Delete via HTMX modals
- ✅ **Pagination** - Configurable page sizes (10, 25, 50, 100)
- ✅ **Real-Time Search** - 500ms debounced search across fields
- ✅ **Column Sorting** - Ascending/descending toggle per field
- ✅ **Boolean Filtering** - Dropdown filters (All/Yes/No) for bool fields
- ✅ **Bulk Operations** - Select multiple items and bulk delete
- ✅ **Toast Notifications** - Success/error messages with DaisyUI alerts
- ✅ **Modal Editing** - No page reloads, smooth UX
- ✅ **Validation** - Client and server-side with clear error messages
- ✅ **Responsive Design** - Works perfectly on mobile and desktop

### Generated Stack

- **Backend:** ASP.NET Core 9.0 MVC
- **Frontend:** HTMX + DaisyUI + Tailwind CSS
- **Database:** Entity Framework Core (SQLite, SQL Server, PostgreSQL)
- **UI Library:** DaisyUI 4.x components
- **Styling:** Tailwind CSS 3.x utilities

## 📋 CLI Commands

### `swap new <name>`

Create a new ASP.NET Core + HTMX application with DaisyUI components.

```bash
swap new MyApp
```

**Generates:**
- Complete ASP.NET Core MVC project structure
- Entity Framework Core with SQLite (configurable)
- DaisyUI + Tailwind CSS configuration
- Sample TodoItem model and CRUD
- Database migrations
- Ready to run immediately

### `swap generate controller <name> --fields <fields>`

Generate a complete CRUD controller with all features.

```bash
# Generate Product controller with fields
swap g c Product --fields "Name:string Price:decimal InStock:bool:f"

# With nullable fields
swap g c Customer --fields "Name:string Email:string Notes:string?"

# Control sorting and filtering per field
swap g c Order --fields "OrderNumber:string:ns Total:decimal Date:DateTime Status:bool:f"
```

**Field Flags:**
- `:sortable` or `:s` - Enable sorting (default for all fields)
- `:nosort` or `:ns` - Disable sorting
- `:filterable` or `:f` - Enable filtering (bool fields only)

**Generates:**
- Controller with full CRUD operations
- Model class with validation
- View model for list operations
- Views (Index, _List, _CreateModal, _EditModal, _DetailsModal)
- Automatic DbContext updates

### `swap generate model <name> --fields <fields>`

Generate just a model class (no controller or views).

```bash
swap g m Category --fields "Name:string Description:string?"
```

### `swap generate resource <name> --fields <fields>`

Generate model + controller together (alias for backward compatibility).

```bash
swap g r BlogPost --fields "Title:string Content:string PublishedDate:DateTime"
```

## 📚 Documentation

- **[Getting Started](https://toonjd.github.io/swap/)** - Complete setup guide
- **[CLI Reference](https://toonjd.github.io/swap/docs/cli/overview)** - All commands and options
- **[Features Guide](https://toonjd.github.io/swap/docs/features/pagination)** - Pagination, search, sorting, filtering
- **[Pattern Library](docs/PATTERNS-LIBRARY.md)** - 30+ proven HTMX patterns
- **[The Product Vision](docs/THE-PRODUCT.md)** - Philosophy and approach

## 🛠️ Development

### Prerequisites
- .NET 9.0 SDK or later
- Your favorite IDE (Visual Studio 2022, VS Code, Rider)

### Building the CLI from Source

```bash
# Clone the repository
git clone https://github.com/toonjd/swap.git
cd swap

# Build the CLI tool
cd tools/Swap.CLI
dotnet build

# Run tests
cd ../Swap.CLI.Tests
dotnet test

# Install locally for testing
cd ../Swap.CLI
dotnet pack
dotnet tool install --global --add-source ./nupkg Swap.CLI
```

### Project Structure

```
swap/
├── tools/
│   ├── Swap.CLI/              # CLI tool source code
│   │   ├── Commands/          # Command implementations
│   │   ├── Infrastructure/    # Template engine, helpers
│   │   └── Program.cs         # CLI entry point
│   └── Swap.CLI.Tests/        # 136 passing tests
│       ├── Commands/          # Command tests
│       └── Infrastructure/    # Template engine tests
├── templates/                 # Code generation templates
│   ├── monolith/             # New project template
│   └── generate/             # CRUD generation templates
│       ├── controller/       # Controller, views, view model
│       └── model/            # Model class
├── docs/                     # Documentation
│   ├── THE-PRODUCT.md        # Product vision
│   └── PATTERNS-LIBRARY.md   # HTMX patterns
├── wiki/                     # Docusaurus documentation site
└── README.md                 # This file
```

## 🤝 Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or code contributions.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes and add tests
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'feat: Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📄 License

Swap CLI is [MIT licensed](LICENSE). Use it freely in your projects, commercial or otherwise.

## � Project Status

**Current Version:** `0.1.0-dev` (Active Development)

### ✅ Phase 2C Complete (Current)

- ✅ **New Project Generation** - `swap new` command with full ASP.NET Core setup
- ✅ **Controller Generation** - `swap g c` with all CRUD operations
- ✅ **Model Generation** - `swap g m` for entity classes
- ✅ **Pagination** - Configurable page sizes (10, 25, 50, 100)
- ✅ **Search** - Real-time search with 500ms debounce
- ✅ **Sorting** - Column sorting with field-level control
- ✅ **Filtering** - Boolean filters with dropdown UI
- ✅ **Modal Editing** - Create, Edit, Details modals via HTMX
- ✅ **Bulk Delete** - Select multiple items and delete
- ✅ **Toast Notifications** - DaisyUI alerts for success/error
- ✅ **DaisyUI Components** - Modern, accessible UI library
- ✅ **Tailwind CSS** - Utility-first styling
- ✅ **136 Passing Tests** - Comprehensive test coverage
- ✅ **Documentation** - Complete wiki with examples

### 🔄 Phase 2D: Seeders (Next)

- ⏳ **Database Seeding** - Generate seed data for testing
- ⏳ **Faker Integration** - Realistic fake data generation
- ⏳ **Seeder Commands** - `swap g seeder` CLI command

### 🎯 Phase 3: Polish & Release

- ⏳ **NuGet Package** - Publish to NuGet.org
- ⏳ **VS Code Extension** - Integrated CLI experience
- ⏳ **Video Tutorials** - Getting started screencasts
- ⏳ **Production Release** (v1.0.0) - Q1 2026

See the complete [roadmap](docs/ROADMAP.md) for details.

## 💬 Community

- **Documentation**: https://toonjd.github.io/swap/
- **GitHub Issues**: https://github.com/toonjd/swap/issues
- **GitHub Discussions**: Coming soon

For questions or feedback, open an [issue](https://github.com/toonjd/swap/issues)!

## 🔗 Links

- **Documentation**: https://toonjd.github.io/swap/
- **GitHub**: https://github.com/toonjd/swap
- **Issues**: https://github.com/toonjd/swap/issues
- **NuGet** (coming soon): https://www.nuget.org/packages/Swap.CLI

---

**Built with ❤️ for the .NET community**

*Swap CLI - Generate production-ready ASP.NET + HTMX applications in seconds.*

