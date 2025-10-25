using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ttw.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHotel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityFKID",
                table: "Hotel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityFKID",
                table: "Hotel",
                type: "INTEGER",
                nullable: true);
        }
    }
}
