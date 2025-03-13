using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class JaiRamakrishna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Solutions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Solutions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Solutions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Solutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Solutions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SolutionId",
                table: "Solutions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateCategory",
                table: "Solutions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Solutions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Solutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_SolutionId",
                table: "Solutions",
                column: "SolutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Solutions_Solutions_SolutionId",
                table: "Solutions",
                column: "SolutionId",
                principalTable: "Solutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solutions_Solutions_SolutionId",
                table: "Solutions");

            migrationBuilder.DropIndex(
                name: "IX_Solutions_SolutionId",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "SolutionId",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "TemplateCategory",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Solutions");
        }
    }
}
