using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CormSquareSupportHub.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _categoryRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _categoryRepo = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            List<Category> categories = await _categoryRepo.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            var categories = _categoryRepo.Categories.AsNoTracking().ToList();
            return View(new Category { Categories = categories });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category obj)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = await _categoryRepo.Categories.AsNoTracking().ToListAsync();
                return View(obj);
            }

            obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

            if (obj.ParentCategoryId != null)
            {
                var parentCategory = await _categoryRepo.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == obj.ParentCategoryId);

                if (parentCategory == null)
                {
                    ModelState.AddModelError("ParentCategoryId", "The selected Parent Category does not exist.");
                    obj.Categories = await _categoryRepo.Categories.AsNoTracking().ToListAsync();
                    return View(obj);
                }

                // Ensure subcategory CANNOT enable what the parent has disabled
                obj.AllowAttachments &= parentCategory.AllowAttachments;
                obj.AllowReferenceLinks &= parentCategory.AllowReferenceLinks;

                // Inherit TemplateJson if empty
                obj.TemplateJson = string.IsNullOrWhiteSpace(obj.TemplateJson) ? parentCategory.TemplateJson : obj.TemplateJson;
            }

            // Auto-assign Display Order
            obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
                (_categoryRepo.Categories
                    .Where(c => c.ParentCategoryId == obj.ParentCategoryId)
                    .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;

            _categoryRepo.Categories.Add(obj);
            await _categoryRepo.SaveChangesAsync();
            TempData["success"] = "Category created successfully";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var categoryFromDb = await _categoryRepo.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            categoryFromDb.Categories = await _categoryRepo.Categories.AsNoTracking().ToListAsync();
            return View(categoryFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category obj)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = await _categoryRepo.Categories.AsNoTracking().ToListAsync();
                return View(obj);
            }

            obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

            if (obj.ParentCategoryId != null)
            {
                var parentCategory = await _categoryRepo.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == obj.ParentCategoryId);

                if (parentCategory == null)
                {
                    ModelState.AddModelError("ParentCategoryId", "The selected Parent Category does not exist.");
                    obj.Categories = await _categoryRepo.Categories.AsNoTracking().ToListAsync();
                    return View(obj);
                }

                // Ensure subcategory CANNOT enable what the parent has disabled
                obj.AllowAttachments &= parentCategory.AllowAttachments;
                obj.AllowReferenceLinks &= parentCategory.AllowReferenceLinks;

                // Inherit TemplateJson if empty
                obj.TemplateJson = string.IsNullOrWhiteSpace(obj.TemplateJson) ? parentCategory.TemplateJson : obj.TemplateJson;
            }

            // Auto-assign Display Order
            obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
                (_categoryRepo.Categories
                    .Where(c => c.ParentCategoryId == obj.ParentCategoryId)
                    .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;

            _categoryRepo.Categories.Update(obj);
            await _categoryRepo.SaveChangesAsync();
            TempData["success"] = "Category updated successfully";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var categoryFromDb = await _categoryRepo.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

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

            var category = await _categoryRepo.Categories.FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            _categoryRepo.Categories.Remove(category);
            await _categoryRepo.SaveChangesAsync();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorySettings(int id)
        {
            var parentCategory = await _categoryRepo.Categories.FindAsync(id);

            if (parentCategory == null)
            {
                return NotFound();
            }

            return Json(new
            {
                allowAttachments = parentCategory.AllowAttachments,
                allowReferenceLinks = parentCategory.AllowReferenceLinks
            });
        }

    }
}
