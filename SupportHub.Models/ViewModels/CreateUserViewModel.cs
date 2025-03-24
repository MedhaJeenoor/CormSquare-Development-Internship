using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SupportHub.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        public string CompanyName { get; set; }
        public string EmployeeID { get; set; }

        [Required(ErrorMessage = "Role selection is required.")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }
        [Required]
        public string Country { get; set; } // Store selected country
        [NotMapped]
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Internal User", Text = "Internal User" },
            new SelectListItem { Value = "KM Creator", Text = "KM Creator" },
            new SelectListItem { Value = "KM Champion", Text = "KM Champion" }
        };
    }
}
