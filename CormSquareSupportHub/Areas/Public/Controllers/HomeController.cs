using Microsoft.AspNetCore.Mvc;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.IO;

namespace CormSquareSupportHub.Areas.Public.Controllers
{
    [Area("Public")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;
        private readonly AttachmentSettings _attachmentSettings;

        public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger, IOptions<AttachmentSettings> attachmentSettings)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _attachmentSettings = attachmentSettings.Value;
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
                PublishedDate = solution.PublishedDate,
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

        [HttpGet]
        [Route("Public/Home/DownloadAttachment/{id?}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _unitOfWork.SolutionAttachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (attachment == null)
            {
                _logger.LogWarning("Attachment with ID {Id} not found", id);
                return NotFound();
            }

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath).Replace('/', Path.DirectorySeparatorChar);
            _logger.LogInformation("Attempting to access attachment at path: {Path}", fullPath);

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Attachment file not found at path: {Path}", fullPath);
                return NotFound();
            }

            try
            {
                // Determine MIME type and Content-Disposition
                string mimeType = "application/octet-stream";
                string contentDisposition = "attachment"; // Default to download
                string ext = Path.GetExtension(attachment.FileName)?.ToLowerInvariant();

                if (ext != null)
                {
                    switch (ext)
                    {
                        case ".pdf":
                            mimeType = "application/pdf";
                            contentDisposition = "inline"; // Allow opening in browser
                            break;
                        case ".png":
                            mimeType = "image/png";
                            contentDisposition = "inline";
                            break;
                        case ".jpg":
                        case ".jpeg":
                            mimeType = "image/jpeg";
                            contentDisposition = "inline";
                            break;
                        case ".txt":
                            mimeType = "text/plain";
                            contentDisposition = "inline";
                            break;
                        case ".docx":
                            mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                            contentDisposition = "attachment"; // Force download for non-viewable types
                            break;
                        default:
                            mimeType = "application/octet-stream";
                            contentDisposition = "attachment";
                            break;
                    }
                }

                var fileStream = System.IO.File.OpenRead(fullPath);
                _logger.LogInformation("Serving attachment ID {Id}: {FileName}, MIME: {MimeType}, Disposition: {Disposition}", id, attachment.FileName, mimeType, contentDisposition);

                // Set Content-Disposition header
                Response.Headers.Add("Content-Disposition", $"{contentDisposition}; filename=\"{attachment.FileName}\"");
                return File(fileStream, mimeType);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO Error accessing attachment ID {Id} at path: {Path}", id, fullPath);
                return StatusCode(500, "Error reading the attachment file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error serving attachment ID {Id}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}