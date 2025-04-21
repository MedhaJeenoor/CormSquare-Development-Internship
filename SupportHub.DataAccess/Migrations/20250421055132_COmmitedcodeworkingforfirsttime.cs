using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class COmmitedcodeworkingforfirsttime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solutions_AspNetUsers_FirstNameId",
                table: "Solutions");

            migrationBuilder.DropIndex(
                name: "IX_Solutions_FirstNameId",
                table: "Solutions");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2fb0bd71-7e17-4075-af0c-935067b15472");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5fe12320-80c4-4cbc-b0e5-685914ded7cb");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7741d863-2383-4fc2-a26a-7846641a673c");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9e588880-45da-465b-b6df-429341250144");

            migrationBuilder.DropColumn(
                name: "FirstNameId",
                table: "Solutions");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "FirstNameId",
                table: "Solutions",
                type: "nvarchar(450)",
                nullable: true);

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "2fb0bd71-7e17-4075-af0c-935067b15472", null, "Internal User", "INTERNAL USER" },
            //        { "5fe12320-80c4-4cbc-b0e5-685914ded7cb", null, "Admin", "ADMIN" },
            //        { "7741d863-2383-4fc2-a26a-7846641a673c", null, "KM Champion", "KM CHAMPION" },
            //        { "9e588880-45da-465b-b6df-429341250144", null, "KM Creator", "KM CREATOR" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 17, 11, 56, 3, 862, DateTimeKind.Utc).AddTicks(8261));

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_FirstNameId",
                table: "Solutions",
                column: "FirstNameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Solutions_AspNetUsers_FirstNameId",
                table: "Solutions",
                column: "FirstNameId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
