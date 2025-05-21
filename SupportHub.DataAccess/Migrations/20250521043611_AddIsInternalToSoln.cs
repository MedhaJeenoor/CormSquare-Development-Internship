using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsInternalToSoln : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<bool>(
                name: "IsInternalTemplate",
                table: "Solutions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "0778202c-6488-470e-a79b-8e4f5d89a738", null, "KM Creator", "KM CREATOR" },
            //        { "11658ae5-c43c-4633-a301-2855a98b3bb2", null, "Admin", "ADMIN" },
            //        { "cdefc7b2-3508-48a2-99a9-dca832ec0158", null, "KM Champion", "KM CHAMPION" },
            //        { "e5752c84-d673-4ae6-a301-226a0e2544df", null, "Internal User", "INTERNAL USER" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 5, 21, 4, 36, 9, 953, DateTimeKind.Utc).AddTicks(8318));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0778202c-6488-470e-a79b-8e4f5d89a738");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "11658ae5-c43c-4633-a301-2855a98b3bb2");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cdefc7b2-3508-48a2-99a9-dca832ec0158");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e5752c84-d673-4ae6-a301-226a0e2544df");

            migrationBuilder.DropColumn(
                name: "IsInternalTemplate",
                table: "Solutions");

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
    }
}
