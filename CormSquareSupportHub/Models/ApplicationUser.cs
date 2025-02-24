using Microsoft.AspNetCore.Identity;

namespace CormSquareSupportHub.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } // Add custom fields if needed
        public bool IsApproved { get; set; } = false; // External user approval status
    }
}
