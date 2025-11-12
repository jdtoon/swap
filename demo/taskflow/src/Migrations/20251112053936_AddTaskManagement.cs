using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: false),
                    ColorClass = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "AssignedTo", "CompletedAt", "CreatedAt", "Description", "DueDate", "Priority", "Status", "Tags", "Title" },
                values: new object[,]
                {
                    { 1, "DevOps Team", new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 11, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Configure CI/CD, database, and hosting", null, 2, 2, "", "Set up project infrastructure" },
                    { 2, "Design Team", null, new DateTime(2025, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Create mockups for the task management interface", new DateTime(2025, 11, 14, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, "", "Design task board UI" },
                    { 3, "Backend Team", null, new DateTime(2025, 11, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Add user login and registration features", new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Utc), 3, 0, "", "Implement authentication" },
                    { 4, "Documentation Team", null, new DateTime(2025, 11, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Document all REST endpoints and data models", new DateTime(2025, 11, 22, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0, "", "Write API documentation" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
