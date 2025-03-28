using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AssignedRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "29477400-076f-4b12-b918-9b196e3f9987");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "299964de-a961-4f27-ae0a-a6ca92c86e2d");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "31fe337d-f612-4076-b6d4-4ef08774c116");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e13c7bef-95df-4eb5-b90d-8324eed40239");

            migrationBuilder.AddColumn<string>(
                name: "AssignedRole",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1564e10e-51a9-4616-8c5f-28b05144b0c8", null, "KM Champion", "KM CHAMPION" },
                    { "57272824-fcf9-49f8-8a89-3f52cb1e0e87", null, "Internal User", "INTERNAL USER" },
                    { "b71b327d-1eec-40f7-95c8-b44d0591aa86", null, "Admin", "ADMIN" },
                    { "d4b83526-661a-44d9-aa80-bb4ad2c4a65d", null, "KM Creator", "KM CREATOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1564e10e-51a9-4616-8c5f-28b05144b0c8");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "57272824-fcf9-49f8-8a89-3f52cb1e0e87");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b71b327d-1eec-40f7-95c8-b44d0591aa86");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d4b83526-661a-44d9-aa80-bb4ad2c4a65d");

            migrationBuilder.DropColumn(
                name: "AssignedRole",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "29477400-076f-4b12-b918-9b196e3f9987", null, "Admin", "ADMIN" },
                    { "299964de-a961-4f27-ae0a-a6ca92c86e2d", null, "KM Champion", "KM CHAMPION" },
                    { "31fe337d-f612-4076-b6d4-4ef08774c116", null, "KM Creator", "KM CREATOR" },
                    { "e13c7bef-95df-4eb5-b90d-8324eed40239", null, "Internal User", "INTERNAL USER" }
                });
        }
    }
}
