using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace habits.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreUserNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationSent",
                table: "CalendarEvent",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiveNotifications",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationSent",
                table: "CalendarEvent");

            migrationBuilder.DropColumn(
                name: "ReceiveNotifications",
                table: "AspNetUsers");
        }
    }
}
