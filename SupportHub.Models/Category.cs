using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SupportHub.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [DisplayName("Category Name")]
        public string Name { get; set; }

        public int? ParentCategoryId { get; set; } // Nullable for Parent Categories
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category>? SubCategories { get; set; }

        [Range(0, 30, ErrorMessage = "For parent categories, it can be 0-30. For subcategories, it must be 1-30.")]
        [Required]
        [DisplayName("Optimal Creation Time")]
        public int OptimalCreationTime { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Display Order cannot be negative.")]
        [DisplayName("Display Order")]
        public int DisplayOrder { get; set; }

        public bool AllowAttachments { get; set; }
        public bool AllowReferenceLinks { get; set; } = false;

        [Column(TypeName = "NVARCHAR(MAX)")]
        public string? TemplateJson { get; set; }

        public virtual ICollection<CategoryAttachment>? Attachments { get; set; }
        public virtual ICollection<CategoryReference>? References { get; set; }

        //Added for Dropdown in View
        [NotMapped]
        public List<Category>? Categories { get; set; } // Load existing categories for dropdown

        //Added for handling file uploads
        [NotMapped]
        public List<IFormFile>? AttachmentFiles { get; set; }
    }
}
