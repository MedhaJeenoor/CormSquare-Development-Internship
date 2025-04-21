using Microsoft.AspNetCore.Mvc;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.Diagnostics;

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

        public IActionResult Index()
        {
            var approvedSolutions = _unitOfWork.Solution.GetApprovedSolutions();
            return View(approvedSolutions);
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
                includeProperties: "Category,Product,SubCategory,Attachments,References"
            );

            if (solution == null)
                return NotFound();

            var viewModel = new SolutionViewModel
            {
                Id = solution.Id,
                Title = solution.Title,
                CategoryId = solution.CategoryId,
                ProductId = solution.ProductId,
                SubCategoryId = solution.SubCategoryId,
                Contributors = solution.Contributors,
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

            // Filter out internal attachments and references for ExternalUser role
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