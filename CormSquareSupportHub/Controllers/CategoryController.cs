using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.Models;

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
        public async Task<IActionResult> Create(Category obj)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = await _db.Categories.ToListAsync(); // Reload categories for dropdown
                return View(obj);
            }

            obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

            if (obj.ParentCategoryId != null)
            {
                var parentExists = await _db.Categories.AnyAsync(c => c.Id == obj.ParentCategoryId);
                if (!parentExists)
                {
                    ModelState.AddModelError("ParentCategoryId", "The selected Parent Category does not exist.");
                    obj.Categories = await _db.Categories.ToListAsync();
                    return View(obj);
                }
            }

            if (string.IsNullOrWhiteSpace(obj.TemplateJson))
            {
                obj.TemplateJson = "{}";
            }

            if (obj.TemplateJson == "{}" && obj.ParentCategoryId != null)
            {
                var parentCategory = await _db.Categories.FindAsync(obj.ParentCategoryId);
                if (parentCategory != null)
                {
                    obj.TemplateJson = parentCategory.TemplateJson;
                }
            }

            if (obj.ParentCategoryId != null)
            {
                var parentCategory = await _db.Categories.FindAsync(obj.ParentCategoryId);
                if (parentCategory != null)
                {
                    obj.AllowAttachments = parentCategory.AllowAttachments;
                    obj.AllowReferenceLinks = parentCategory.AllowReferenceLinks;
                }
            }

            if (obj.DisplayOrder == 0)
            {
                int maxOrder = _db.Categories
                    .Where(c => c.ParentCategoryId == obj.ParentCategoryId)
                    .Max(c => (int?)c.DisplayOrder) ?? 0;

                obj.DisplayOrder = maxOrder + 1;
            }

            _db.Categories.Add(obj);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category created successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            //Fetch category from DB
            Category? categoryFromDb = _db.Categories.Find(id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            //Load categories for dropdown, ensuring the current category is pre-selected
            categoryFromDb.Categories = _db.Categories.ToList();

            return View(categoryFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category obj)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = await _db.Categories.ToListAsync(); // Reload categories for dropdown
                return View(obj);
            }

            //Ensure ParentCategoryId is NULL for parent categories
            obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

            //Validate Parent Category existence
            if (obj.ParentCategoryId != null)
            {
                var parentExists = await _db.Categories.AnyAsync(c => c.Id == obj.ParentCategoryId);
                if (!parentExists)
                {
                    ModelState.AddModelError("ParentCategoryId", "The selected Parent Category does not exist.");
                    obj.Categories = await _db.Categories.ToListAsync();
                    return View(obj);
                }
            }

            //Handle TemplateJson inheritance
            if (string.IsNullOrWhiteSpace(obj.TemplateJson))
            {
                obj.TemplateJson = "{}";
            }
            if (obj.TemplateJson == "{}" && obj.ParentCategoryId != null)
            {
                var parentCategory = await _db.Categories.FindAsync(obj.ParentCategoryId);
                if (parentCategory != null)
                {
                    obj.TemplateJson = parentCategory.TemplateJson;
                }
            }

            //Inherit attachment and reference settings from parent
            if (obj.ParentCategoryId != null)
            {
                var parentCategory = await _db.Categories.FindAsync(obj.ParentCategoryId);
                if (parentCategory != null)
                {
                    obj.AllowAttachments = parentCategory.AllowAttachments;
                    obj.AllowReferenceLinks = parentCategory.AllowReferenceLinks;
                }
            }

            //Auto-fill Display Order if not provided
            if (obj.DisplayOrder <= 0)
            {
                int maxOrder = _db.Categories
                    .Where(c => c.ParentCategoryId == obj.ParentCategoryId)
                    .Max(c => (int?)c.DisplayOrder) ?? 0;

                obj.DisplayOrder = maxOrder + 1;
            }

            //Update existing category instead of adding a new one
            _db.Categories.Update(obj);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category updated successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            //Fetch category from DB
            Category? categoryFromDb = _db.Categories.Find(id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }
    }
}
