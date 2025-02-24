using CormSquareSupportHub.Models;
using System.ComponentModel.DataAnnotations;

namespace CormSquareSupportHub.ViewModels
{
    public class CategoryViewModel
    {
        [Required(ErrorMessage = "Category Name is required.")]
        [MaxLength(50, ErrorMessage = "Category Name cannot exceed 50 characters.")]
        public string Name { get; set; }

        public int? ParentCategoryId { get; set; } // Nullable for Parent Categories
        public List<Category>? Categories { get; set; } // Load existing categories

        [Required(ErrorMessage = "Optimal Creation Time is required.")]
        [Range(1, 30, ErrorMessage = "Optimal Creation Time must be between 1 and 30.")]
        public int OptimalCreationTime { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Display Order is required.")]
        public int DisplayOrder { get; set; }

        public bool AllowAttachments { get; set; }
        public bool AllowReferenceLinks { get; set; }

        public string TemplateJson { get; set; } // Stores template structure

        public List<IFormFile>? Attachments { get; set; } // For File Uploads

        [RegularExpression(@"^(https?:\/\/[^\s]+(,[^\s]+)*)?$",
            ErrorMessage = "Invalid URLs. Provide comma-separated valid links.")]
        public string? ReferenceLinks { get; set; }
    }
}
