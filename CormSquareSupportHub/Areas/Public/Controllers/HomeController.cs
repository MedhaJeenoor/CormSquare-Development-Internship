using Microsoft.AspNetCore.Mvc;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CormSquareSupportHub.Areas.Public.Controllers
{
    [Area("Public")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            // Persist search term in ViewData for the view
            ViewData["searchString"] = searchString;

            // Get approved solutions with related data
            var solutions = _unitOfWork.Solution.GetApprovedSolutions(
                includeProperties: "Category,Product,SubCategory,Author"
            );

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.ToLower();
                solutions = solutions.Where(s =>
                    (!string.IsNullOrEmpty(s.Title) && s.Title.ToLower().Contains(searchString)) ||
                    (s.Category != null && !string.IsNullOrEmpty(s.Category.Name) && s.Category.Name.ToLower().Contains(searchString)) ||
                    (s.Product != null && !string.IsNullOrEmpty(s.Product.ProductName) && s.Product.ProductName.ToLower().Contains(searchString)) ||
                    (s.SubCategory != null && !string.IsNullOrEmpty(s.SubCategory.Name) && s.SubCategory.Name.ToLower().Contains(searchString)) ||
                    (s.Author != null && !string.IsNullOrEmpty(s.Author.FirstName) && s.Author.FirstName.ToLower().Contains(searchString))
                ).ToList();
            }

            // Get distinct ProductNames from solutions
            var solutionProductNames = solutions
                .Select(s => s.Product?.ProductName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();

            // Fetch all products and their subcategories
            var products = await _unitOfWork.Product.GetAllAsync(
                includeProperties: "SubCategories"
            );
            var clientProducts = products
                .Select(p => new
                {
                    p.ProductName,
                    SubCategories = p.SubCategories
                        .Select(s => s.Name)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList()
                })
                .Where(p => !string.IsNullOrEmpty(p.ProductName))
                .ToDictionary(
                    p => p.ProductName,
                    p => p.SubCategories
                );

            // Ensure all ProductNames from solutions are in clientProducts (with empty list if no subcategories)
            foreach (var productName in solutionProductNames)
            {
                if (!clientProducts.ContainsKey(productName))
                {
                    clientProducts[productName] = new List<string>();
                }
            }

            // Pass clientProducts to the view via ViewData
            ViewData["ClientProducts"] = clientProducts;

            _logger.LogInformation("Retrieved {Count} approved solutions with search term: {Search}", solutions.Count, searchString ?? "none");

            return View(solutions);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Details(int id)
        {
            var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(
                s => s.Id == id,
                includeProperties: "Category,Product,SubCategory,Attachments,References,Author"
            );

            if (solution == null)
            {
                _logger.LogWarning("Solution with ID {Id} not found", id);
                return NotFound();
            }

            var viewModel = new SolutionViewModel
            {
                Id = solution.Id,
                DocId = solution.DocId,
                Author = solution.Author?.FirstName,
                Contributors = solution.Contributors,
                Title = solution.Title,
                CategoryId = solution.CategoryId,
                ProductId = solution.ProductId,
                SubCategoryId = solution.SubCategoryId,
                HtmlContent = solution.HtmlContent,
                IssueDescription = solution.IssueDescription,
                Feedback = solution.Feedback,
                Status = solution.Status,
                Attachments = solution.Attachments?.ToList() ?? new List<SolutionAttachment>(),
                References = solution.References?.ToList() ?? new List<SolutionReference>(),
                Categories = new List<Category>(),
                Products = new List<Product>(),
                SubCategories = new List<SubCategory>()
            };

            if (User.IsInRole("ExternalUser"))
            {
                viewModel.Attachments = viewModel.Attachments
                    .Where(a => !a.IsInternal)
                    .ToList();
                viewModel.References = viewModel.References
                    .Where(r => !r.IsInternal)
                    .ToList();
            }

            return View("Details", viewModel);
        }
    }
}