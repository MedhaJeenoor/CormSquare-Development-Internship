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
    [Migration("20250224102143_AddCategoryValidation")]
    partial class AddCategoryValidation
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CormSquareSupportHub.Models.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("AllowAttachments")
                        .HasColumnType("bit");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<int>("DisplayOrder")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("OptimalCreationTime")
                        .HasColumnType("int");

                    b.Property<int?>("ParentCategoryId")
                        .HasColumnType("int");

                    b.Property<string>("TemplateJson")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ParentCategoryId");

                    b.ToTable("Categories");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            AllowAttachments = false,
                            Description = "Different types of documentation for knowledge management.",
                            DisplayOrder = 1,
                            Name = "Documentation",
                            OptimalCreationTime = 0,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 2,
                            AllowAttachments = false,
                            Description = "Short, direct answers to common questions.",
                            DisplayOrder = 1,
                            Name = "FAQs",
                            OptimalCreationTime = 2,
                            ParentCategoryId = 1,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 3,
                            AllowAttachments = false,
                            Description = "Step-by-step guides for specific tasks.",
                            DisplayOrder = 2,
                            Name = "How-To Guides",
                            OptimalCreationTime = 5,
                            ParentCategoryId = 1,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 4,
                            AllowAttachments = false,
                            Description = "In-depth troubleshooting and fixes.",
                            DisplayOrder = 3,
                            Name = "Technical Solutions",
                            OptimalCreationTime = 7,
                            ParentCategoryId = 1,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 5,
                            AllowAttachments = false,
                            Description = "General guidelines and industry standards.",
                            DisplayOrder = 4,
                            Name = "Best Practices",
                            OptimalCreationTime = 4,
                            ParentCategoryId = 1,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 6,
                            AllowAttachments = false,
                            Description = "Real-world problem analysis and results.",
                            DisplayOrder = 5,
                            Name = "Case Studies",
                            OptimalCreationTime = 10,
                            ParentCategoryId = 1,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 7,
                            AllowAttachments = false,
                            Description = "Extensive research and validation documents.",
                            DisplayOrder = 6,
                            Name = "Whitepapers",
                            OptimalCreationTime = 14,
                            ParentCategoryId = 1,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 8,
                            AllowAttachments = false,
                            Description = "Templates and methodologies for identifying root causes.",
                            DisplayOrder = 2,
                            Name = "Root Cause Analysis (RCA)",
                            OptimalCreationTime = 0,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 9,
                            AllowAttachments = false,
                            Description = "Simple RCA technique by asking 'Why' five times.",
                            DisplayOrder = 1,
                            Name = "5 Whys Analysis",
                            OptimalCreationTime = 2,
                            ParentCategoryId = 8,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 10,
                            AllowAttachments = false,
                            Description = "Cause-and-effect diagram for RCA.",
                            DisplayOrder = 2,
                            Name = "Fishbone Diagram (Ishikawa)",
                            OptimalCreationTime = 5,
                            ParentCategoryId = 8,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 11,
                            AllowAttachments = false,
                            Description = "Proactive RCA technique identifying failure points.",
                            DisplayOrder = 3,
                            Name = "Failure Mode and Effects Analysis (FMEA)",
                            OptimalCreationTime = 7,
                            ParentCategoryId = 8,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 12,
                            AllowAttachments = false,
                            Description = "Prioritizes most significant issues for RCA.",
                            DisplayOrder = 4,
                            Name = "Pareto Analysis (80/20 Rule)",
                            OptimalCreationTime = 4,
                            ParentCategoryId = 8,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 13,
                            AllowAttachments = false,
                            Description = "Graphical model for identifying root causes.",
                            DisplayOrder = 5,
                            Name = "Fault Tree Analysis (FTA)",
                            OptimalCreationTime = 10,
                            ParentCategoryId = 8,
                            TemplateJson = "{}"
                        },
                        new
                        {
                            Id = 14,
                            AllowAttachments = false,
                            Description = "Structured RCA process with eight disciplines.",
                            DisplayOrder = 6,
                            Name = "8D Problem Solving",
                            OptimalCreationTime = 14,
                            ParentCategoryId = 8,
                            TemplateJson = "{}"
                        });
                });

            modelBuilder.Entity("CormSquareSupportHub.Models.CategoryAttachment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("CategoryAttachments");
                });

            modelBuilder.Entity("CormSquareSupportHub.Models.CategoryReference", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("CategoryReferences");
                });

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

            modelBuilder.Entity("CormSquareSupportHub.Models.Category", b =>
                {
                    b.HasOne("CormSquareSupportHub.Models.Category", "ParentCategory")
                        .WithMany("SubCategories")
                        .HasForeignKey("ParentCategoryId");

                    b.Navigation("ParentCategory");
                });

            modelBuilder.Entity("CormSquareSupportHub.Models.CategoryAttachment", b =>
                {
                    b.HasOne("CormSquareSupportHub.Models.Category", "Category")
                        .WithMany("Attachments")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("CormSquareSupportHub.Models.CategoryReference", b =>
                {
                    b.HasOne("CormSquareSupportHub.Models.Category", "Category")
                        .WithMany("References")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
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

            modelBuilder.Entity("CormSquareSupportHub.Models.Category", b =>
                {
                    b.Navigation("Attachments");

                    b.Navigation("References");

                    b.Navigation("SubCategories");
                });
#pragma warning restore 612, 618
        }
    }
}
