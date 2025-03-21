using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportHub.Models
{
    public class Reference : AuditableEntity
    {

        [Required]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        [MaxLength(500, ErrorMessage = "URL cannot exceed 500 characters.")]
        public string Url { get; set; }

        [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; } // Optional description
        public string OpenOption { get; set; }

        public bool IsInternal { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        // References Linked to Solutions (Independent Copies)
        //public virtual ICollection<SolutionReferences>? SolutionReferences { get; set; }
    }
}

