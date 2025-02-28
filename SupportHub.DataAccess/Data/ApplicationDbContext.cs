using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SupportHub.Models;

namespace SupportHub.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryAttachment> CategoryAttachments { get; set; }
        public DbSet<CategoryReference> CategoryReferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
    // Parent Category: Documentation Types
    new Category
    {
        Id = 1,
        Name = "Documentation",
        Description = "Different types of documentation for knowledge management.",
        ParentCategoryId = null,
        OptimalCreationTime = 0,
        DisplayOrder = 1,
        AllowAttachments = false,
        AllowReferenceLinks = false,
        TemplateJson = "{}"
    },
    // Subcategories under Documentation
    new Category
    {
        Id = 2,
        Name = "FAQs",
        Description = "Short, direct answers to common questions.",
        ParentCategoryId = 1,
        OptimalCreationTime = 2,
        DisplayOrder = 1,
        AllowAttachments = false,
        AllowReferenceLinks = false,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Question', 'type': 'text' },
                { 'label': 'Answer', 'type': 'textarea' }
            ]
        }"
    },
    new Category
    {
        Id = 3,
        Name = "How-To Guides",
        Description = "Step-by-step guides for specific tasks.",
        ParentCategoryId = 1,
        OptimalCreationTime = 5,
        DisplayOrder = 2,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Title', 'type': 'text' },
                { 'label': 'Step-by-Step Instructions', 'type': 'textarea' },
                { 'label': 'Screenshots', 'type': 'image' }
            ]
        }"
    },
    new Category
    {
        Id = 4,
        Name = "Technical Solutions",
        Description = "In-depth troubleshooting and fixes.",
        ParentCategoryId = 1,
        OptimalCreationTime = 7,
        DisplayOrder = 3,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Problem Statement', 'type': 'text' },
                { 'label': 'Solution', 'type': 'textarea' },
                { 'label': 'Code Snippets', 'type': 'code' }
            ]
        }"
    },
    new Category
    {
        Id = 5,
        Name = "Best Practices",
        Description = "General guidelines and industry standards.",
        ParentCategoryId = 1,
        OptimalCreationTime = 4,
        DisplayOrder = 4,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Guideline Title', 'type': 'text' },
                { 'label': 'Description', 'type': 'textarea' }
            ]
        }"
    },
    new Category
    {
        Id = 6,
        Name = "Case Studies",
        Description = "Real-world problem analysis and results.",
        ParentCategoryId = 1,
        OptimalCreationTime = 10,
        DisplayOrder = 5,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Case Study Title', 'type': 'text' },
                { 'label': 'Background', 'type': 'textarea' },
                { 'label': 'Findings', 'type': 'textarea' },
                { 'label': 'Conclusion', 'type': 'textarea' }
            ]
        }"
    },
    new Category
    {
        Id = 7,
        Name = "Whitepapers",
        Description = "Extensive research and validation documents.",
        ParentCategoryId = 1,
        OptimalCreationTime = 14,
        DisplayOrder = 6,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Abstract', 'type': 'textarea' },
                { 'label': 'Methodology', 'type': 'textarea' },
                { 'label': 'Results', 'type': 'textarea' },
                { 'label': 'References', 'type': 'text' }
            ]
        }"
    },
    // Parent Category: RCA Templates
    new Category
    {
        Id = 8,
        Name = "Root Cause Analysis (RCA)",
        Description = "Templates and methodologies for identifying root causes.",
        ParentCategoryId = null,
        OptimalCreationTime = 0,
        DisplayOrder = 2,
        AllowAttachments = false,
        AllowReferenceLinks = false,
        TemplateJson = "{}"
    },
    // Subcategories under RCA Templates
    new Category
    {
        Id = 9,
        Name = "5 Whys Analysis",
        Description = "Simple RCA technique by asking 'Why' five times.",
        ParentCategoryId = 8,
        OptimalCreationTime = 2,
        DisplayOrder = 1,
        AllowAttachments = false,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Problem Statement', 'type': 'text' },
                { 'label': 'Why 1', 'type': 'text' },
                { 'label': 'Why 2', 'type': 'text' },
                { 'label': 'Why 3', 'type': 'text' },
                { 'label': 'Why 4', 'type': 'text' },
                { 'label': 'Why 5', 'type': 'text' },
                { 'label': 'Root Cause', 'type': 'textarea' }
            ]
        }"
    },
    new Category
    {
        Id = 10,
        Name = "Fishbone Diagram (Ishikawa)",
        Description = "Cause-and-effect diagram for RCA.",
        ParentCategoryId = 8,
        OptimalCreationTime = 5,
        DisplayOrder = 2,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Problem Statement', 'type': 'text' },
                { 'label': 'Causes', 'type': 'textarea' },
                { 'label': 'Fishbone Diagram Image', 'type': 'image' }
            ]
        }"
    },
    new Category
    {
        Id = 11,
        Name = "Failure Mode and Effects Analysis (FMEA)",
        Description = "Proactive RCA technique identifying failure points.",
        ParentCategoryId = 8,
        OptimalCreationTime = 7,
        DisplayOrder = 3,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Component', 'type': 'text' },
                { 'label': 'Potential Failure Mode', 'type': 'text' },
                { 'label': 'Effect of Failure', 'type': 'textarea' },
                { 'label': 'Recommended Action', 'type': 'textarea' }
            ]
        }"
    },
    new Category
    {
        Id = 12,
        Name = "Pareto Analysis (80/20 Rule)",
        Description = "Prioritizes most significant issues for RCA.",
        ParentCategoryId = 8,
        OptimalCreationTime = 4,
        DisplayOrder = 4,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Problem Statement', 'type': 'text' },
                { 'label': 'Contributing Factors', 'type': 'textarea' },
                { 'label': 'Pareto Chart', 'type': 'image' }
            ]
        }"
    },
    new Category
    {
        Id = 13,
        Name = "Fault Tree Analysis (FTA)",
        Description = "Graphical model for identifying root causes.",
        ParentCategoryId = 8,
        OptimalCreationTime = 10,
        DisplayOrder = 5,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Fault Event', 'type': 'text' },
                { 'label': 'Contributing Factors', 'type': 'textarea' }
            ]
        }"
    },
    new Category
    {
        Id = 14,
        Name = "8D Problem Solving",
        Description = "Structured RCA process with eight disciplines.",
        ParentCategoryId = 8,
        OptimalCreationTime = 14,
        DisplayOrder = 6,
        AllowAttachments = true,
        AllowReferenceLinks = true,
        TemplateJson = @"{
            'fields': [
                { 'label': 'Problem Description', 'type': 'text' },
                { 'label': 'Root Cause', 'type': 'textarea' },
                { 'label': 'Corrective Action', 'type': 'textarea' }
            ]
        }"
    }
);




        }
    }
}
