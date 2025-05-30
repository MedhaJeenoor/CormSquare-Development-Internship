﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SupportHub.Models
{
    public class SolutionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Product is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid subcategory if applicable.")]
        public int? SubCategoryId { get; set; }

        [StringLength(500, ErrorMessage = "Author cannot exceed 500 characters.")]
        public string Author { get; set; }

        [StringLength(500, ErrorMessage = "Contributors cannot exceed 500 characters.")]
        public string? Contributors { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string HtmlContent { get; set; }

        [StringLength(1000, ErrorMessage = "Issue description cannot exceed 1000 characters.")]
        public string? IssueDescription { get; set; }

        [StringLength(2000, ErrorMessage = "Feedback cannot exceed 2000 characters.")]
        public string? Feedback { get; set; }

        public string Status { get; set; } = "Draft";

        [StringLength(50, ErrorMessage = "DocId cannot exceed 50 characters.")]
        public string DocId { get; set; }
        public DateTime? PublishedDate { get; set; }
        public bool IsInternalTemplate { get; set; } // New field for template checkbox

        public List<Category> Categories { get; set; }
        public List<Product> Products { get; set; }
        public List<SubCategory> SubCategories { get; set; }
        public List<SolutionAttachment> Attachments { get; set; }
        public List<SolutionReference> References { get; set; }
    }
}