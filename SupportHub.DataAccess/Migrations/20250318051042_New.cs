using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class New : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropTable(
                name: "SolutionAttachments");

            migrationBuilder.DropTable(
                name: "SolutionReferences");

            migrationBuilder.DropTable(
                name: "Solutions");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories");

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

            migrationBuilder.DropColumn(
                name: "AllowAttachments",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AllowReferenceLinks",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "OptimalCreationTime",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "TemplateJson",
                table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAttachments",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowReferenceLinks",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OptimalCreationTime",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateJson",
                table: "Categories",
                type: "NVARCHAR(MAX)",
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
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    DocumentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
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
                    SolutionId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    SolutionId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "AllowAttachments", "AllowReferenceLinks", "CreatedBy", "CreatedDate", "DeletedBy", "DeletedDate", "Description", "DisplayOrder", "IsDeleted", "Name", "OptimalCreationTime", "ParentCategoryId", "TemplateJson", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Different types of documentation for knowledge management.", 1, false, "Documentation", 0, null, "{}", null, null },
                    { 8, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Templates and methodologies for identifying root causes.", 2, false, "Root Cause Analysis (RCA)", 0, null, "{}", null, null },
                    { 2, false, false, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Short, direct answers to common questions.", 1, false, "FAQs", 2, 1, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Question\", \"type\": \"text\" },\r\n                            { \"label\": \"Answer\", \"type\": \"textarea\", \"editor\": \"tinymce\"}\r\n                        ]\r\n                    }", null, null },
                    { 3, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Step-by-step guides for specific tasks.", 2, false, "How-To Guides", 5, 1, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Title\", \"type\": \"text\" },\r\n                            { \"label\": \"Step-by-Step Instructions\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Screenshots\", \"type\": \"image\" }\r\n                        ]\r\n                    }", null, null },
                    { 4, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "In-depth troubleshooting and fixes.", 3, false, "Technical Solutions", 7, 1, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Solution\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Code Snippets\", \"type\": \"code\" }\r\n                        ]\r\n                    }", null, null },
                    { 5, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "General guidelines and industry standards.", 4, false, "Best Practices", 4, 1, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Guideline Title\", \"type\": \"text\" },\r\n                            { \"label\": \"Description\", \"type\": \"textarea\", \"editor\": \"tinymce\" }\r\n                        ]\r\n                    }", null, null },
                    { 6, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Real-world problem analysis and results.", 5, false, "Case Studies", 10, 1, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Case Study Title\", \"type\": \"text\" },\r\n                            { \"label\": \"Background\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Findings\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Conclusion\", \"type\": \"textarea\", \"editor\": \"tinymce\" }\r\n                        ]\r\n                    }", null, null },
                    { 7, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Extensive research and validation documents.", 6, false, "Whitepapers", 14, 1, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Abstract\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Methodology\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Results\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"References\", \"type\": \"text\" }\r\n                        ]\r\n                    }", null, null },
                    { 9, false, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Simple RCA technique by asking \"Why\" five times.", 1, false, "5 Whys Analysis", 2, 8, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 1\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 2\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 3\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 4\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 5\", \"type\": \"text\" },\r\n                            { \"label\": \"Root Cause\", \"type\": \"textarea\", \"editor\": \"tinymce\" }\r\n                        ]\r\n                    }", null, null },
                    { 10, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Cause-and-effect diagram for RCA.", 2, false, "Fishbone Diagram (Ishikawa)", 5, 8, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Causes\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Fishbone Diagram Image\", \"type\": \"image\" }\r\n                        ]\r\n                    }", null, null },
                    { 11, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Proactive RCA technique identifying failure points.", 3, false, "Failure Mode and Effects Analysis (FMEA)", 7, 8, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Component\", \"type\": \"text\" },\r\n                            { \"label\": \"Potential Failure Mode\", \"type\": \"text\" },\r\n                            { \"label\": \"Effect of Failure\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Recommended Action\", \"type\": \"textarea\", \"editor\": \"tinymce\" }\r\n                        ]\r\n                    }", null, null },
                    { 12, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Prioritizes most significant issues for RCA.", 4, false, "Pareto Analysis (80/20 Rule)", 4, 8, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Contributing Factors\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Pareto Chart\", \"type\": \"image\" }\r\n                        ]\r\n                    }", null, null },
                    { 13, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Graphical model for identifying root causes.", 5, false, "Fault Tree Analysis (FTA)", 10, 8, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Fault Event\", \"type\": \"text\" },\r\n                            { \"label\": \"Contributing Factors\", \"type\": \"textarea\", \"editor\": \"tinymce\" }\r\n                        ]\r\n                    }", null, null },
                    { 14, true, true, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Structured RCA process with eight disciplines.", 6, false, "8D Problem Solving", 14, 8, "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Description\", \"type\": \"text\" },\r\n                            { \"label\": \"Root Cause\", \"type\": \"textarea\", \"editor\": \"tinymce\" },\r\n                            { \"label\": \"Corrective Action\", \"type\": \"textarea\", \"editor\": \"tinymce\" }\r\n                        ]\r\n                    }", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
