using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportHub.Models
{
    public class Product : AuditableEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public List<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
