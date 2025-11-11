# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install LibMan CLI
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /work

# Copy project files for restore (build context is src)
COPY ["Web/Web.csproj", "Web/"]
COPY ["Modules/Todos/Todos.Contracts/Todos.Contracts.csproj", "Modules/Todos/Todos.Contracts/"]
COPY ["Modules/Todos/Todos.Module/Todos.Module.csproj", "Modules/Todos/Todos.Module/"]
COPY ["Modules/Todos/Todos.Web/Todos.Web.csproj", "Modules/Todos/Todos.Web/"]
COPY ["Modules/Demo/Demo.Contracts/Demo.Contracts.csproj", "Modules/Demo/Demo.Contracts/"]
COPY ["Modules/Demo/Demo.Module/Demo.Module.csproj", "Modules/Demo/Demo.Module/"]
COPY ["Modules/Demo/Demo.Web/Demo.Web.csproj", "Modules/Demo/Demo.Web/"]

RUN dotnet restore "Web/Web.csproj"

# Copy the rest of the source
COPY . .

# Build frontend assets
WORKDIR /work/Web
RUN libman restore

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
