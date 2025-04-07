using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NullableSourceColumnInSolutionAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "227f568e-293e-44bb-a5fa-a4d480f41fbb");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "792536ad-4ffc-4be0-958f-03d1e8f240ac");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ccae02fd-627d-4310-8bd2-a6bb8629a3dc");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d6423c72-e52c-4a44-b3ad-6e91eace8761");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SolutionAttachments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "051f3b43-09f7-4c65-a2f8-85a4cc2e8fdd", null, "KM Champion", "KM CHAMPION" },
            //        { "9ad74054-d4e0-4f0d-9650-1566a9e2c4e9", null, "Internal User", "INTERNAL USER" },
            //        { "a661169b-87d1-4df9-984b-6e81bf1b0ec8", null, "Admin", "ADMIN" },
            //        { "f783e86a-a073-428f-b737-cd5f65c94f79", null, "KM Creator", "KM CREATOR" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 4, 10, 14, 6, 33, DateTimeKind.Utc).AddTicks(5255));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "051f3b43-09f7-4c65-a2f8-85a4cc2e8fdd");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9ad74054-d4e0-4f0d-9650-1566a9e2c4e9");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a661169b-87d1-4df9-984b-6e81bf1b0ec8");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f783e86a-a073-428f-b737-cd5f65c94f79");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SolutionAttachments",
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
            //        { "227f568e-293e-44bb-a5fa-a4d480f41fbb", null, "Admin", "ADMIN" },
            //        { "792536ad-4ffc-4be0-958f-03d1e8f240ac", null, "Internal User", "INTERNAL USER" },
            //        { "ccae02fd-627d-4310-8bd2-a6bb8629a3dc", null, "KM Champion", "KM CHAMPION" },
            //        { "d6423c72-e52c-4a44-b3ad-6e91eace8761", null, "KM Creator", "KM CREATOR" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 3, 10, 37, 20, 723, DateTimeKind.Utc).AddTicks(4969));
        }
    }
}
