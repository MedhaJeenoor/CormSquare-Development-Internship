using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportHub.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string CompanyName { get; set; }

        public string EmployeeID { get; set; }

        [Required]
        public string Country { get; set; }
        [Required(ErrorMessage = "Role selection is required.")]
        public string Role { get; set; }
        [Required]
        public string Password { get; set; }

        [Required]
        [NotMapped]
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Internal User", Text = "Internal User" },
            new SelectListItem { Value = "KM Creator", Text = "KM Creator" },
            new SelectListItem { Value = "KM Champion", Text = "KM Champion" }
        };
    }
}
