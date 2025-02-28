using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "DisplayOrder", "Name", "OptimalCreationTime", "ParentCategoryId" },
                values: new object[,]
                {
                    { 1, "Different types of documentation for knowledge management.", 1, "Documentation", 0, null },
                    { 8, "Templates and methodologies for identifying root causes.", 2, "Root Cause Analysis (RCA)", 0, null },
                    { 2, "Short, direct answers to common questions.", 1, "FAQs", 2, 1 },
                    { 3, "Step-by-step guides for specific tasks.", 2, "How-To Guides", 5, 1 },
                    { 4, "In-depth troubleshooting and fixes.", 3, "Technical Solutions", 7, 1 },
                    { 5, "General guidelines and industry standards.", 4, "Best Practices", 4, 1 },
                    { 6, "Real-world problem analysis and results.", 5, "Case Studies", 10, 1 },
                    { 7, "Extensive research and validation documents.", 6, "Whitepapers", 14, 1 },
                    { 9, "Simple RCA technique by asking 'Why' five times.", 1, "5 Whys Analysis", 2, 8 },
                    { 10, "Cause-and-effect diagram for RCA.", 2, "Fishbone Diagram (Ishikawa)", 5, 8 },
                    { 11, "Proactive RCA technique identifying failure points.", 3, "Failure Mode and Effects Analysis (FMEA)", 7, 8 },
                    { 12, "Prioritizes most significant issues for RCA.", 4, "Pareto Analysis (80/20 Rule)", 4, 8 },
                    { 13, "Graphical model for identifying root causes.", 5, "Fault Tree Analysis (FTA)", 10, 8 },
                    { 14, "Structured RCA process with eight disciplines.", 6, "8D Problem Solving", 14, 8 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
