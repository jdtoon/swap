---
sidebar_position: 1
---

# Docker Deployment

Every Swap project is **Docker-ready** out of the box. The `swap new` command automatically generates Docker configuration files optimized for your chosen database.

## What's Included

When you create a new Swap project, you get:

- ✅ **Dockerfile** - Multi-stage build optimized for production
- ✅ **docker-compose.yml** - Complete development environment with database
- ✅ **.dockerignore** - Optimized build context
- ✅ **Database configuration** - Pre-configured for SQLite, SQL Server, or PostgreSQL
- ✅ **Health checks** - Database readiness checks before app starts
- ✅ **Auto-migrations** - Migrations run automatically on container startup
- ✅ **libman restore** - HTMX and DaisyUI libraries installed during build
- ✅ **Data protection** - Keys persisted across container restarts

## Quick Start

### Build and Run with Docker Compose (Recommended)

The simplest way to run your Swap project in Docker:

```bash
# Create a new project
swap new MyApp --database postgres
cd MyApp

# Start all services (builds automatically)
docker-compose up --build

# Visit http://localhost:5000
```

That's it! Docker Compose will:
1. Build your app with multi-stage Dockerfile
2. Start the database container with health checks
3. Wait for database to be healthy
4. Start your app container
5. Auto-apply migrations on startup
6. Your app is ready at http://localhost:5000

### View Logs

```bash
# Follow app logs
docker-compose logs -f app

# View all logs
docker-compose logs -f
```

### Stop Services

```bash
# Stop and remove containers
docker-compose down

# Stop and remove containers + volumes (deletes database!)
docker-compose down -v
```

## Database-Specific Configurations

### SQLite (Default)

SQLite projects include volume mounting for database persistence and data protection keys:

```yaml
# docker-compose.yml (excerpt)
services:
  app:
    environment:
      - DOTNET_RUNNING_IN_CONTAINER=true
    volumes:
      - sqlite-data:/app/data      # Database persistence
      - key-storage:/app/keys      # Data protection keys

volumes:
  sqlite-data:
  key-storage:
```

**Key Features:**
- ✅ Database persists across container restarts
- ✅ Data protection keys stored in separate volume
- ✅ Migrations auto-apply on startup
- ✅ No separate database container needed
- ✅ Perfect for development and small deployments

**No manual migration needed!** Migrations run automatically when the container starts.

### SQL Server

SQL Server projects include a complete SQL Server container with health checks:

```yaml
# docker-compose.yml includes:
services:
  app:
    depends_on:
      sqlserver:
        condition: service_healthy
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourStrong@Password123
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Password123 -C -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
```

**Key Features:**
- ✅ Health check ensures SQL Server is ready before app starts
- ✅ App waits up to 30s for SQL Server to initialize
- ✅ Migrations auto-apply on startup
- ✅ Persistent volume for database data
- ✅ Exposed on localhost:1433 for external tools

**⚠️ Security:** Change the default password before production!

**No manual migration needed!** Migrations run automatically after SQL Server is healthy.

### PostgreSQL

PostgreSQL projects include a PostgreSQL container with health checks:

```yaml
# docker-compose.yml includes:
services:
  app:
    depends_on:
      postgres:
        condition: service_healthy
  
  postgres:
    image: postgres:16-alpine
    environment:
      - POSTGRES_PASSWORD=YourStrong@Password123
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
      start_period: 10s
```

**Key Features:**
- ✅ Health check ensures PostgreSQL is ready before app starts
- ✅ Fast startup with Alpine Linux (10s start period)
- ✅ Migrations auto-apply on startup
- ✅ Persistent volume for database data
- ✅ Exposed on localhost:5432 for external tools

**⚠️ Security:** Change the default password before production!

**No manual migration needed!** Migrations run automatically after PostgreSQL is healthy.

## Dockerfile Deep Dive

Swap generates a **multi-stage Dockerfile** optimized for ASP.NET Core with all dependencies:

### Build Stage

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Node.js for Tailwind CSS
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs

# Install libman CLI for client libraries
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src

# Restore .NET dependencies
COPY ["MyApp.csproj", "./"]
RUN dotnet restore

# Restore client libraries (HTMX, DaisyUI)
COPY ["libman.json", "./"]
RUN libman restore

# Copy and prepare wwwroot
COPY ["wwwroot/", "/app/wwwroot/"]
RUN chmod -R 755 /app/wwwroot

# Install npm packages and build CSS
COPY ["package.json", "./"]
RUN npm install

# Copy source and build
COPY . .
RUN npm run build:css
RUN dotnet publish "MyApp.csproj" -c Release -o /app/publish /p:UseAppHost=false
```

### Runtime Stage

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Copy wwwroot with libman libraries
COPY --from=build /app/wwwroot ./wwwroot

# Configure HTTP-only for Development
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

**Key Features:**
- ✅ Separate build and runtime stages (smaller final image)
- ✅ Node.js for Tailwind CSS compilation
- ✅ libman CLI for HTMX/DaisyUI dependencies
- ✅ wwwroot preserved with all client libraries
- ✅ HTTP-only configuration for Development
- ✅ Layer caching optimized for fast rebuilds

## Production Deployment

### Environment Variables

Override connection strings and settings via environment variables:

```bash
docker run -d \
  -p 5000:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=prod-db;..." \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  myapp
```

### Using .env Files

Create a `.env` file:

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=prod-db;Database=MyApp;User=app;Password=SecurePassword
```

Reference in `docker-compose.yml`:

```yaml
services:
  app:
    env_file:
      - .env
```

**⚠️ Security:** Never commit `.env` files to source control!

### HTTPS Configuration

For production HTTPS support, mount certificates:

```yaml
services:
  app:
    ports:
      - "443:8081"
    volumes:
      - ./certs:/https:ro
    environment:
      - ASPNETCORE_HTTPS_PORTS=8081
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=YourCertPassword
```

## Common Tasks

### View Application Logs

```bash
# Docker Compose
docker-compose logs -f app

# Docker
docker logs -f myapp
```

### Access Container Shell

```bash
# Docker Compose
docker-compose exec app /bin/bash

# Docker
docker exec -it myapp /bin/bash
```

### Manually Run Database Migrations

Migrations run automatically on startup, but you can run them manually:

```bash
# Docker Compose
docker-compose exec app dotnet ef database update

# Docker
docker exec myapp dotnet ef database update
```

### Add New Migrations

After changing your models:

```bash
# 1. Stop the containers
docker-compose down

# 2. Create migration (on host machine)
dotnet ef migrations add YourMigrationName

# 3. Rebuild and start (migration auto-applies)
docker-compose up --build
```

### Rebuild After Code Changes

```bash
# Docker Compose (recommended)
docker-compose up --build

# Or stop, rebuild, and restart
docker-compose down
docker-compose up --build -d
```

## Cloud Deployment

### Azure Container Instances

```bash
# Build and push to Azure Container Registry
az acr build --registry myregistry --image myapp:latest .

# Deploy to Azure Container Instances
az container create \
  --resource-group mygroup \
  --name myapp \
  --image myregistry.azurecr.io/myapp:latest \
  --dns-name-label myapp \
  --ports 8080
```

### AWS ECS

```bash
# Build and push to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 123456789.dkr.ecr.us-east-1.amazonaws.com
docker build -t myapp .
docker tag myapp:latest 123456789.dkr.ecr.us-east-1.amazonaws.com/myapp:latest
docker push 123456789.dkr.ecr.us-east-1.amazonaws.com/myapp:latest
```

### Google Cloud Run

```bash
# Build and deploy
gcloud builds submit --tag gcr.io/PROJECT-ID/myapp
gcloud run deploy myapp --image gcr.io/PROJECT-ID/myapp --platform managed
```

## Performance Tips

### Image Size Optimization

Swap's multi-stage builds are already optimized, but you can further reduce size:

1. **Use Alpine-based images** (modify Dockerfile):
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
   ```

2. **Remove unnecessary files** (add to `.dockerignore`):
   ```
   tests/
   docs/
   *.md
   ```

### Build Caching

Leverage Docker's layer caching:

```bash
# Build with cache
docker build -t myapp .

# Force rebuild without cache
docker build --no-cache -t myapp .
```

### Resource Limits

Set memory and CPU limits:

```yaml
# docker-compose.yml
services:
  app:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          memory: 256M
```

## Troubleshooting

### Container Won't Start

Check logs:
```bash
docker logs myapp
# or
docker-compose logs app
```

Common issues:
- **Database not ready** - Health checks should handle this, but check database logs
- **Port already in use** - Change host port in docker-compose.yml
- **Missing environment variables** - Check docker-compose.yml environment section
- **HTMX libraries missing** - Ensure libman restore ran during build

### Database Connection Fails

**SQLite:** Ensure volume is mounted correctly
```bash
# Check volume exists
docker volume ls | grep sqlite
```

**SQL Server/PostgreSQL:** Check network connectivity and health status
```bash
# Check service health
docker-compose ps

# Test from inside app container
docker-compose exec app ping sqlserver
# or
docker-compose exec app ping postgres
```

### Migrations Don't Run Automatically

Check that your `Program.cs` includes auto-migration code:

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

This is included by default in all Swap projects.

### HTMX/DaisyUI Not Loading

Ensure libman restore ran during build:

```bash
# Check Dockerfile includes:
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN libman restore
```

Check container logs for libman errors:
```bash
docker-compose logs app | grep libman
```

## Best Practices

✅ **Use multi-stage builds** (Swap does this by default)  
✅ **Set explicit versions** for base images  
✅ **Use .dockerignore** to minimize build context  
✅ **Never commit secrets** to Docker images  
✅ **Use environment variables** for configuration  
✅ **Run as non-root user** in production (add to Dockerfile)  
✅ **Scan images** for vulnerabilities regularly  

## Next Steps

- [Deploy to Azure App Service](/docs/deployment/azure)
- [Deploy to AWS Elastic Beanstalk](/docs/deployment/aws)
- [Configure CI/CD Pipelines](/docs/deployment/cicd)

---

**Questions?** Open an issue on [GitHub](https://github.com/jdtoon/swap/issues)
