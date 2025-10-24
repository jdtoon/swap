# NetMX Microservices Template

**Distributed Architecture** - Independently deployable services

## 💡 Best For

- ✅ Large-scale applications
- ✅ Independent service scaling
- ✅ Polyglot persistence (different DBs per service)
- ✅ Teams owning services end-to-end
- ✅ Cloud-native deployments (Kubernetes)

## 📦 What You Get

- **ASP.NET Core 9.0** - Latest .NET stack
- **PostgreSQL/SQLite** - Per-service databases
- **Entity Framework Core** - Code-first migrations
- **HTMX-First UI** - Server-rendered HTML with interactivity
- **API Gateway** - YARP reverse proxy
- **Service Discovery** - Consul or built-in
- **Docker + Kubernetes** - Production-ready orchestration
- **Event Bus** - RabbitMQ for inter-service communication
- **NetMX Framework** - DDD patterns, HTMX helpers, events

## 🏗️ Structure

```
MyApp/
├── MyApp.sln                            # Root solution
├── services/                            # ⭐ SERVICES (independently deployable)
│   ├── Identity.Service/
│   │   ├── Identity.Service.sln
│   │   ├── Identity.Api/               # REST API
│   │   ├── Identity.Core/              # Domain
│   │   ├── Identity.Application/       # Services
│   │   ├── Identity.Infrastructure/    # Data, external
│   │   ├── Dockerfile
│   │   └── helm/                       # Kubernetes charts
│   ├── Catalog.Service/
│   │   ├── Catalog.Api/
│   │   ├── Catalog.Core/
│   │   ├── Catalog.Application/
│   │   ├── Catalog.Infrastructure/
│   │   └── Dockerfile
│   └── Orders.Service/
├── gateway/                             # API Gateway
│   ├── ApiGateway/
│   │   ├── appsettings.json            # YARP routes
│   │   └── Program.cs
│   └── Dockerfile
├── shared/                              # Shared contracts
│   ├── Contracts/                      # Inter-service DTOs
│   └── Events/                         # Event definitions
├── infrastructure/
│   ├── docker-compose.yml              # Local development
│   ├── docker-compose.prod.yml         # Production
│   └── kubernetes/                     # K8s manifests
│       ├── identity-service.yaml
│       ├── catalog-service.yaml
│       ├── orders-service.yaml
│       ├── gateway.yaml
│       ├── rabbitmq.yaml
│       └── ingress.yaml
└── nuget.config
```

**Key Characteristic**: Services are **independently deployable**, each with own database, Docker image, K8s deployment

## 🚀 Quick Start

### 1. Prerequisites

- .NET 9.0 SDK
- Docker Desktop
- Kubernetes (Docker Desktop, minikube, or cloud)
- NetMX CLI: `dotnet tool install --global NetMX.CLI`
- kubectl, helm (for K8s)

### 2. Create Your App

```bash
netmx new microservices MyShop
cd MyShop
```

### 3. Add Services

```bash
netmx add service Identity          # Copies Identity service
netmx create service Catalog        # Scaffolds NEW service
```

### 4. Generate Features in Service

```bash
cd services/Catalog.Service/Catalog.Api
netmx generate feature Product
```

### 5. Local Development (Docker Compose)

```bash
# Start all services
docker-compose up

# Services:
# - Identity: http://localhost:5001
# - Catalog: http://localhost:5002
# - Orders: http://localhost:5003
# - Gateway: http://localhost:8080
# - RabbitMQ: http://localhost:15672
```

### 6. Production Deployment (Kubernetes)

```bash
# Build images
docker build -t myshop/identity:1.0 -f services/Identity.Service/Dockerfile .
docker build -t myshop/catalog:1.0 -f services/Catalog.Service/Dockerfile .
docker build -t myshop/orders:1.0 -f services/Orders.Service/Dockerfile .
docker build -t myshop/gateway:1.0 -f gateway/Dockerfile .

# Push to registry
docker push myshop/identity:1.0
docker push myshop/catalog:1.0

# Deploy to Kubernetes
kubectl apply -f infrastructure/kubernetes/

# Or use Helm
helm install myshop ./infrastructure/helm/
```

### 7. Access

- Gateway: `http://localhost:8080`
- Identity UI: `http://localhost:8080/identity`
- Catalog UI: `http://localhost:8080/catalog`
- Orders UI: `http://localhost:8080/orders`
- Health: `http://localhost:8080/health`

## 🐳 Docker + Kubernetes

### Docker Compose (Local)

```bash
# Start all services
docker-compose up

# Start specific service
docker-compose up identity-service

# View logs
docker-compose logs -f catalog-service

# Stop all
docker-compose down
```

### Kubernetes (Production)

```bash
# Deploy
kubectl apply -f infrastructure/kubernetes/

# Check status
kubectl get pods
kubectl get services
kubectl get ingress

# View logs
kubectl logs -f deployment/catalog-service

# Scale service
kubectl scale deployment catalog-service --replicas=3

# Update service
kubectl set image deployment/catalog-service catalog=myshop/catalog:1.1
```

## 📊 When to Choose This Template

**Choose Microservices if:**
- ✅ Need independent scaling (scale services separately)
- ✅ Polyglot persistence (different DBs per service)
- ✅ Team autonomy (teams own services)
- ✅ Cloud-native deployment (Kubernetes)
- ✅ Resilience (service failures isolated)

**Downgrade to Modular if:**
- ⚠️ Distributed complexity not needed
- ⚠️ Single deployment simpler
- ⚠️ Network latency concerns

## ⚙️ Service Communication

### Synchronous (HTTP)

```csharp
// Catalog.Api → Identity.Api (user info)
var user = await _httpClient.GetFromJsonAsync<UserDto>(
    "http://identity-service/api/users/{id}"
);
```

### Asynchronous (Event Bus)

```csharp
// Orders.Api publishes event
await _eventBus.PublishAsync(new OrderCreatedEvent
{
    OrderId = order.Id,
    ProductIds = order.Items.Select(x => x.ProductId).ToList()
});

// Catalog.Api subscribes
public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // Update stock levels
    }
}
```

## 🎯 API Gateway (YARP)

Routes requests to services:

```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": { "Path": "/identity/{**catch-all}" }
      },
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": { "Path": "/catalog/{**catch-all}" }
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "identity": { "Address": "http://identity-service:80" }
        }
      },
      "catalog-cluster": {
        "Destinations": {
          "catalog": { "Address": "http://catalog-service:80" }
        }
      }
    }
  }
}
```

## 🔧 CLI Commands

```bash
# Add existing service
netmx add service Identity

# Create new service
netmx create service Catalog

# Generate feature in service
cd services/Catalog.Service/Catalog.Api
netmx generate feature Product

# Database per service
cd services/Catalog.Service/Catalog.Api
netmx db migrate AddProduct
netmx db update
```

## 🏗️ Service Structure (Clean Architecture)

Each service:

1. **Api** - Controllers, Program.cs
2. **Core** - Domain entities, interfaces
3. **Application** - Services, use cases
4. **Infrastructure** - Data, external APIs, event handlers

## 📚 Learn More

- [NetMX Documentation](../../docs/)
- [Microservices Architecture](../../docs/MICROSERVICES-ARCHITECTURE.md)
- [Event Bus Guide](../../docs/EVENT-BUS-ARCHITECTURE.md)
- [Kubernetes Deployment](../../docs/KUBERNETES-DEPLOYMENT.md)

## 💰 Pricing

**$199 one-time purchase**

Includes:
- Microservices template
- API Gateway (YARP)
- Event Bus (RabbitMQ)
- Docker + Kubernetes configs
- 1 year of template updates
- Priority support

---

**Maximum scalability** - Each service can scale independently
