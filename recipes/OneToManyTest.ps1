# OneToMany Relationship Test Recipe
# This script creates a test application with Author -> Post one-to-many relationship

# Navigate to testApps directory
Set-Location $PSScriptRoot\..\testApps

# Create new Swap project
dotnet run --project ..\tools\Swap.CLI --no-build -c Release -- new OneToManyTest --database sqlite

# Navigate into project
Set-Location OneToManyTest

# Generate Author model
dotnet run --project ..\..\tools\Swap.CLI --no-build -c Release -- g m Author -f "Name:string Email:string Bio:string"

# Generate Post model
dotnet run --project ..\..\tools\Swap.CLI --no-build -c Release -- g m Post -f "Title:string Content:string IsPublished:bool"

# Generate OneToMany relationship: Author -> Post
dotnet run --project ..\..\tools\Swap.CLI --no-build -c Release -- g rel -s Author -t Post --type one-to-many

# Generate Author controller with navigation
dotnet run --project ..\..\tools\Swap.CLI --no-build -c Release -- g c Author --add-nav

# Generate Post controller with navigation
dotnet run --project ..\..\tools\Swap.CLI --no-build -c Release -- g c Post --add-nav

# Apply migrations
dotnet ef database update

# Generate seed data
dotnet run --project ..\..\tools\Swap.CLI --no-build -c Release -- g seed Author --count 5
dotnet run --project ..\..\tools\Swap.CLI --no-build -c Release -- g seed Post --count 20

# Build with seeding
$env:SEED_COUNT = "5,20"
dotnet build --nologo

Write-Host "`n✅ OneToManyTest app created successfully!" -ForegroundColor Green
Write-Host "To run: cd testApps\OneToManyTest; dotnet run" -ForegroundColor Cyan
Write-Host "Then visit: http://localhost:5000" -ForegroundColor Cyan
