using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SupportHub.Models
{
    public class Category : AuditableEntity
    {
        // Category Name
        [Required, MaxLength(50)]
        [DisplayName("Category Name")]
        public string Name { get; set; }

        // Parent Category Relationship
        [ForeignKey("ParentCategory")]
        public int? ParentCategoryId { get; set; } // Nullable for Parent Categories
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category>? SubCategories { get; set; }

        // Description (Optional)
        [MaxLength(500)]
        public string? Description { get; set; }

        // Display Order for Sorting
        public int DisplayOrder { get; set; }

        // HTML Content (TinyMCE Content as HTML)
        [Column(TypeName = "NVARCHAR(MAX)")]
        public string? HtmlContent { get; set; }

<<<<<<< HEAD
        public virtual ICollection<Solution>? Solutions { get; set; } // Updated: Solutions linked to Category
       
        //Added for Dropdown in View
=======
        // Attachments Collection (Correctly Mapped)
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        // References Collection (Correctly Mapped)
        public virtual ICollection<Reference> References { get; set; } = new List<Reference>();

        // Load Existing Categories for Dropdown (NOT MAPPED)
>>>>>>> e524f3233d44cfc1603dd81100fef59a05af4ae6
        [NotMapped]
        public List<Category>? Categories { get; set; } // Used for Parent Category Dropdown

        // Determine if the Category is a Subcategory
        public bool IsSubcategory => ParentCategoryId != null;

        // Correctly handle IFormFile for Uploads (NOT MAPPED to DB)
        [NotMapped]
        public List<IFormFile>? UploadAttachments { get; set; } // For file uploads only
    }
}
