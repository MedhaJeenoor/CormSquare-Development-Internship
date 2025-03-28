using Microsoft.EntityFrameworkCore.Migrations;

namespace SupportHub.DataAccess.Migrations
{
    public partial class AssignedRole46 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "AssignedRole",
            //    table: "AspNetUsers",
            //    type: "nvarchar(max)",
            //    nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedRole",
                table: "AspNetUsers");
        }
    }
}