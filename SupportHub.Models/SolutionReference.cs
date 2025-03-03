using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportHub.Models
{
    public class SolutionReference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Url { get; set; }

        [ForeignKey("Solution")]
        public int SolutionId { get; set; }
        public virtual Solution Solution { get; set; }
    }
}
