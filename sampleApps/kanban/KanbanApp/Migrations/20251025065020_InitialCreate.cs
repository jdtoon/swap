using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KanbanApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lists_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ListId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_Lists_ListId",
                        column: x => x.ListId,
                        principalTable: "Lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Boards",
                columns: new[] { "Id", "CreatedAt", "Description", "IsArchived", "Name", "Position" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 12, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Track product features and releases", false, "Product Roadmap", 0 },
                    { 2, new DateTime(2024, 12, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Current sprint tasks", false, "Sprint Planning", 1 }
                });

            migrationBuilder.InsertData(
                table: "Lists",
                columns: new[] { "Id", "BoardId", "IsArchived", "Name", "Position" },
                values: new object[,]
                {
                    { 1, 1, false, "Backlog", 0 },
                    { 2, 1, false, "In Progress", 1 },
                    { 3, 1, false, "Done", 2 },
                    { 4, 2, false, "To Do", 0 },
                    { 5, 2, false, "Doing", 1 },
                    { 6, 2, false, "Review", 2 },
                    { 7, 2, false, "Completed", 3 }
                });

            migrationBuilder.InsertData(
                table: "Cards",
                columns: new[] { "Id", "CreatedAt", "Description", "DueDate", "ListId", "Position", "Priority", "Title" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 12, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Implement OAuth2 login", null, 1, 0, 2, "User Authentication" },
                    { 2, new DateTime(2024, 12, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Integrate Stripe payments", null, 1, 1, 1, "Payment Gateway" },
                    { 3, new DateTime(2024, 12, 14, 0, 0, 0, 0, DateTimeKind.Utc), "Send transactional emails", null, 1, 2, 0, "Email Notifications" },
                    { 4, new DateTime(2024, 12, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Design EF Core models", null, 2, 0, 2, "Database Schema" },
                    { 5, new DateTime(2024, 12, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Build REST API", null, 2, 1, 2, "API Endpoints" },
                    { 6, new DateTime(2024, 12, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Initialize project with .NET 9", null, 3, 0, 2, "Project Setup" },
                    { 7, new DateTime(2024, 12, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Setup GitHub Actions", null, 3, 1, 1, "CI/CD Pipeline" },
                    { 8, new DateTime(2024, 12, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Plan next 2 weeks", new DateTime(2025, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), 4, 0, 3, "Sprint Planning Meeting" },
                    { 9, new DateTime(2024, 12, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Fix 401 error on login", null, 4, 1, 3, "Bug Fix: Login Error" },
                    { 10, new DateTime(2024, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Add search functionality", null, 5, 0, 2, "Implement Search" },
                    { 11, new DateTime(2024, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Review PR #42", null, 6, 0, 1, "Code Review: Auth Module" },
                    { 12, new DateTime(2024, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Deploy latest version", null, 7, 0, 1, "Deploy to Staging" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boards_Position",
                table: "Boards",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ListId_Position",
                table: "Cards",
                columns: new[] { "ListId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Lists_BoardId_Position",
                table: "Lists",
                columns: new[] { "BoardId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "Lists");

            migrationBuilder.DropTable(
                name: "Boards");
        }
    }
}
