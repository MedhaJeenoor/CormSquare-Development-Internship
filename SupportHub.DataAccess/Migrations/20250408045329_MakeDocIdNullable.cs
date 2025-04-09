using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MakeDocIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "DocId",
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
            //        { "50a1b25c-89f0-428c-b051-ee930cfaf2ae", null, "Internal User", "INTERNAL USER" },
            //        { "a02947e6-0abc-48ff-9e78-4e2c23d0740f", null, "Admin", "ADMIN" },
            //        { "f89fc2a3-a1fc-470b-94b2-8a41fafa46ef", null, "KM Creator", "KM CREATOR" },
            //        { "fc3105e6-0540-4759-8a3e-8a017694d717", null, "KM Champion", "KM CHAMPION" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 8, 4, 53, 28, 506, DateTimeKind.Utc).AddTicks(1966));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "50a1b25c-89f0-428c-b051-ee930cfaf2ae");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a02947e6-0abc-48ff-9e78-4e2c23d0740f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f89fc2a3-a1fc-470b-94b2-8a41fafa46ef");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "fc3105e6-0540-4759-8a3e-8a017694d717");

            migrationBuilder.AlterColumn<string>(
                name: "DocId",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "051f3b43-09f7-4c65-a2f8-85a4cc2e8fdd", null, "KM Champion", "KM CHAMPION" },
                    { "9ad74054-d4e0-4f0d-9650-1566a9e2c4e9", null, "Internal User", "INTERNAL USER" },
                    { "a661169b-87d1-4df9-984b-6e81bf1b0ec8", null, "Admin", "ADMIN" },
                    { "f783e86a-a073-428f-b737-cd5f65c94f79", null, "KM Creator", "KM CREATOR" }
                });

            //migrationBuilder.UpdateData(
            //    table: "Categories",
            //    keyColumn: "Id",
            //    keyValue: 1,
            //    column: "CreatedDate",
            //    value: new DateTime(2025, 4, 4, 10, 14, 6, 33, DateTimeKind.Utc).AddTicks(5255));
        }
    }
}
