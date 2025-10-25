using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace habits.Data.Migrations
{
    /// <inheritdoc />
    public partial class MealPlanRemoveStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MealPlan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "MealPlan",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
