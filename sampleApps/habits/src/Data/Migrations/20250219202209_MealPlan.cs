using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace habits.Data.Migrations
{
    /// <inheritdoc />
    public partial class MealPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MealPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MondayMeal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TuesdayMeal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WednesdayMeal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ThursdayMeal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FridayMeal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SaturdayMeal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SundayMeal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlan", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealPlan");
        }
    }
}
