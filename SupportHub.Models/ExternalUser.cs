using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SupportHub.Models;

namespace SupportHub.Models
{
    public class ExternalUser : IdentityUser
    {

        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; }
    
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public override string Email { get; set; } // Keep it as an override
        [Required]
        public string Country { get; set; } // Store selected country
        [NotMapped]
        public List<SelectListItem> CountryList { get; set; } // ✅ Correct

        public override string? PhoneNumber { get; set; } // Allow null (optional)

        //public string? Address { get; set; } // Fix spelling from "Adress" to "Address"

        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Employee ID is required")]
        public string EmployeeID { get; set; }
        public bool IsApproved { get; set; } = false; // Default to false

    }
}
