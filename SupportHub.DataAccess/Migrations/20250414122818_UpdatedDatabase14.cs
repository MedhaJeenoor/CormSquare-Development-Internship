using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDatabase14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "446c9476-db87-4152-8a8e-a54d9ea041cb");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4701db37-52dd-43d6-9a81-3fe8ee634819");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5e3e3c8e-4d75-42dc-bb63-2df516997fec");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "94344321-e486-4636-976a-5233eeadaef4");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Solutions");

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "446c9476-db87-4152-8a8e-a54d9ea041cb", null, "Internal User", "INTERNAL USER" },
            //        { "4701db37-52dd-43d6-9a81-3fe8ee634819", null, "KM Creator", "KM CREATOR" },
            //        { "5e3e3c8e-4d75-42dc-bb63-2df516997fec", null, "KM Champion", "KM CHAMPION" },
            //        { "94344321-e486-4636-976a-5233eeadaef4", null, "Admin", "ADMIN" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 8, 5, 51, 39, 870, DateTimeKind.Utc).AddTicks(1481));
        }
    }
}
