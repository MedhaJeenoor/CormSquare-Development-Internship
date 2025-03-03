using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SupportHub.Models
{
    public class Solution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [DisplayName("Solution Title")]
        public string Title { get; set; } = string.Empty;

        [ForeignKey("Category")]
        [DisplayName("Category")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        [ForeignKey("Product")]
        [DisplayName("Product")]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Column(TypeName = "NVARCHAR(MAX)")]
        [DisplayName("Solution Content")]
        public string Content { get; set; } = string.Empty;

        [Required]
        [DisplayName("Document ID")]
        public string DocumentId { get; set; } = string.Empty; // Auto-generated ID

        public virtual ICollection<SolutionReference> References { get; set; } = new List<SolutionReference>();
        public virtual ICollection<SolutionAttachment> Attachments { get; set; } = new List<SolutionAttachment>();

        // Added for handling file uploads in forms
        [NotMapped]
        public List<IFormFile>? AttachmentFiles { get; set; }
    }
}
