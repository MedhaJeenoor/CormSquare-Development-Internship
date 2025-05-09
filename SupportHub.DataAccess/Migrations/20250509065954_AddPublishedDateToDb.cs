using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedDateToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0412bce3-f390-4fa5-a94f-f5b54a01d9a6");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a77645a0-0867-40bd-9876-b447d2c3cf1e");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b0325e7e-0600-4f97-a36f-3c46f0455cc0");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bde572f3-aca1-4bdc-ac7d-ac8cdae06b35");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedDate",
                table: "Solutions",
                type: "datetime2",
                nullable: true);

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "09d780b3-c874-4983-906b-b85df09071a7", null, "KM Creator", "KM CREATOR" },
            //        { "2e721803-167c-4308-b727-ec94f4bd5afd", null, "KM Champion", "KM CHAMPION" },
            //        { "337f9c27-240a-4d61-94a4-59c6ceff09b7", null, "Admin", "ADMIN" },
            //        { "85130375-e3b7-4be8-8c24-252b19887f10", null, "Internal User", "INTERNAL USER" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 5, 9, 6, 59, 53, 297, DateTimeKind.Utc).AddTicks(9549));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "09d780b3-c874-4983-906b-b85df09071a7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2e721803-167c-4308-b727-ec94f4bd5afd");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "337f9c27-240a-4d61-94a4-59c6ceff09b7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "85130375-e3b7-4be8-8c24-252b19887f10");

            migrationBuilder.DropColumn(
                name: "PublishedDate",
                table: "Solutions");

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "0412bce3-f390-4fa5-a94f-f5b54a01d9a6", null, "Internal User", "INTERNAL USER" },
            //        { "a77645a0-0867-40bd-9876-b447d2c3cf1e", null, "KM Champion", "KM CHAMPION" },
            //        { "b0325e7e-0600-4f97-a36f-3c46f0455cc0", null, "KM Creator", "KM CREATOR" },
            //        { "bde572f3-aca1-4bdc-ac7d-ac8cdae06b35", null, "Admin", "ADMIN" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 30, 6, 16, 45, 209, DateTimeKind.Utc).AddTicks(8998));
        }
    }
}
