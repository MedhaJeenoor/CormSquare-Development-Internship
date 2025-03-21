using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemovingPreviousDbRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deletes all rows from Categories where Id is NOT 1
            migrationBuilder.Sql("DELETE FROM [Categories] WHERE [Id] != 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
