using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RevertingToOriginalCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Question', 'type': 'text' },\r\n                { 'label': 'Answer', 'type': 'textarea' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Title', 'type': 'text' },\r\n                { 'label': 'Step-by-Step Instructions', 'type': 'textarea' },\r\n                { 'label': 'Screenshots', 'type': 'image' }\r\n            ]\r\n        }");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "AllowAttachments", "AllowReferenceLinks", "Description", "DisplayOrder", "Name", "OptimalCreationTime", "ParentCategoryId", "TemplateJson" },
                values: new object[,]
                {
                    { 1, false, false, "Different types of documentation for knowledge management.", 1, "Documentation", 0, null, "{}" },
                    { 8, false, false, "Templates and methodologies for identifying root causes.", 2, "Root Cause Analysis (RCA)", 0, null, "{}" },
                    { 4, true, true, "In-depth troubleshooting and fixes.", 3, "Technical Solutions", 7, 1, "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Solution', 'type': 'textarea' },\r\n                { 'label': 'Code Snippets', 'type': 'code' }\r\n            ]\r\n        }" },
                    { 5, true, true, "General guidelines and industry standards.", 4, "Best Practices", 4, 1, "{\r\n            'fields': [\r\n                { 'label': 'Guideline Title', 'type': 'text' },\r\n                { 'label': 'Description', 'type': 'textarea' }\r\n            ]\r\n        }" },
                    { 6, true, true, "Real-world problem analysis and results.", 5, "Case Studies", 10, 1, "{\r\n            'fields': [\r\n                { 'label': 'Case Study Title', 'type': 'text' },\r\n                { 'label': 'Background', 'type': 'textarea' },\r\n                { 'label': 'Findings', 'type': 'textarea' },\r\n                { 'label': 'Conclusion', 'type': 'textarea' }\r\n            ]\r\n        }" },
                    { 7, true, true, "Extensive research and validation documents.", 6, "Whitepapers", 14, 1, "{\r\n            'fields': [\r\n                { 'label': 'Abstract', 'type': 'textarea' },\r\n                { 'label': 'Methodology', 'type': 'textarea' },\r\n                { 'label': 'Results', 'type': 'textarea' },\r\n                { 'label': 'References', 'type': 'text' }\r\n            ]\r\n        }" },
                    { 9, false, true, "Simple RCA technique by asking 'Why' five times.", 1, "5 Whys Analysis", 2, 8, "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Why 1', 'type': 'text' },\r\n                { 'label': 'Why 2', 'type': 'text' },\r\n                { 'label': 'Why 3', 'type': 'text' },\r\n                { 'label': 'Why 4', 'type': 'text' },\r\n                { 'label': 'Why 5', 'type': 'text' },\r\n                { 'label': 'Root Cause', 'type': 'textarea' }\r\n            ]\r\n        }" },
                    { 10, true, true, "Cause-and-effect diagram for RCA.", 2, "Fishbone Diagram (Ishikawa)", 5, 8, "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Causes', 'type': 'textarea' },\r\n                { 'label': 'Fishbone Diagram Image', 'type': 'image' }\r\n            ]\r\n        }" },
                    { 11, true, true, "Proactive RCA technique identifying failure points.", 3, "Failure Mode and Effects Analysis (FMEA)", 7, 8, "{\r\n            'fields': [\r\n                { 'label': 'Component', 'type': 'text' },\r\n                { 'label': 'Potential Failure Mode', 'type': 'text' },\r\n                { 'label': 'Effect of Failure', 'type': 'textarea' },\r\n                { 'label': 'Recommended Action', 'type': 'textarea' }\r\n            ]\r\n        }" },
                    { 12, true, true, "Prioritizes most significant issues for RCA.", 4, "Pareto Analysis (80/20 Rule)", 4, 8, "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Contributing Factors', 'type': 'textarea' },\r\n                { 'label': 'Pareto Chart', 'type': 'image' }\r\n            ]\r\n        }" },
                    { 13, true, true, "Graphical model for identifying root causes.", 5, "Fault Tree Analysis (FTA)", 10, 8, "{\r\n            'fields': [\r\n                { 'label': 'Fault Event', 'type': 'text' },\r\n                { 'label': 'Contributing Factors', 'type': 'textarea' }\r\n            ]\r\n        }" },
                    { 14, true, true, "Structured RCA process with eight disciplines.", 6, "8D Problem Solving", 14, 8, "{\r\n            'fields': [\r\n                { 'label': 'Problem Description', 'type': 'text' },\r\n                { 'label': 'Root Cause', 'type': 'textarea' },\r\n                { 'label': 'Corrective Action', 'type': 'textarea' }\r\n            ]\r\n        }" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "TemplateJson",
                value: "{\"fields\": [{\"label\": \"Question\", \"type\": \"text\"}, {\"label\": \"Answer\", \"type\": \"textarea\"}], \"allowAttachments\": false, \"allowReferenceLinks\": false}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "TemplateJson",
                value: "{\"fields\": [{\"label\": \"Title\", \"type\": \"text\"}, {\"label\": \"Step-by-Step Instructions\", \"type\": \"textarea\"}, {\"label\": \"Screenshots\", \"type\": \"image\"}], \"allowAttachments\": true, \"allowReferenceLinks\": true}");
        }
    }
}
