using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MakeContributorsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "72c99f4c-6004-489c-94ce-e757804361a8");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e6f308ac-6654-4ef0-80bd-e21586292b57");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ef7ebe14-16d7-45e9-a651-e49dd2833499");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f26eba9c-5473-442a-9c27-064a824576eb");

            migrationBuilder.AlterColumn<string>(
                name: "IssueDescription",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Contributors",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "2bb5d392-f6f4-4cc4-a09c-6b9bc695f77f", null, "Admin", "ADMIN" },
            //        { "bfdb68c9-fe33-497c-93b3-1d3da950f077", null, "KM Champion", "KM CHAMPION" },
            //        { "cc4e1220-0d44-4e60-8dad-1ea11515f21d", null, "Internal User", "INTERNAL USER" },
            //        { "d73f3fea-5a82-456a-8e36-f5710f339018", null, "KM Creator", "KM CREATOR" }
            //    });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2bb5d392-f6f4-4cc4-a09c-6b9bc695f77f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bfdb68c9-fe33-497c-93b3-1d3da950f077");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cc4e1220-0d44-4e60-8dad-1ea11515f21d");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d73f3fea-5a82-456a-8e36-f5710f339018");

            migrationBuilder.AlterColumn<string>(
                name: "IssueDescription",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contributors",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "72c99f4c-6004-489c-94ce-e757804361a8", null, "KM Creator", "KM CREATOR" },
            //        { "e6f308ac-6654-4ef0-80bd-e21586292b57", null, "Admin", "ADMIN" },
            //        { "ef7ebe14-16d7-45e9-a651-e49dd2833499", null, "Internal User", "INTERNAL USER" },
            //        { "f26eba9c-5473-442a-9c27-064a824576eb", null, "KM Champion", "KM CHAMPION" }
            //    });
        }
    }
}
