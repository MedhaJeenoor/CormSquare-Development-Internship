using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ReseedingWithNewConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "DeletedBy", "DeletedDate", "Description", "DisplayOrder", "HtmlContent", "IsDeleted", "Name", "ParentCategoryId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 1, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Different types of documentation for knowledge management.", 1, "", false, "Documentation", null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
