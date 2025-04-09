using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductWithCodeForDocId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "SubCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "Code",
                table: "SubCategories");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Products");

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
    }
}
