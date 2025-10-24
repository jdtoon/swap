# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all .csproj files and restore dependencies first.
# This layer is cached and will only be re-run if projects change.
COPY ["framework/NetMX.Core/NetMX.Core.csproj", "framework/NetMX.Core/"]
COPY ["framework/NetMX.Ddd.Domain/NetMX.Ddd.Domain.csproj", "framework/NetMX.Ddd.Domain/"]
COPY ["framework/NetMX.Ddd.Application.Contracts/NetMX.Ddd.Application.Contracts.csproj", "framework/NetMX.Ddd.Application.Contracts/"]
COPY ["framework/NetMX.Ddd.Application/NetMX.Ddd.Application.csproj", "framework/NetMX.Ddd.Application/"]
COPY ["framework/NetMX.Data/NetMX.Data.csproj", "framework/NetMX.Data/"]
COPY ["framework/NetMX.EntityFrameworkCore/NetMX.EntityFrameworkCore.csproj", "framework/NetMX.EntityFrameworkCore/"]
COPY ["framework/NetMX.AspNetCore.Core/NetMX.AspNetCore.Core.csproj", "framework/NetMX.AspNetCore.Core/"]
COPY ["framework/NetMX.AspNetCore.Mvc/NetMX.AspNetCore.Mvc.csproj", "framework/NetMX.AspNetCore.Mvc/"]
COPY ["tools/NetMX.CLI/NetMX.CLI.csproj", "tools/NetMX.CLI/"]
COPY ["templates/modular/src/NetMXApp.Web/NetMXApp.Web.csproj", "templates/modular/src/NetMXApp.Web/"]

# Copy the solution files
COPY ["framework/NetMX.sln", "framework/"]
COPY ["templates/modular/NetMXApp.sln", "templates/modular/"]

RUN dotnet restore "templates/modular/NetMXApp.sln"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/templates/modular/src/NetMXApp.Web"
RUN dotnet build "NetMXApp.Web.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "NetMXApp.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the final, small runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetMXApp.Web.dll"]