using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupportHub.Models;

namespace SupportHub.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Reference> References { get; set; }
        public DbSet<ExternalUser> ExternalUsers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Issue> Issues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Explicitly define ExternalUser as the default type
            modelBuilder.Entity<ExternalUser>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<ExternalUser>("ExternalUser");

            // Seed roles if necessary
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Name = "Internal User", NormalizedName = "INTERNAL USER" },
                new IdentityRole { Name = "KM Creator", NormalizedName = "KM CREATOR" },
                new IdentityRole { Name = "KM Champion", NormalizedName = "KM CHAMPION" }
            );
            modelBuilder.Ignore<SelectListGroup>();

            // Seed Initial Data
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Documentation",
                    Description = "Different types of documentation for knowledge management.",
                    ParentCategoryId = null, // It's a parent category
                    DisplayOrder = 1,
                    HtmlContent = ""
                }
            );
            modelBuilder.Entity<Product>()
        .HasMany(p => p.SubCategories)
        .WithOne(s => s.Product)
        .HasForeignKey(s => s.ProductId)
        .OnDelete(DeleteBehavior.Cascade);
            // Define Relationships for Attachments and References
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Attachments)
                .WithOne(a => a.Category)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.References)
                .WithOne(r => r.Category)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            /*modelBuilder.Entity<Category>().HasData(
            // Parent Category: Documentation Types
            new Category
            {
                Id = 1,
                Name = "Documentation",
                Description = "Different types of documentation for knowledge management.",
                ParentCategoryId = null,
                //OptimalCreationTime = 0,
                DisplayOrder = 1,
                HtmlContent = "{}"
            }
            // Subcategories under Documentation
            new Category
            {
                Id = 2,
                Name = "FAQs",
                Description = "Short, direct answers to common questions.",
                ParentCategoryId = 1,
                //OptimalCreationTime = 2,
                DisplayOrder = 1,
                AllowAttachments = false,
                AllowReferenceLinks = false,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Question"", ""type"": ""text"" },
                        { ""label"": ""Answer"", ""type"": ""textarea"", ""editor"": ""tinymce""}
                    ]
                }"
            },
            new Category
            {
                Id = 3,
                Name = "How-To Guides",
                Description = "Step-by-step guides for specific tasks.",
                ParentCategoryId = 1,
                //OptimalCreationTime = 5,
                DisplayOrder = 2,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Title"", ""type"": ""text"" },
                        { ""label"": ""Step-by-Step Instructions"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Screenshots"", ""type"": ""image"" }
                    ]
                }"
            },
            new Category
            {
                Id = 4,
                Name = "Technical Solutions",
                Description = "In-depth troubleshooting and fixes.",
                ParentCategoryId = 1,
                //OptimalCreationTime = 7,
                DisplayOrder = 3,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Problem Statement"", ""type"": ""text"" },
                        { ""label"": ""Solution"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Code Snippets"", ""type"": ""code"" }
                    ]
                }"
            },
            new Category
            {
                Id = 5,
                Name = "Best Practices",
                Description = "General guidelines and industry standards.",
                ParentCategoryId = 1,
                //OptimalCreationTime = 4,
                DisplayOrder = 4,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Guideline Title"", ""type"": ""text"" },
                        { ""label"": ""Description"", ""type"": ""textarea"", ""editor"": ""tinymce"" }
                    ]
                }"
            },
            new Category
            {
                Id = 6,
                Name = "Case Studies",
                Description = "Real-world problem analysis and results.",
                ParentCategoryId = 1,
                //OptimalCreationTime = 10,
                DisplayOrder = 5,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Case Study Title"", ""type"": ""text"" },
                        { ""label"": ""Background"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Findings"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Conclusion"", ""type"": ""textarea"", ""editor"": ""tinymce"" }
                    ]
                }"
            },
            new Category
            {
                Id = 7,
                Name = "Whitepapers",
                Description = "Extensive research and validation documents.",
                ParentCategoryId = 1,
                //OptimalCreationTime = 14,
                DisplayOrder = 6,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Abstract"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Methodology"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Results"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""References"", ""type"": ""text"" }
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
                //OptimalCreationTime = 0,
                DisplayOrder = 2,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = "{}"
            },
            // Subcategories under RCA Templates
            new Category
            {
                Id = 9,
                Name = "5 Whys Analysis",
                Description = "Simple RCA technique by asking \"Why\" five times.",
                ParentCategoryId = 8,
                //OptimalCreationTime = 2,
                DisplayOrder = 1,
                AllowAttachments = false,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Problem Statement"", ""type"": ""text"" },
                        { ""label"": ""Why 1"", ""type"": ""text"" },
                        { ""label"": ""Why 2"", ""type"": ""text"" },
                        { ""label"": ""Why 3"", ""type"": ""text"" },
                        { ""label"": ""Why 4"", ""type"": ""text"" },
                        { ""label"": ""Why 5"", ""type"": ""text"" },
                        { ""label"": ""Root Cause"", ""type"": ""textarea"", ""editor"": ""tinymce"" }
                    ]
                }"
            },
            new Category
            {
                Id = 10,
                Name = "Fishbone Diagram (Ishikawa)",
                Description = "Cause-and-effect diagram for RCA.",
                ParentCategoryId = 8,
                //OptimalCreationTime = 5,
                DisplayOrder = 2,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Problem Statement"", ""type"": ""text"" },
                        { ""label"": ""Causes"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Fishbone Diagram Image"", ""type"": ""image"" }
                    ]
                }"
            },
            new Category
            {
                Id = 11,
                Name = "Failure Mode and Effects Analysis (FMEA)",
                Description = "Proactive RCA technique identifying failure points.",
                ParentCategoryId = 8,
                //OptimalCreationTime = 7,
                DisplayOrder = 3,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Component"", ""type"": ""text"" },
                        { ""label"": ""Potential Failure Mode"", ""type"": ""text"" },
                        { ""label"": ""Effect of Failure"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Recommended Action"", ""type"": ""textarea"", ""editor"": ""tinymce"" }
                    ]
                }"
            },
            new Category
            {
                Id = 12,
                Name = "Pareto Analysis (80/20 Rule)",
                Description = "Prioritizes most significant issues for RCA.",
                ParentCategoryId = 8,
                //OptimalCreationTime = 4,
                DisplayOrder = 4,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Problem Statement"", ""type"": ""text"" },
                        { ""label"": ""Contributing Factors"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Pareto Chart"", ""type"": ""image"" }
                    ]
                }"
            },
            new Category
            {
                Id = 13,
                Name = "Fault Tree Analysis (FTA)",
                Description = "Graphical model for identifying root causes.",
                ParentCategoryId = 8,
                //OptimalCreationTime = 10,
                DisplayOrder = 5,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Fault Event"", ""type"": ""text"" },
                        { ""label"": ""Contributing Factors"", ""type"": ""textarea"", ""editor"": ""tinymce"" }
                    ]
                }"
            },
            new Category
            {
                Id = 14,
                Name = "8D Problem Solving",
                Description = "Structured RCA process with eight disciplines.",
                ParentCategoryId = 8,
                //OptimalCreationTime = 14,
                DisplayOrder = 6,
                AllowAttachments = true,
                AllowReferenceLinks = true,
                TemplateJson = @"{
                    ""fields"": [
                        { ""label"": ""Problem Description"", ""type"": ""text"" },
                        { ""label"": ""Root Cause"", ""type"": ""textarea"", ""editor"": ""tinymce"" },
                        { ""label"": ""Corrective Action"", ""type"": ""textarea"", ""editor"": ""tinymce"" }
                    ]
                }"
            }
        );*/
        }

        // Auto Add CreatedDate & UpdatedDate for AuditableEntity
        public override int SaveChanges()
        {
            ApplyAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();
            var result = await base.SaveChangesAsync(cancellationToken);
            return result;
        }

        // Handle CreatedDate and UpdatedDate for all Auditable Entities
        private void ApplyAuditInfo()
        {
            var entries = ChangeTracker.Entries<AuditableEntity>();

            foreach (var entry in entries)
            {
                int currentUserId = 1; // Default User ID for now

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUserId;
                }

                if (entry.State == EntityState.Modified)
                {
                    if (!entry.Entity.IsDeleted)
                    {
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUserId;
                    }
                }
            }
            var userEntries = ChangeTracker.Entries<ExternalUser>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            foreach (var entry in userEntries)
            {
                Console.WriteLine($"Saving ExternalUser: Email={entry.Entity.Email}, UserName={entry.Entity.UserName}, State={entry.State}");
            }
        }
    }
}
