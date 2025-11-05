# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Node.js for Tailwind CSS build
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

# Install LibMan CLI
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /work

# Copy project files for restore
COPY ["demo/ModularMonolithDemo/src/Web/Web.csproj", "demo/ModularMonolithDemo/src/Web/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Orders/Orders.Module/Orders.Module.csproj", "demo/ModularMonolithDemo/src/Modules/Orders/Orders.Module/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Orders/Orders.Contracts/Orders.Contracts.csproj", "demo/ModularMonolithDemo/src/Modules/Orders/Orders.Contracts/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Inventory/Inventory.Module/Inventory.Module.csproj", "demo/ModularMonolithDemo/src/Modules/Inventory/Inventory.Module/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Inventory/Inventory.Contracts/Inventory.Contracts.csproj", "demo/ModularMonolithDemo/src/Modules/Inventory/Inventory.Contracts/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Todos/Todos.Contracts/Todos.Contracts.csproj", "demo/ModularMonolithDemo/src/Modules/Todos/Todos.Contracts/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Todos/Todos.Module/Todos.Module.csproj", "demo/ModularMonolithDemo/src/Modules/Todos/Todos.Module/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Todos/Todos.Web/Todos.Web.csproj", "demo/ModularMonolithDemo/src/Modules/Todos/Todos.Web/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Demo/Demo.Contracts/Demo.Contracts.csproj", "demo/ModularMonolithDemo/src/Modules/Demo/Demo.Contracts/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Demo/Demo.Module/Demo.Module.csproj", "demo/ModularMonolithDemo/src/Modules/Demo/Demo.Module/"]
COPY ["demo/ModularMonolithDemo/src/Modules/Demo/Demo.Web/Demo.Web.csproj", "demo/ModularMonolithDemo/src/Modules/Demo/Demo.Web/"]
COPY ["framework/Swap.Modularity/Swap.Modularity.csproj", "framework/Swap.Modularity/"]
COPY ["framework/Swap.Htmx/Swap.Htmx.csproj", "framework/Swap.Htmx/"]

RUN dotnet restore "demo/ModularMonolithDemo/src/Web/Web.csproj"

# Copy the rest of the source
COPY . ./

# Build frontend assets
WORKDIR /work/demo/ModularMonolithDemo/src/Web
RUN npm install
RUN libman restore
RUN npm run build:css

# Publish app
RUN dotnet publish "Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Web.dll"]
