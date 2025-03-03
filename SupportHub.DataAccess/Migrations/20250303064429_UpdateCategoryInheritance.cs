using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryAttachments");

            migrationBuilder.DropTable(
                name: "CategoryReferences");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Categories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Solutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    DocumentId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solutions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Solutions_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolutionAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SolutionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolutionAttachments_Solutions_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "Solutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolutionReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SolutionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolutionReferences_Solutions_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "Solutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AllowAttachments", "AllowReferenceLinks", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Question', 'type': 'text' },\r\n                            { 'label': 'Answer', 'type': 'textarea' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Title', 'type': 'text' },\r\n                            { 'label': 'Step-by-Step Instructions', 'type': 'textarea' },\r\n                            { 'label': 'Screenshots', 'type': 'image' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Solution', 'type': 'textarea' },\r\n                            { 'label': 'Code Snippets', 'type': 'code' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Guideline Title', 'type': 'text' },\r\n                            { 'label': 'Description', 'type': 'textarea' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Case Study Title', 'type': 'text' },\r\n                            { 'label': 'Background', 'type': 'textarea' },\r\n                            { 'label': 'Findings', 'type': 'textarea' },\r\n                            { 'label': 'Conclusion', 'type': 'textarea' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Abstract', 'type': 'textarea' },\r\n                            { 'label': 'Methodology', 'type': 'textarea' },\r\n                            { 'label': 'Results', 'type': 'textarea' },\r\n                            { 'label': 'References', 'type': 'text' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "AllowAttachments", "AllowReferenceLinks", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Why 1', 'type': 'text' },\r\n                            { 'label': 'Why 2', 'type': 'text' },\r\n                            { 'label': 'Why 3', 'type': 'text' },\r\n                            { 'label': 'Why 4', 'type': 'text' },\r\n                            { 'label': 'Why 5', 'type': 'text' },\r\n                            { 'label': 'Root Cause', 'type': 'textarea' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Causes', 'type': 'textarea' },\r\n                            { 'label': 'Fishbone Diagram Image', 'type': 'image' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Component', 'type': 'text' },\r\n                            { 'label': 'Potential Failure Mode', 'type': 'text' },\r\n                            { 'label': 'Effect of Failure', 'type': 'textarea' },\r\n                            { 'label': 'Recommended Action', 'type': 'textarea' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Statement', 'type': 'text' },\r\n                            { 'label': 'Contributing Factors', 'type': 'textarea' },\r\n                            { 'label': 'Pareto Chart', 'type': 'image' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Fault Event', 'type': 'text' },\r\n                            { 'label': 'Contributing Factors', 'type': 'textarea' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CreatedBy", "CreatedDate", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\r\n                        'fields': [\r\n                            { 'label': 'Problem Description', 'type': 'text' },\r\n                            { 'label': 'Root Cause', 'type': 'textarea' },\r\n                            { 'label': 'Corrective Action', 'type': 'textarea' }\r\n                        ]\r\n                    }", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_SolutionAttachments_SolutionId",
                table: "SolutionAttachments",
                column: "SolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SolutionReferences_SolutionId",
                table: "SolutionReferences",
                column: "SolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_CategoryId",
                table: "Solutions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_ProductId",
                table: "Solutions",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolutionAttachments");

            migrationBuilder.DropTable(
                name: "SolutionReferences");

            migrationBuilder.DropTable(
                name: "Solutions");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "CategoryAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryAttachments_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryReferences_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AllowAttachments", "AllowReferenceLinks" },
                values: new object[] { false, false });

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

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Solution', 'type': 'textarea' },\r\n                { 'label': 'Code Snippets', 'type': 'code' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Guideline Title', 'type': 'text' },\r\n                { 'label': 'Description', 'type': 'textarea' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Case Study Title', 'type': 'text' },\r\n                { 'label': 'Background', 'type': 'textarea' },\r\n                { 'label': 'Findings', 'type': 'textarea' },\r\n                { 'label': 'Conclusion', 'type': 'textarea' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Abstract', 'type': 'textarea' },\r\n                { 'label': 'Methodology', 'type': 'textarea' },\r\n                { 'label': 'Results', 'type': 'textarea' },\r\n                { 'label': 'References', 'type': 'text' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "AllowAttachments", "AllowReferenceLinks" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Why 1', 'type': 'text' },\r\n                { 'label': 'Why 2', 'type': 'text' },\r\n                { 'label': 'Why 3', 'type': 'text' },\r\n                { 'label': 'Why 4', 'type': 'text' },\r\n                { 'label': 'Why 5', 'type': 'text' },\r\n                { 'label': 'Root Cause', 'type': 'textarea' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Causes', 'type': 'textarea' },\r\n                { 'label': 'Fishbone Diagram Image', 'type': 'image' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Component', 'type': 'text' },\r\n                { 'label': 'Potential Failure Mode', 'type': 'text' },\r\n                { 'label': 'Effect of Failure', 'type': 'textarea' },\r\n                { 'label': 'Recommended Action', 'type': 'textarea' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Problem Statement', 'type': 'text' },\r\n                { 'label': 'Contributing Factors', 'type': 'textarea' },\r\n                { 'label': 'Pareto Chart', 'type': 'image' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Fault Event', 'type': 'text' },\r\n                { 'label': 'Contributing Factors', 'type': 'textarea' }\r\n            ]\r\n        }");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14,
                column: "TemplateJson",
                value: "{\r\n            'fields': [\r\n                { 'label': 'Problem Description', 'type': 'text' },\r\n                { 'label': 'Root Cause', 'type': 'textarea' },\r\n                { 'label': 'Corrective Action', 'type': 'textarea' }\r\n            ]\r\n        }");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttachments_CategoryId",
                table: "CategoryAttachments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryReferences_CategoryId",
                table: "CategoryReferences",
                column: "CategoryId");
        }
    }
}
