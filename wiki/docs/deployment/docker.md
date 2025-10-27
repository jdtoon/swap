---
sidebar_position: 1
---

# Docker Deployment

Every Swap project is **Docker-ready** out of the box. The `swap new` command automatically generates Docker configuration files optimized for your chosen database.

## What's Included

When you create a new Swap project, you get:

- ✅ **Dockerfile** - Multi-stage build optimized for production
- ✅ **docker-compose.yml** - Complete development environment
- ✅ **.dockerignore** - Optimized build context
- ✅ **Database configuration** - Pre-configured for SQLite, SQL Server, or PostgreSQL

## Quick Start

### Build and Run

```bash
# Build the Docker image
docker build -t myapp .

# Run the container
docker run -d -p 5000:8080 -p 5001:8081 --name myapp myapp
```

Your app will be available at `http://localhost:5000`

### Using Docker Compose (Recommended)

Docker Compose provides a complete environment with database included:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f app

# Stop services
docker-compose down
```

## Database-Specific Configurations

### SQLite (Default)

SQLite projects include volume mounting for database persistence:

```yaml
# docker-compose.yml (excerpt)
volumes:
  - ./data:/app/data
```

The database file is stored in `./data/` and persists across container restarts.

**Run migrations:**

```bash
docker-compose exec app dotnet ef database update
```

### SQL Server

SQL Server projects include a complete SQL Server container:

```yaml
# docker-compose.yml includes:
services:
  app:
    depends_on:
      - sqlserver
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourStrong@Password123
```

**⚠️ Security:** Change the default password before production!

**Run migrations:**

```bash
docker-compose exec app dotnet ef database update
```

### PostgreSQL

PostgreSQL projects include a PostgreSQL container:

```yaml
# docker-compose.yml includes:
services:
  app:
    depends_on:
      - postgres
  
  postgres:
    image: postgres:16-alpine
    environment:
      - POSTGRES_PASSWORD=YourStrong@Password123
```

**⚠️ Security:** Change the default password before production!

**Run migrations:**

```bash
docker-compose exec app dotnet ef database update
```

## Dockerfile Deep Dive

Swap generates a **multi-stage Dockerfile** optimized for ASP.NET Core:

### Build Stage

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Node.js for Tailwind CSS
RUN apt-get update && apt-get install -y nodejs npm

# Restore dependencies
COPY ["MyApp.csproj", "./"]
RUN dotnet restore

# Install npm packages
COPY ["package.json", "./"]
RUN npm install

# Build CSS with Tailwind
RUN npm run build:css

# Publish application
RUN dotnet publish -c Release -o /app/publish
```

### Runtime Stage

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Copy published app
COPY --from=build /app/publish .

# Configure ports and environment
EXPOSE 8080 8081
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

**Key Features:**
- ✅ Separate build and runtime stages (smaller final image)
- ✅ Node.js installed for Tailwind CSS compilation
- ✅ Multi-platform support (Linux, Windows containers)
- ✅ Production-optimized configuration

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

### Run Database Migrations

```bash
# Docker Compose
docker-compose exec app dotnet ef database update

# Docker
docker exec myapp dotnet ef database update
```

### Rebuild After Code Changes

```bash
# Docker Compose
docker-compose up -d --build

# Docker
docker build -t myapp .
docker stop myapp && docker rm myapp
docker run -d -p 5000:8080 --name myapp myapp
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
```

Common issues:
- Database connection string incorrect
- Port already in use (change host port)
- Missing environment variables

### Database Connection Fails

**SQLite:** Ensure volume is mounted correctly
```bash
docker run -v $(pwd)/data:/app/data myapp
```

**SQL Server/PostgreSQL:** Check network connectivity
```bash
# Test from inside container
docker exec myapp ping sqlserver
```

### CSS Not Building

Ensure Node.js is installed in build stage:
```dockerfile
RUN apt-get update && apt-get install -y nodejs npm
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
