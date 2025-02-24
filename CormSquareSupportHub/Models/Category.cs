using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CormSquareSupportHub.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [DisplayName("Category Name")]
        public string Name { get; set; }

        public int? ParentCategoryId { get; set; } // Nullable for Parent Categories
        public virtual Category? ParentCategory { get; set; } // Self-referencing relationship
        public virtual ICollection<Category>? SubCategories { get; set; }

        [Range(1, 30)]
        [Required]
        public int OptimalCreationTime { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public int DisplayOrder { get; set; }

        public bool AllowAttachments { get; set; }

        public string? TemplateJson { get; set; } // Stores template structure in JSON

        public virtual ICollection<CategoryAttachment>? Attachments { get; set; }
        public virtual ICollection<CategoryReference>? References { get; set; }
    }

    public class CategoryAttachment
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
    }

    public class CategoryReference
    {
        [Key]
        public int Id { get; set; }
        public string Url { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
    }
}
