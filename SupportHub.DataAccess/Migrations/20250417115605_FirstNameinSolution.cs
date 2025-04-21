using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FirstNameinSolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "05659fd1-2ab7-4202-89b3-f8f4cdc50f36");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "6c1a882c-72a4-4473-b03a-8dab957920a6");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9353f3a5-0f98-4e34-a63e-5addcbc620d9");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cd959e79-8f47-48e8-a57c-84a6b5a3b65e");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
            //        { "05659fd1-2ab7-4202-89b3-f8f4cdc50f36", null, "KM Creator", "KM CREATOR" },
            //        { "6c1a882c-72a4-4473-b03a-8dab957920a6", null, "Admin", "ADMIN" },
            //        { "9353f3a5-0f98-4e34-a63e-5addcbc620d9", null, "KM Champion", "KM CHAMPION" },
            //        { "cd959e79-8f47-48e8-a57c-84a6b5a3b65e", null, "Internal User", "INTERNAL USER" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 14, 12, 28, 18, 257, DateTimeKind.Utc).AddTicks(638));
        }
    }
}
