---
sidebar_position: 2
---

# Deploy to Azure App Service

This guide shows a minimal path to deploy a Swap-generated app to Azure App Service (Linux) using a Docker image.

> Note: A full step-by-step with screenshots is coming soon. This page exists to satisfy internal links and unblock builds.

## Prerequisites
- Azure subscription
- Azure CLI installed and logged in
- Docker installed (for local builds)

## Quick Start (Container)

1) Build and tag your image

```bash
docker build -t myapp:latest .
```

2) Create Azure resources and push image (replace names)

```bash
# Create a resource group
az group create -n myapp-rg -l westeurope

# Create an Azure Container Registry (ACR)
az acr create -n myappacr1234 -g myapp-rg --sku Basic

# Login to ACR
az acr login -n myappacr1234

# Tag and push image
acr=myappacr1234.azurecr.io
docker tag myapp:latest $acr/myapp:latest
docker push $acr/myapp:latest
```

3) Create App Service with container

```bash
# Create the App Service plan
az appservice plan create -g myapp-rg -n myapp-plan --is-linux --sku B1

# Create the Web App referencing ACR image
az webapp create -g myapp-rg -p myapp-plan -n myapp-web \
  -i myappacr1234.azurecr.io/myapp:latest

# Configure connection strings as needed
az webapp config connection-string set \
  -g myapp-rg -n myapp-web \
  --settings DefaultConnection="<connection-string>" \
  --connection-string-type=Custom
```

4) Browse your site

```bash
az webapp browse -g myapp-rg -n myapp-web
```

## Notes
- For SQL Server/PostgreSQL, provision a managed database (Azure SQL / Azure Database for PostgreSQL) and update your connection string.
- Enable HTTPS and custom domains via App Service configuration.
- Consider using managed identity or Key Vault for secrets.
