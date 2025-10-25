# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
# Install Chrome dependencies and Chrome itself
RUN apt-get update && \
    apt-get install -y wget gnupg2 apt-transport-https ca-certificates && \
    apt-get install -y fonts-liberation libasound2 libatk-bridge2.0-0 libatk1.0-0 && \
    apt-get install -y libatspi2.0-0 libcups2 libdbus-1-3 libdrm2 libgbm1 && \
    apt-get install -y libgtk-3-0 libnspr4 libnss3 libx11-6 libxcb1 && \
    apt-get install -y libxcomposite1 libxdamage1 libxext6 libxfixes3 libxrandr2 && \
    apt-get install -y xdg-utils libu2f-udev libvulkan1 chromium && \
    rm -rf /var/lib/apt/lists/*

# Create directory for Chrome user data
RUN mkdir -p /chrome-user-data && \
    chmod 777 /chrome-user-data

ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

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
COPY ["ttw.csproj", "."]

# Restore .NET dependencies
RUN dotnet restore "./ttw.csproj"

# Restore LibMan dependencies
COPY ["libman.json", "./"]
RUN libman restore

# Copy the wwwroot folder first to ensure static files are in place
COPY ["wwwroot/", "/app/wwwroot/"]
RUN chmod -R 755 /app/wwwroot

# Install npm packages
COPY ["package.json", "package-lock.json", "./"]
RUN npm install

# Copy the rest of the files
COPY . .

# Run Tailwind build
RUN npm run css:build

# Build the application
RUN dotnet build "./ttw.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ttw.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /app/wwwroot ./wwwroot

ENTRYPOINT ["dotnet", "ttw.dll"]

ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000