﻿using CormSquareSupportHub.Models;
using Microsoft.EntityFrameworkCore;

namespace CormSquareSupportHub.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Category>().HasData(
                // Parent Category: Documentation Types
                new Category
                {
                    Id = 1,
                    Name = "Documentation",
                    Description = "Different types of documentation for knowledge management.",
                    ParentCategoryId = null,
                    OptimalCreationTime = 0,
                    DisplayOrder = 1
                },
                // Subcategories under Documentation
                new Category
                {
                    Id = 2,
                    Name = "FAQs",
                    Description = "Short, direct answers to common questions.",
                    ParentCategoryId = 1,
                    OptimalCreationTime = 2,
                    DisplayOrder = 1
                },
                new Category
                {
                    Id = 3,
                    Name = "How-To Guides",
                    Description = "Step-by-step guides for specific tasks.",
                    ParentCategoryId = 1,
                    OptimalCreationTime = 5,
                    DisplayOrder = 2
                },
                new Category
                {
                    Id = 4,
                    Name = "Technical Solutions",
                    Description = "In-depth troubleshooting and fixes.",
                    ParentCategoryId = 1,
                    OptimalCreationTime = 7,
                    DisplayOrder = 3
                },
                new Category
                {
                    Id = 5,
                    Name = "Best Practices",
                    Description = "General guidelines and industry standards.",
                    ParentCategoryId = 1,
                    OptimalCreationTime = 4,
                    DisplayOrder = 4
                },
                new Category
                {
                    Id = 6,
                    Name = "Case Studies",
                    Description = "Real-world problem analysis and results.",
                    ParentCategoryId = 1,
                    OptimalCreationTime = 10,
                    DisplayOrder = 5
                },
                new Category
                {
                    Id = 7,
                    Name = "Whitepapers",
                    Description = "Extensive research and validation documents.",
                    ParentCategoryId = 1,
                    OptimalCreationTime = 14,
                    DisplayOrder = 6
                },
                // Parent Category: RCA Templates
                new Category
                {
                    Id = 8,
                    Name = "Root Cause Analysis (RCA)",
                    Description = "Templates and methodologies for identifying root causes.",
                    ParentCategoryId = null,
                    OptimalCreationTime = 0,
                    DisplayOrder = 2
                },
                // Subcategories under RCA Templates
                new Category
                {
                    Id = 9,
                    Name = "5 Whys Analysis",
                    Description = "Simple RCA technique by asking 'Why' five times.",
                    ParentCategoryId = 8,
                    OptimalCreationTime = 2,
                    DisplayOrder = 1
                },
                new Category
                {
                    Id = 10,
                    Name = "Fishbone Diagram (Ishikawa)",
                    Description = "Cause-and-effect diagram for RCA.",
                    ParentCategoryId = 8,
                    OptimalCreationTime = 5,
                    DisplayOrder = 2
                },
                new Category
                {
                    Id = 11,
                    Name = "Failure Mode and Effects Analysis (FMEA)",
                    Description = "Proactive RCA technique identifying failure points.",
                    ParentCategoryId = 8,
                    OptimalCreationTime = 7,
                    DisplayOrder = 3
                },
                new Category
                {
                    Id = 12,
                    Name = "Pareto Analysis (80/20 Rule)",
                    Description = "Prioritizes most significant issues for RCA.",
                    ParentCategoryId = 8,
                    OptimalCreationTime = 4,
                    DisplayOrder = 4
                },
                new Category
                {
                    Id = 13,
                    Name = "Fault Tree Analysis (FTA)",
                    Description = "Graphical model for identifying root causes.",
                    ParentCategoryId = 8,
                    OptimalCreationTime = 10,
                    DisplayOrder = 5
                },
                new Category
                {
                    Id = 14,
                    Name = "8D Problem Solving",
                    Description = "Structured RCA process with eight disciplines.",
                    ParentCategoryId = 8,
                    OptimalCreationTime = 14,
                    DisplayOrder = 6
                }
            );

        }
    }
}
