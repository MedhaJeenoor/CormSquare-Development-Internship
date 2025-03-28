#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using SupportHub.Models;
using SupportHub.Utility;

namespace CormSquareSupportHub.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ExternalUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ExternalUser> _userManager;
        private readonly IUserStore<ExternalUser> _userStore;
        private readonly IUserEmailStore<ExternalUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ExternalUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<ExternalUser> userStore,
            SignInManager<ExternalUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public List<SelectListItem> CountryList { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required]
            [Phone]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Company Name (Optional)")]
            public string CompanyName { get; set; }

            [Display(Name = "Employee ID (Required if Company Name is provided)")]
            public string EmployeeID { get; set; }

            [Required]
            [Display(Name = "Country")]
            public string Country { get; set; }

            public bool IsApproved { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            CountryList = GetCountryList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = new ExternalUser
                {
                    Email = Input.Email,
                    UserName = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    CompanyName = Input.CompanyName,
                    EmployeeID = Input.EmployeeID,
                    Country = Input.Country,
                    IsApproved = false
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // Ensure "ExternalUser" role exists
                    if (!await _roleManager.RoleExistsAsync("ExternalUser"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("ExternalUser"));
                    }

                    // Assign "ExternalUser" role to the newly created user
                    await _userManager.AddToRoleAsync(user, "ExternalUser");

                    _logger.LogInformation("User registered and assigned ExternalUser role but requires admin approval.");
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            CountryList = GetCountryList();
            return Page();
        }

        private List<SelectListItem> GetCountryList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "United States", Text = "United States" },
                new SelectListItem { Value = "United Kingdom", Text = "United Kingdom" },
                new SelectListItem { Value = "Canada", Text = "Canada" },
                new SelectListItem { Value = "India", Text = "India" },
                new SelectListItem { Value = "Germany", Text = "Germany" },
                new SelectListItem { Value = "Australia", Text = "Australia" },
                new SelectListItem { Value = "France", Text = "France" },
                new SelectListItem { Value = "Japan", Text = "Japan" },
                new SelectListItem { Value = "China", Text = "China" },
                new SelectListItem { Value = "Brazil", Text = "Brazil" }
            };
        }

        private IUserEmailStore<ExternalUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ExternalUser>)_userStore;
        }
    }
}
