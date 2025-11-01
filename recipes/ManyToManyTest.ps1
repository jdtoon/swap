# ManyToManyTest Recipe
# Tests many-to-many relationship generation (Student ↔ Course)
# Validates: junction table creation, Edit tracking fix, M2M display in views, M2M seeding

# Navigate to testApps folder
Set-Location c:\jd\swap\testApps

# Create new test app (skip setup for speed)
dotnet c:\jd\swap\tools\Swap.CLI\bin\Release\net9.0\Swap.CLI.dll new ManyToManyTest --skip-setup

# Navigate into project
Set-Location ManyToManyTest

# Generate Student model
dotnet c:\jd\swap\tools\Swap.CLI\bin\Release\net9.0\Swap.CLI.dll g m Student --fields "Name:string Email:string EnrollmentDate:DateTime"

# Generate Course model
dotnet c:\jd\swap\tools\Swap.CLI\bin\Release\net9.0\Swap.CLI.dll g m Course --fields "Title:string Credits:int"

# Generate many-to-many relationship
dotnet c:\jd\swap\tools\Swap.CLI\bin\Release\net9.0\Swap.CLI.dll g rel --source Student --target Course --type many-to-many

# Generate Student controller with relationships and nav link
dotnet c:\jd\swap\tools\Swap.CLI\bin\Release\net9.0\Swap.CLI.dll g c Student --add-nav

# Generate Course controller with relationships and nav link
dotnet c:\jd\swap\tools\Swap.CLI\bin\Release\net9.0\Swap.CLI.dll g c Course --add-nav

# Generate seeders for all entities (20 records each, ordered: Student, Course, CourseStudent)
dotnet c:\jd\swap\tools\Swap.CLI\bin\Release\net9.0\Swap.CLI.dll g seed all --count 20

# Install frontend dependencies
npm install

# Restore client libraries
libman restore

# Build Tailwind CSS
npm run build:css

# Create initial migration
dotnet ef migrations add InitManyToMany

# Apply migration
dotnet ef database update

# Run the app
# dotnet run
# Then browse to:
#   /Student - view list with Courses column, create/edit with course selection
#   /Course - view list with Students column, create/edit with student selection
# Expected:
#   - Lists show "Title1, Title2, Title3 (+N more)" for related items
#   - Details modals show badges for all related items
#   - Edit works without EF tracking conflicts
#   - Seeding populates CourseStudent junction table
