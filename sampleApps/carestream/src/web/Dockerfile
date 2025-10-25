# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 3000

# Build image with Node.js and LibMan
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

ARG BUILD_CONFIGURATION=Release
# Use /source for a clean, non-conflicting working directory
WORKDIR /source

# --- STEP 1: Copy project and manifest files with CORRECT pathing ---
# This creates a clean /source/src/... structure, mirroring your repo.
COPY ["carestream.sln", "."]
COPY ["src/core/carestream.core/carestream.core.csproj", "src/core/carestream.core/"]
COPY ["src/persistence/carestream.persistence/carestream.persistence.csproj", "src/persistence/carestream.persistence/"]
COPY ["src/web/carestream.web.csproj", "src/web/"]
COPY ["src/web/package.json", "src/web/"]
COPY ["src/web/package-lock.json", "src/web/"]
COPY ["src/web/libman.json", "src/web/"]
COPY ["src/web/tailwind.config.js", "src/web/"]

# --- STEP 2: Restore ONLY the web project and its dependencies ---
# This is the key fix you correctly pointed out. We do NOT restore the .sln.
RUN dotnet restore "src/web/carestream.web.csproj"

# --- STEP 3: Copy ALL remaining source files ---
# This brings in your .sql files and all other code, placing them in the correct, clean paths.
COPY . .

# --- STEP 4: Build & Publish the web application ---
WORKDIR /source/src/web
RUN libman restore
RUN npm install
# dotnet publish will build the entire dependency tree correctly now that
# all files are present and packages are restored.
RUN dotnet publish "carestream.web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# --- FINAL STAGE ---
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "carestream.web.dll"]
ENV ASPNETCORE_URLS=http://+:3000