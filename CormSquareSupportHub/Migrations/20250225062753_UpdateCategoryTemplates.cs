using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CormSquareSupportHub.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Question', 'type': 'text' },\r\n                            { 'label': 'Answer', 'type': 'textarea' }\r\n                        ],\r\n                        'allowAttachments': false,\r\n                        'allowReferenceLinks': false\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Title', 'type': 'text' },\r\n                            { 'label': 'Step-by-Step Instructions', 'type': 'textarea' },\r\n                            { 'label': 'Screenshots', 'type': 'image' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Solution', 'type': 'textarea' },\r\n                            { 'label': 'Code Snippets', 'type': 'code' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Guideline Title', 'type': 'text' },\r\n                            { 'label': 'Description', 'type': 'textarea' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Case Study Title', 'type': 'text' },\r\n                            { 'label': 'Background', 'type': 'textarea' },\r\n                            { 'label': 'Findings', 'type': 'textarea' },\r\n                            { 'label': 'Conclusion', 'type': 'textarea' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Abstract', 'type': 'textarea' },\r\n                            { 'label': 'Methodology', 'type': 'textarea' },\r\n                            { 'label': 'Results', 'type': 'textarea' },\r\n                            { 'label': 'References', 'type': 'text' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Why 1', 'type': 'text' },\r\n                            { 'label': 'Why 2', 'type': 'text' },\r\n                            { 'label': 'Why 3', 'type': 'text' },\r\n                            { 'label': 'Why 4', 'type': 'text' },\r\n                            { 'label': 'Why 5', 'type': 'text' },\r\n                            { 'label': 'Root Cause', 'type': 'textarea' }\r\n                        ],\r\n                        'allowAttachments': false,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Causes', 'type': 'textarea' },\r\n                            { 'label': 'Fishbone Diagram Image', 'type': 'image' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Component', 'type': 'text' },\r\n                            { 'label': 'Potential Failure Mode', 'type': 'text' },\r\n                            { 'label': 'Effect of Failure', 'type': 'textarea' },\r\n                            { 'label': 'Recommended Action', 'type': 'textarea' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12,
                column: "TemplateJson",
                value: "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Contributing Factors', 'type': 'textarea' },\r\n                            { 'label': 'Pareto Chart', 'type': 'image' }\r\n                        ],\r\n                        'allowAttachments': true,\r\n                        'allowReferenceLinks': true\r\n                    }");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12,
                column: "TemplateJson",
                value: "{}");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "AllowAttachments", "Description", "DisplayOrder", "Name", "OptimalCreationTime", "ParentCategoryId", "TemplateJson" },
                values: new object[,]
                {
                    { 13, false, "Graphical model for identifying root causes.", 5, "Fault Tree Analysis (FTA)", 10, 8, "{}" },
                    { 14, false, "Structured RCA process with eight disciplines.", 6, "8D Problem Solving", 14, 8, "{}" }
                });
        }
    }
}
