using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IssueStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "069334fa-3c84-412c-921f-f81cb482b83e");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2b9339c9-d1c6-47ae-8b83-b90db7f5541a");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ae650e9c-c8cd-426f-a2d9-fc606fd80cde");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c7eaade8-1e2b-4e2e-93a5-eec1ce2de8ba");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending");

            // Update existing rows to set Status = 'Pending'
            migrationBuilder.Sql("UPDATE Issues SET Status = 'Pending' WHERE Status IS NULL");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "069334fa-3c84-412c-921f-f81cb482b83e", null, "Internal User", "INTERNAL USER" },
            //        { "2b9339c9-d1c6-47ae-8b83-b90db7f5541a", null, "KM Creator", "KM CREATOR" },
            //        { "ae650e9c-c8cd-426f-a2d9-fc606fd80cde", null, "KM Champion", "KM CHAMPION" },
            //        { "c7eaade8-1e2b-4e2e-93a5-eec1ce2de8ba", null, "Admin", "ADMIN" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 21, 5, 51, 30, 585, DateTimeKind.Utc).AddTicks(958));

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Issues");
        }
    }
}
