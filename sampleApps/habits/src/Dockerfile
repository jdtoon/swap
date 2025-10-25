# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Ensure the .db folder is marked as a volume for database persistence
RUN mkdir -p /app/.db && chown -R $APP_UID:$APP_UID /app/.db
VOLUME ["/app/.db"]

RUN mkdir -p /app/keys && chmod -R 700 /app/keys
RUN chown -R $APP_UID:$APP_UID /app/keys

# Build image with Node.js and LibMan
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Add Node.js installation
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs

# Install LibMan CLI globally
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli

# Add .NET tools to PATH
ENV PATH="$PATH:/root/.dotnet/tools"

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["habits.csproj", "."]

# Restore .NET dependencies
RUN dotnet restore "./habits.csproj"

# Restore LibMan dependencies
COPY ["libman.json", "./"]
RUN libman restore

# Copy the wwwroot folder (containing the static files) into the container
COPY /wwwroot /app/wwwroot

# Install npm packages
COPY ["package.json", "package-lock.json", "./"]
RUN npm install

# Copy the rest of the files
COPY . .

# Run Tailwind build
RUN npm run css:build

# Build the application
RUN dotnet build "./habits.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./habits.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "habits.dll"]

ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000