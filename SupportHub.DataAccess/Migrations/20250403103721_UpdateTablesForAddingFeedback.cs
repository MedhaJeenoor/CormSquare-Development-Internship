using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTablesForAddingFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5854e741-8c49-476c-9f35-56f1feca4acc");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5a8e137d-b22a-46f8-af7d-3cd249ba5dc8");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a36969d9-afaa-4365-90e1-4c98e10b85de");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f0c1038d-9cf0-46f8-a53e-643b3be490d0");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryAttachmentId",
                table: "SolutionAttachments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SolutionAttachments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "CategoryAttachmentId",
                table: "SolutionAttachments");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SolutionAttachments");

            //migrationBuilder.InsertData(
            //    table: "AspNetRoles",
            //    columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            //    values: new object[,]
            //    {
            //        { "5854e741-8c49-476c-9f35-56f1feca4acc", null, "Internal User", "INTERNAL USER" },
            //        { "5a8e137d-b22a-46f8-af7d-3cd249ba5dc8", null, "KM Champion", "KM CHAMPION" },
            //        { "a36969d9-afaa-4365-90e1-4c98e10b85de", null, "KM Creator", "KM CREATOR" },
            //        { "f0c1038d-9cf0-46f8-a53e-643b3be490d0", null, "Admin", "ADMIN" }
            //    });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 2, 5, 11, 6, 626, DateTimeKind.Utc).AddTicks(8581));
        }
    }
}
