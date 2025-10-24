# NetMX Template Strategy

**Last Updated**: October 24, 2025  
**Status**: Templates Restructured (4 distinct templates)

---

## 🎯 Template Overview

NetMX provides **4 templates** for different application scales and architectures.

| Template | Price | Best For | Structure | Deployment |
|----------|-------|----------|-----------|------------|
| **Simple Monolith** | FREE | < 10 entities, MVPs, learning | Flat folders | Docker |
| **Vertical Slice** | $49 | 10-20 entities, feature organization | `Features/` folders | Docker |
| **Modular Monolith** | $99 | 20+ entities, reusable modules | `modules/` projects | Docker |
| **Microservices** | $199 | Distributed, independent scaling | `services/` deployments | Docker + K8s |

---

## 1. Simple Monolith Template (FREE)

**Location**: `templates/monolith/`

### Structure
```
MyApp/
├── src/
│   └── MyApp.Web/          # ALL code in ONE project
│       ├── Models/         # Flat folder
│       ├── Dtos/
│       ├── Services/
│       ├── Controllers/
│       ├── Views/
│       └── Data/
└── docker-compose.yml
```

### CLI Commands
```bash
netmx new monolith MyShop
cd src/MyShop.Web
netmx generate feature Product
# Creates: Models/Product.cs, Services/ProductService.cs, etc.
```

### Key Characteristics
- **Flat structure** - No features folders
- **Single project** - Everything in one place
- **SQLite** - Zero Docker dependency
- **Perfect for**: MVPs, learning, < 10 entities

---

## 2. Vertical Slice Template ($49)

**Location**: `templates/vertical-slice/`

### Structure
```
MyApp/
├── src/
│   └── MyApp.Web/
│       ├── Features/              # ⭐ Feature folders
│       │   ├── Products/
│       │   │   ├── Product.cs
│       │   │   ├── ProductService.cs
│       │   │   ├── ProductController.cs
│       │   │   └── Views/
│       │   └── Orders/
│       └── Data/
└── docker-compose.yml
```

### CLI Commands
```bash
netmx new vertical MyShop
cd src/MyShop.Web
netmx generate feature Product
# Creates: Features/Product/ (self-contained)
```

### Key Characteristics
- **Feature folders** - Each feature self-contained
- **Single project** - Still simple deployment
- **Better organization** than flat
- **Perfect for**: 10-20 entities, feature-oriented teams

---

## 3. Modular Monolith Template ($99)

**Location**: `templates/modular/`

### Structure
```
MyApp/
├── src/
│   └── MyApp.Web/          # Host (thin)
│       └── Program.cs      # Wires modules
├── modules/                 # ⭐ Modules (4-layer)
│   ├── Identity/
│   │   ├── Identity.Core/
│   │   ├── Identity.Contracts/
│   │   ├── Identity.Application/
│   │   └── Identity.Web/
│   └── Catalog/
└── docker-compose.yml
```

### CLI Commands
```bash
netmx new modular MyShop
netmx add module Identity
netmx create module Catalog
cd modules/Catalog/Catalog.Web
netmx generate feature Product
# Creates: Catalog.Core/Entities/Product.cs, etc.
```

### Key Characteristics
- **Separate projects** - True module boundaries
- **4-layer architecture** - Core, Contracts, Application, Web
- **Reusable** - Can NuGet package modules
- **Perfect for**: 20+ entities, multiple teams, clear boundaries

---

## 4. Microservices Template ($199)

**Location**: `templates/microservices/`

### Structure
```
MyApp/
├── services/                # ⭐ Services (independent)
│   ├── Identity.Service/
│   │   ├── Identity.Api/
│   │   ├── Identity.Core/
│   │   └── Dockerfile
│   └── Catalog.Service/
├── gateway/                 # API Gateway (YARP)
├── shared/                  # Shared contracts
└── infrastructure/
    ├── docker-compose.yml
    └── kubernetes/          # K8s manifests
```

### CLI Commands
```bash
netmx new microservices MyShop
netmx add service Identity
netmx create service Catalog
cd services/Catalog.Service/Catalog.Api
netmx generate feature Product
# Creates: Catalog.Core/Entities/Product.cs, etc.
```

### Key Characteristics
- **Independent deployments** - Each service separate
- **Docker + Kubernetes** - Cloud-native
- **Event Bus** - RabbitMQ for communication
- **Perfect for**: Distributed systems, independent scaling

---

## Template Detection Logic

CLI detects template type automatically:

```csharp
private TemplateType DetectTemplateType(string solutionDir)
{
    // Check for services/ directory → Microservices
    if (Directory.Exists(Path.Combine(solutionDir, "services")))
        return TemplateType.Microservices;
    
    // Check for modules/ directory → Modular
    if (Directory.Exists(Path.Combine(solutionDir, "modules")))
        return TemplateType.Modular;
    
    // Check for Features/ directory → Vertical Slice
    var webProject = FindWebProject(solutionDir);
    if (webProject != null)
    {
        var webProjectDir = Path.GetDirectoryName(webProject);
        if (Directory.Exists(Path.Combine(webProjectDir, "Features")))
            return TemplateType.VerticalSlice;
    }
    
    // Default → Simple Monolith
    return TemplateType.SimpleMonolith;
}
```

---

## Feature Generation per Template

### Simple Monolith
```
Models/Product.cs
Dtos/ProductDto.cs
Services/ProductService.cs
Controllers/ProductController.cs
Views/Product/
```

### Vertical Slice
```
Features/Product/
├── Product.cs
├── ProductDto.cs
├── ProductService.cs
├── ProductController.cs
└── Views/
```

### Modular Monolith
```
modules/Catalog/
├── Catalog.Core/Entities/Product.cs
├── Catalog.Contracts/Dtos/ProductDto.cs
├── Catalog.Application/Services/ProductService.cs
└── Catalog.Web/Controllers/ProductController.cs
```

### Microservices
```
services/Catalog.Service/
├── Catalog.Core/Entities/Product.cs
├── Catalog.Application/Services/ProductService.cs
└── Catalog.Api/Controllers/ProductController.cs
```

---

## Migration Path

**Start Simple → Scale When Needed**

```
Simple Monolith (FREE)
    ↓
Vertical Slice ($49)        ← Better organization
    ↓
Modular Monolith ($99)      ← Reusable modules
    ↓
Microservices ($199)        ← Independent services
```

**Key Insight**: All templates use same NetMX framework. Migration is incremental, not a rewrite.

---

## Next Steps

1. ✅ Templates restructured (4 distinct READMEs)
2. ⏳ Update CLI commands (`new monolith/vertical/modular/microservices`)
3. ⏳ Update `generate feature` to detect template type
4. ⏳ Create actual template content (Dockerfiles, etc.)

---

**All templates are Docker-ready** with `Dockerfile` and `docker-compose.yml`. Microservices adds Kubernetes manifests.
