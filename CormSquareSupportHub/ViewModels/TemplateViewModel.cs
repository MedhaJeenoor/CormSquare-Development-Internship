using System.ComponentModel.DataAnnotations;

namespace CormSquareSupportHub.ViewModels
{
    public class TemplateViewModel
    {
        [Required, MaxLength(50)]
        public string TemplateName { get; set; } // Name of the template

        [Required]
        public string TemplateJson { get; set; } // Stores template structure as JSON

        public bool AllowAttachments { get; set; } // Controls file attachment permissions
        public bool AllowReferenceLinks { get; set; } // Controls reference link permissions

        public List<IFormFile>? Attachments { get; set; } // File uploads
        public string? ReferenceLinks { get; set; } // Comma-separated reference links
    }
}
