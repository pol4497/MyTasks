using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTasks.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskOwnerXorConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_TaskItem_OwnerXor",
                table: "TaskItems",
                sql: "((UserId IS NOT NULL AND GuestSessionId IS NULL) OR (UserId IS NULL AND GuestSessionId IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TaskItem_OwnerXor",
                table: "TaskItems");
        }
    }
}
