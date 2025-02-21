using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CormSquareSupportHub.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Category Name cannot exceed 50 characters.")]
        [DisplayName("Category Name")]
        public string Name { get; set; }

        [DisplayName("Display order")]
        [Range(1, 100, ErrorMessage = "Display Order must be between 1-100")]
        public int DisplayOrder { get; set; }

        [DisplayName("Optimal Creation Time (Days)")]
        [Range(1, 30, ErrorMessage = "Optimal Creation Time must be between 1 and 30 days.")]
        public int OptimalCreationTime { get; set; }

        [MaxLength(500)]
        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("Parent Category")]
        public int? ParentCategoryId { get; set; } // Nullable for subcategories
        public virtual Category? ParentCategory { get; set; } // Self-referencing for subcategories

        public virtual ICollection<Category>? SubCategories { get; set; }
    }
}
