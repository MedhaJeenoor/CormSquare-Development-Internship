using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttachmentsAndReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenOption",
                table: "References",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "Attachments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenOption",
                table: "References");

            migrationBuilder.DropColumn(
                name: "Caption",
                table: "Attachments");
        }
    }
}
