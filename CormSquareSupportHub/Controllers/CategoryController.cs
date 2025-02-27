using CormSquareSupportHub.Data;
using CormSquareSupportHub.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CormSquareSupportHub.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            List<Category> categories = await _db.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            var categories = _db.Categories.ToList();
            return View(new Category { Categories = categories });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _db.Categories.ToListAsync(); // Reload categories for dropdown
                return View(model);
            }

            model.ParentCategoryId = model.ParentCategoryId == 0 ? null : model.ParentCategoryId;

            if (model.ParentCategoryId != null)
            {
                var parentExists = await _db.Categories.AnyAsync(c => c.Id == model.ParentCategoryId);
                if (!parentExists)
                {
                    ModelState.AddModelError("ParentCategoryId", "The selected Parent Category does not exist.");
                    model.Categories = await _db.Categories.ToListAsync();
                    return View(model);
                }
            }

            if (string.IsNullOrWhiteSpace(model.TemplateJson))
            {
                model.TemplateJson = "{}";
            }

            if (model.TemplateJson == "{}" && model.ParentCategoryId != null)
            {
                var parentCategory = await _db.Categories.FindAsync(model.ParentCategoryId);
                if (parentCategory != null)
                {
                    model.TemplateJson = parentCategory.TemplateJson;
                }
            }

            if (model.ParentCategoryId != null)
            {
                var parentCategory = await _db.Categories.FindAsync(model.ParentCategoryId);
                if (parentCategory != null)
                {
                    model.AllowAttachments = parentCategory.AllowAttachments;
                    model.AllowReferenceLinks = parentCategory.AllowReferenceLinks;
                }
            }

            if (model.DisplayOrder == 0)
            {
                int maxOrder = _db.Categories
                    .Where(c => c.ParentCategoryId == model.ParentCategoryId)
                    .Max(c => (int?)c.DisplayOrder) ?? 0;

                model.DisplayOrder = maxOrder + 1;
            }

            _db.Categories.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }






    }
}
