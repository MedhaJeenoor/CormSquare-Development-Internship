using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using SupportHub.Models;
using Microsoft.EntityFrameworkCore;
using SupportHub.Utility.EmailServices;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly UserManager<ExternalUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;

        public AdminController(UserManager<ExternalUser> userManager, EmailService emailService, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _emailService = emailService;
            _roleManager = roleManager;
        }
        public async Task<IActionResult> CreateUser()
        {
            var model = new CreateUserViewModel
            {
                AvailableRoles = (await _roleManager.Roles.ToListAsync())
                                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Internal User", Text = "Internal User" },
                    new SelectListItem { Value = "KM Creator", Text = "KM Creator" },
                    new SelectListItem { Value = "KM Champion", Text = "KM Champion" }
                };
                return View(model);
            }

            var user = new ExternalUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.Name.Split(' ')[0],
                LastName = model.Name.Split(' ').Length > 1 ? model.Name.Split(' ')[1] : "",
                CompanyName = model.CompanyName,
                EmployeeID = model.EmployeeID,
                Country = model.Country,
                IsApproved = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign role
                await _userManager.AddToRoleAsync(user, model.Role);

                TempData["Success"] = "User created successfully!";
                return RedirectToAction("UserApproval"); // Redirect after success
            }
            else
            {
                // Display errors if user creation fails
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // Reload roles in case of an error
            model.AvailableRoles = new List<SelectListItem>
    {
        new SelectListItem { Value = "Internal User", Text = "Internal User" },
        new SelectListItem { Value = "KM Creator", Text = "KM Creator" },
        new SelectListItem { Value = "KM Champion", Text = "KM Champion" }
    };


            return View(model);
        }
        // Helper method to fetch available roles
        private async Task<List<SelectListItem>> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return roles.ConvertAll(r => new SelectListItem { Value = r.Name, Text = r.Name });
        }
        public async Task<IActionResult> UserApproval()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Name = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                CompanyName = user.CompanyName,
                EmployeeID = user.EmployeeID,
                Country = user.Country,
                Role = userRoles.FirstOrDefault() ?? "",
                AvailableRoles = roles
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            // Split Name into FirstName and LastName
            var nameParts = model.Name.Split(' ');
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";

            user.Email = model.Email;
            user.CompanyName = model.CompanyName;
            user.EmployeeID = model.EmployeeID;
            user.Country = model.Country;

            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, existingRoles);

            await _userManager.AddToRoleAsync(user, model.Role);

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "User details updated successfully!";
                return RedirectToAction("UserApproval");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            model.AvailableRoles = _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user != null)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);

                string subject = "Your Account Has Been Approved!";
                string body = $"<p>Dear {user.FirstName},</p><p>Your account has been approved.</p>";

                Console.WriteLine($"Sending email to {user.Email}..."); // Debugging

                await _emailService.SendEmailAsync(user.Email, subject, body);

                Console.WriteLine("Email function executed."); // Debugging

                return RedirectToAction("UserApproval");
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user != null)
            {
                // Send Rejection Email
                string subject = "Your Account Registration Has Been Rejected";
                string body = $"<p>Dear {user.FirstName},</p><p>We regret to inform you that your registration has been rejected.</p>";

                await _emailService.SendEmailAsync(user.Email, subject, body);

                // You may choose to delete the user or keep them for records
                await _userManager.DeleteAsync(user);

                return RedirectToAction("UserApproval");
            }
            return NotFound();
        }
    }
}