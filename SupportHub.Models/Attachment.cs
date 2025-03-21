using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportHub.Models
{
    public class Attachment : AuditableEntity
    {
        [Required]
        [MaxLength(255, ErrorMessage = "File name cannot exceed 255 characters.")]
        public string FileName { get; set; }

        [Required]
        [MaxLength(500, ErrorMessage = "File path cannot exceed 500 characters.")]
        public string FilePath { get; set; }

        public string? Caption { get; set; }
        public bool IsInternal { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        // Attachments Linked to Solutions (Independent Copies)
        //public virtual ICollection< SolutionAttachment>? SolutionAttachments { get; set; }
    }
}
