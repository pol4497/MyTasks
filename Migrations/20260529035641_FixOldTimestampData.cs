using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTasks.Migrations
{
    /// <inheritdoc />
    public partial class FixOldTimestampData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE TaskItems
                SET CreatedAt = CURRENT_TIMESTAMP
                WHERE CreatedAt = '0001-01-01 00:00:00';
            ");

            migrationBuilder.Sql(@"
                UPDATE TaskItems
                SET UpdatedAt = CURRENT_TIMESTAMP
                WHERE UpdatedAt = '0001-01-01 00:00:00';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
