# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install LibMan CLI globally
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli

# Add .NET tools to PATH
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src

# Copy project file and restore dependencies
COPY ["Swap.Htmx.TestApp.csproj", "./"]
RUN dotnet restore "Swap.Htmx.TestApp.csproj"

# Restore LibMan dependencies
COPY ["libman.json", "./"]
RUN libman restore

# Copy the wwwroot folder to ensure static files are in place
COPY ["wwwroot/", "/app/wwwroot/"]
RUN chmod -R 755 /app/wwwroot

# Copy the rest of the application
COPY . .

# Build and publish the application
RUN dotnet publish "Swap.Htmx.TestApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Expose port (HTTP only)
EXPOSE 8080

# Copy published application from build stage
COPY --from=build /app/publish .
# Copy wwwroot with libman dependencies
COPY --from=build /app/wwwroot ./wwwroot

# For SQLite: Create directories for database file and data protection keys
RUN mkdir -p /app/data /app/keys && chmod -R 700 /app/keys
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/Swap.Htmx.TestApp.db"

# Set ASP.NET Core environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Swap.Htmx.TestApp.dll"]
