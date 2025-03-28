using Microsoft.AspNetCore.Mvc;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class IssueController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ExternalUser> _userManager; // Inject UserManager

        public IssueController(IUnitOfWork unitOfWork, UserManager<ExternalUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var userIssues = await _unitOfWork.Issue.GetIssuesByUserAsync(userId);
            return View(userIssues);
        }
        public async Task<IActionResult> IssueList()
        {
            var userId = _userManager.GetUserId(User);
            var userIssues = await _unitOfWork.Issue.GetIssuesByUserAsync(userId);
            return View(userIssues);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new IssueViewModel
            {
                Products = (await _unitOfWork.Product.GetAllAsync()).ToList(),
                SubCategories = new List<SubCategory>() // Initially empty
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(IssueViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User); // Get logged-in user
                if (user == null)
                {
                    return Unauthorized(); // Ensure user is logged in
                }

                var issue = new Issue
                {
                    ProductId = model.ProductId,
                    SubCategoryId = model.SubCategoryId,
                    Description = model.Description,
                    UserId = user.Id, // ✅ Store user.Id (GUID), not EmployeeID
                    Status = "Pending"
                };

                await _unitOfWork.Issue.AddAsync(issue);
                await _unitOfWork.SaveAsync();

                return RedirectToAction("Index");
            }

            model.Products = (await _unitOfWork.Product.GetAllAsync()).ToList();
            model.SubCategories = (await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == model.ProductId)).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetSubCategories(int productId)
        {
            if (productId == 0)
            {
                return Json(new { success = false, message = "Invalid product ID." });
            }

            var subCategories = await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == productId);
            return Json(subCategories.Select(s => new { s.Id, s.Name }));
        }
    }
}
