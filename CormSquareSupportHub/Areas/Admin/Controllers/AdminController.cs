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
                model.AvailableRoles = await GetRoles();
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
                IsApproved = false,
                AssignedRole = model.Role // Set AssignedRole here
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign role if selected, otherwise assign a default role (e.g., "ExternalUser")
                if (!string.IsNullOrEmpty(model.Role))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(model.Role);
                    if (roleExists)
                    {
                        var roleAssignResult = await _userManager.AddToRoleAsync(user, model.Role);
                        if (!roleAssignResult.Succeeded)
                        {
                            foreach (var error in roleAssignResult.Errors)
                            {
                                ModelState.AddModelError("", $"Failed to assign role: {error.Description}");
                            }
                            model.AvailableRoles = await GetRoles();
                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Role '{model.Role}' does not exist.");
                        model.AvailableRoles = await GetRoles();
                        return View(model);
                    }
                }
                else
                {
                    // Optionally assign a default role if no role is selected
                    user.AssignedRole = "ExternalUser"; // Set default AssignedRole
                    var defaultRoleResult = await _userManager.AddToRoleAsync(user, "ExternalUser");
                    if (!defaultRoleResult.Succeeded)
                    {
                        foreach (var error in defaultRoleResult.Errors)
                        {
                            ModelState.AddModelError("", $"Failed to assign default role: {error.Description}");
                        }
                        model.AvailableRoles = await GetRoles();
                        return View(model);
                    }
                }

                TempData["Success"] = "User created successfully!";
                return RedirectToAction("UserApproval");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.AvailableRoles = await GetRoles();
            return View(model);
        }

        // Helper method to fetch available roles
        private async Task<List<SelectListItem>> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
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

            var roles = await _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Name = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                CompanyName = user.CompanyName,
                EmployeeID = user.EmployeeID,
                Country = user.Country,
                Role = user.AssignedRole ?? userRoles.FirstOrDefault() ?? "ExternalUser", // Use AssignedRole if available
                AvailableRoles = roles
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            // Update user details
            var nameParts = model.Name.Split(' ');
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            user.Email = model.Email;
            user.UserName = model.Email; // Ensure UserName stays in sync with Email
            user.CompanyName = model.CompanyName;
            user.EmployeeID = model.EmployeeID;
            user.Country = model.Country;
            user.AssignedRole = model.Role; // Set AssignedRole here

            // Update role
            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, existingRoles);

            var roleAssignResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleAssignResult.Succeeded)
            {
                foreach (var error in roleAssignResult.Errors)
                {
                    ModelState.AddModelError("", $"Failed to assign role: {error.Description}");
                }
                model.AvailableRoles = await GetRoles();
                return View(model);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User details updated successfully!";
                return RedirectToAction("UserApproval");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            model.AvailableRoles = await GetRoles();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user != null)
            {
                user.IsApproved = true;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    string subject = "Your Account Has Been Approved!";
                    string body = $"<p>Dear {user.FirstName},</p><p>Your account has been approved.</p>";
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    return RedirectToAction("UserApproval");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return RedirectToAction("UserApproval"); // Return with errors if update fails
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user != null)
            {
                string subject = "Your Account Registration Has Been Rejected";
                string body = $"<p>Dear {user.FirstName},</p><p>We regret to inform you that your registration has been rejected.</p>";
                await _emailService.SendEmailAsync(user.Email, subject, body);

                await _userManager.DeleteAsync(user);
                return RedirectToAction("UserApproval");
            }
            return NotFound();
        }
    }
}