using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SupportHub.Models
{
    public class IssueViewModel
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int SubCategoryId { get; set; }

        [Required]
        public string Description { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();
        public List<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
