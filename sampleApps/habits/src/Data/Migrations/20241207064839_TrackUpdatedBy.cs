using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace habits.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrackUpdatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "TaskListItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "TaskList",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskListItem_UpdatedById",
                table: "TaskListItem",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TaskList_UpdatedById",
                table: "TaskList",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskList_AspNetUsers_UpdatedById",
                table: "TaskList",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskListItem_AspNetUsers_UpdatedById",
                table: "TaskListItem",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskList_AspNetUsers_UpdatedById",
                table: "TaskList");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskListItem_AspNetUsers_UpdatedById",
                table: "TaskListItem");

            migrationBuilder.DropIndex(
                name: "IX_TaskListItem_UpdatedById",
                table: "TaskListItem");

            migrationBuilder.DropIndex(
                name: "IX_TaskList_UpdatedById",
                table: "TaskList");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "TaskListItem");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "TaskList");
        }
    }
}
