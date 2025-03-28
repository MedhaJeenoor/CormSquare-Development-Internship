using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportHub.Models
{
    public class Issue : AuditableEntity
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int? SubCategoryId { get; set; }
        public SubCategory SubCategory { get; set; }
        public string UserId { get; set; } // External User who raised the issue
        public ExternalUser User { get; set; } // Assuming you have ExternalUser model
        public string Description { get; set; }
        public string Status { get; set; } = "Pending"; // Default status
        public string? AdminResponse { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
