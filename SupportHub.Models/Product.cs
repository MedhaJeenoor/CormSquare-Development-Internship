using System.ComponentModel.DataAnnotations;

namespace SupportHub.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // e.g., HDFC, Yokogawa

        public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();
    }
}
