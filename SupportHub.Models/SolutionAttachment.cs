using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportHub.Models
{
    public class SolutionAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        [ForeignKey("Solution")]
        public int SolutionId { get; set; }
        public virtual Solution Solution { get; set; }
    }
}
