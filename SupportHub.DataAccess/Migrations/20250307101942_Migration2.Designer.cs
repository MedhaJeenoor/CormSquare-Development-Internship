﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SupportHub.DataAccess.Data;

#nullable disable

namespace SupportHub.DataAccess.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250307101942_Migration2")]
    partial class Migration2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("SupportHub.Models.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("AllowAttachments")
                        .HasColumnType("bit");

                    b.Property<bool>("AllowReferenceLinks")
                        .HasColumnType("bit");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<int?>("DeletedBy")
                        .HasColumnType("int");

                    b.Property<DateTime?>("DeletedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<int>("DisplayOrder")
                        .HasColumnType("int");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("OptimalCreationTime")
                        .HasColumnType("int");

                    b.Property<int?>("ParentCategoryId")
                        .HasColumnType("int");

                    b.Property<string>("TemplateJson")
                        .HasColumnType("NVARCHAR(MAX)");

                    b.Property<int?>("UpdatedBy")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentCategoryId");

                    b.ToTable("Categories");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Different types of documentation for knowledge management.",
                            DisplayOrder = 1,
                            IsDeleted = false,
                            Name = "Documentation",
                            OptimalCreationTime = 0,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 2,
                            AllowAttachments = false,
                            AllowReferenceLinks = false,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Short, direct answers to common questions.",
                            DisplayOrder = 1,
                            IsDeleted = false,
                            Name = "FAQs",
                            OptimalCreationTime = 2,
                            ParentCategoryId = 1,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Question\", \"type\": \"text\" },\r\n                            { \"label\": \"Answer\", \"type\": \"textarea\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 3,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Step-by-step guides for specific tasks.",
                            DisplayOrder = 2,
                            IsDeleted = false,
                            Name = "How-To Guides",
                            OptimalCreationTime = 5,
                            ParentCategoryId = 1,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Title\", \"type\": \"text\" },\r\n                            { \"label\": \"Step-by-Step Instructions\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Screenshots\", \"type\": \"image\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 4,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "In-depth troubleshooting and fixes.",
                            DisplayOrder = 3,
                            IsDeleted = false,
                            Name = "Technical Solutions",
                            OptimalCreationTime = 7,
                            ParentCategoryId = 1,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Solution\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Code Snippets\", \"type\": \"code\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 5,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "General guidelines and industry standards.",
                            DisplayOrder = 4,
                            IsDeleted = false,
                            Name = "Best Practices",
                            OptimalCreationTime = 4,
                            ParentCategoryId = 1,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Guideline Title\", \"type\": \"text\" },\r\n                            { \"label\": \"Description\", \"type\": \"textarea\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 6,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Real-world problem analysis and results.",
                            DisplayOrder = 5,
                            IsDeleted = false,
                            Name = "Case Studies",
                            OptimalCreationTime = 10,
                            ParentCategoryId = 1,
                            TemplateJson = "{\r\n        \"fields\": [\r\n            { \"label\": \"Case Study Title\", \"type\": \"text\" },\r\n            { \"label\": \"Background\", \"type\": \"textarea\" },\r\n            { \"label\": \"Findings\", \"type\": \"textarea\" },\r\n            { \"label\": \"Conclusion\", \"type\": \"textarea\" }\r\n        ]\r\n    }"
                        },
                        new
                        {
                            Id = 7,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Extensive research and validation documents.",
                            DisplayOrder = 6,
                            IsDeleted = false,
                            Name = "Whitepapers",
                            OptimalCreationTime = 14,
                            ParentCategoryId = 1,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Abstract\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Methodology\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Results\", \"type\": \"textarea\" },\r\n                            { \"label\": \"References\", \"type\": \"text\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 8,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Templates and methodologies for identifying root causes.",
                            DisplayOrder = 2,
                            IsDeleted = false,
                            Name = "Root Cause Analysis (RCA)",
                            OptimalCreationTime = 0,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 9,
                            AllowAttachments = false,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Simple RCA technique by asking \"Why\" five times.",
                            DisplayOrder = 1,
                            IsDeleted = false,
                            Name = "5 Whys Analysis",
                            OptimalCreationTime = 2,
                            ParentCategoryId = 8,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 1\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 2\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 3\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 4\", \"type\": \"text\" },\r\n                            { \"label\": \"Why 5\", \"type\": \"text\" },\r\n                            { \"label\": \"Root Cause\", \"type\": \"textarea\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 10,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Cause-and-effect diagram for RCA.",
                            DisplayOrder = 2,
                            IsDeleted = false,
                            Name = "Fishbone Diagram (Ishikawa)",
                            OptimalCreationTime = 5,
                            ParentCategoryId = 8,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Causes\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Fishbone Diagram Image\", \"type\": \"image\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 11,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Proactive RCA technique identifying failure points.",
                            DisplayOrder = 3,
                            IsDeleted = false,
                            Name = "Failure Mode and Effects Analysis (FMEA)",
                            OptimalCreationTime = 7,
                            ParentCategoryId = 8,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Component\", \"type\": \"text\" },\r\n                            { \"label\": \"Potential Failure Mode\", \"type\": \"text\" },\r\n                            { \"label\": \"Effect of Failure\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Recommended Action\", \"type\": \"textarea\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 12,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Prioritizes most significant issues for RCA.",
                            DisplayOrder = 4,
                            IsDeleted = false,
                            Name = "Pareto Analysis (80/20 Rule)",
                            OptimalCreationTime = 4,
                            ParentCategoryId = 8,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Statement\", \"type\": \"text\" },\r\n                            { \"label\": \"Contributing Factors\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Pareto Chart\", \"type\": \"image\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 13,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Graphical model for identifying root causes.",
                            DisplayOrder = 5,
                            IsDeleted = false,
                            Name = "Fault Tree Analysis (FTA)",
                            OptimalCreationTime = 10,
                            ParentCategoryId = 8,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Fault Event\", \"type\": \"text\" },\r\n                            { \"label\": \"Contributing Factors\", \"type\": \"textarea\" }\r\n                        ]\r\n                    }"
                        },
                        new
                        {
                            Id = 14,
                            AllowAttachments = true,
                            AllowReferenceLinks = true,
                            CreatedBy = 0,
                            CreatedDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Description = "Structured RCA process with eight disciplines.",
                            DisplayOrder = 6,
                            IsDeleted = false,
                            Name = "8D Problem Solving",
                            OptimalCreationTime = 14,
                            ParentCategoryId = 8,
                            TemplateJson = "{\r\n                        \"fields\": [\r\n                            { \"label\": \"Problem Description\", \"type\": \"text\" },\r\n                            { \"label\": \"Root Cause\", \"type\": \"textarea\" },\r\n                            { \"label\": \"Corrective Action\", \"type\": \"textarea\" }\r\n                        ]\r\n                    }"
                        });
                });

            modelBuilder.Entity("SupportHub.Models.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Product");
                });

            modelBuilder.Entity("SupportHub.Models.Solution", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("NVARCHAR(MAX)");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<int?>("DeletedBy")
                        .HasColumnType("int");

                    b.Property<DateTime?>("DeletedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("DocumentId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<int>("ProductId")
                        .HasColumnType("int");

                    b.Property<int?>("SolutionId")
                        .HasColumnType("int");

                    b.Property<string>("TemplateCategory")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int?>("UpdatedBy")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("ProductId");

                    b.HasIndex("SolutionId");

                    b.ToTable("Solutions");
                });

            modelBuilder.Entity("SupportHub.Models.SolutionAttachment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SolutionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SolutionId");

                    b.ToTable("SolutionAttachments");
                });

            modelBuilder.Entity("SupportHub.Models.SolutionReference", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("SolutionId")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SolutionId");

                    b.ToTable("SolutionReferences");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SupportHub.Models.Category", b =>
                {
                    b.HasOne("SupportHub.Models.Category", "ParentCategory")
                        .WithMany("SubCategories")
                        .HasForeignKey("ParentCategoryId");

                    b.Navigation("ParentCategory");
                });

            modelBuilder.Entity("SupportHub.Models.Solution", b =>
                {
                    b.HasOne("SupportHub.Models.Category", "Category")
                        .WithMany("Solutions")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SupportHub.Models.Product", "Product")
                        .WithMany("Solutions")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SupportHub.Models.Solution", null)
                        .WithMany("Solutions")
                        .HasForeignKey("SolutionId");

                    b.Navigation("Category");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("SupportHub.Models.SolutionAttachment", b =>
                {
                    b.HasOne("SupportHub.Models.Solution", "Solution")
                        .WithMany("Attachments")
                        .HasForeignKey("SolutionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Solution");
                });

            modelBuilder.Entity("SupportHub.Models.SolutionReference", b =>
                {
                    b.HasOne("SupportHub.Models.Solution", "Solution")
                        .WithMany("References")
                        .HasForeignKey("SolutionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Solution");
                });

            modelBuilder.Entity("SupportHub.Models.Category", b =>
                {
                    b.Navigation("Solutions");

                    b.Navigation("SubCategories");
                });

            modelBuilder.Entity("SupportHub.Models.Product", b =>
                {
                    b.Navigation("Solutions");
                });

            modelBuilder.Entity("SupportHub.Models.Solution", b =>
                {
                    b.Navigation("Attachments");

                    b.Navigation("References");

                    b.Navigation("Solutions");
                });
#pragma warning restore 612, 618
        }
    }
}
