# MonolithDemo

Built with [Swap](https://github.com/jdtoon/swap) - ASP.NET Core + HTMX + DaisyUI

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for Tailwind CSS)



## Getting Started

### 1. Install Dependencies

```bash
npm install
dotnet restore
```

### 2. Build CSS

```bash
npm run build:css
```

Or watch for changes during development:

```bash
npm run watch:css
```

### 3. Run Database Migrations

```bash
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

Visit `http://localhost:5000`

## Docker Support

### Build and Run with Docker

**Build the image:**

```bash
docker build -t monolithdemo .
```

**Run the container:**


```bash
# SQLite - with persistent volume
docker run -d -p 5000:8080 -p 5001:8081 \
  -v $(pwd)/data:/app/data \
  --name monolithdemo \
  monolithdemo
```

The SQLite database will be persisted in the `./data` directory.




### Using Docker Compose

Docker Compose provides a complete environment with the application and database:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (destroys data)
docker-compose down -v
```


The application will be available at `http://localhost:5000`.

Database file is stored in `./data/MonolithDemo.db`.




### Run Database Migrations in Docker

After starting the container, run migrations:

```bash
docker exec -it monolithdemo dotnet ef database update
```

Or with Docker Compose:

```bash
docker-compose exec app dotnet ef database update
```

## Project Structure

```
MonolithDemo/
├── Controllers/        # MVC Controllers
├── Data/              # Entity Framework DbContext
├── Models/            # Entity models
├── Views/             # Razor views with HTMX
├── wwwroot/           # Static files (CSS, JS, images)
├── Program.cs         # Application entry point
└── appsettings.json   # Configuration
```

## Development

### Generate Code with Swap CLI

```bash
# Generate a complete resource (model + controller + views)
swap g r Product --fields "Name:string, Price:decimal, Stock:int"

# Generate controller only
swap g c Category --fields "Name:string, Description:string"

# Generate model only
swap g m Tag --fields "Name:string, Slug:string"
```

### Watch CSS Changes

Run this in a separate terminal during development:

```bash
npm run watch:css
```

## Tech Stack

- **Backend:** ASP.NET Core 9.0 (C#)
- **Frontend:** HTMX + DaisyUI (Tailwind CSS)
- **Database:** sqlite
- **ORM:** Entity Framework Core 9.0

## Learn More

- [Swap Documentation](https://jdtoon.github.io/swap)
- [HTMX Documentation](https://htmx.org/)
- [DaisyUI Components](https://daisyui.com/components/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
