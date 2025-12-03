FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src
COPY ["SwapModularMonolith.csproj", "./"]
RUN dotnet restore "SwapModularMonolith.csproj"

COPY ["libman.json", "./"]
RUN libman restore

COPY . .
RUN dotnet publish "SwapModularMonolith.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/wwwroot ./wwwroot

RUN mkdir -p /app/data /app/keys && chmod -R 700 /app/keys
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/swapmodularmonolith.db"
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "SwapModularMonolith.dll"]
