using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SupportHub.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Hosting;

namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        //private readonly IWebHostEnvironment _webHostEnvironment;

        //public CategoryController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        //{
        //    _unitOfWork = unitOfWork;
        //    _webHostEnvironment = webHostEnvironment;
        //}
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            List<Category> categories = (await _unitOfWork.Category
                .GetAllAsync(
                    filter: c => c.ParentCategoryId == null && !c.IsDeleted,
                    includeProperties: "SubCategories"
                ))
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            return View(categories);
        }


        public async Task<IActionResult> Create()
        {
            var categories = await _unitOfWork.Category.GetAllAsync();
            return View(new Category { Categories = categories.ToList() });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category obj)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                return View(obj);
            }

            obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

            if (obj.ParentCategoryId != null)
            {
                var parentCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == obj.ParentCategoryId);
                if (parentCategory == null)
                {
                    ModelState.AddModelError("ParentCategoryId", "The selected Parent Category does not exist.");
                    obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                    return View(obj);
                }

                obj.AllowAttachments = parentCategory.AllowAttachments ? obj.AllowAttachments : false;
                obj.AllowReferenceLinks = parentCategory.AllowReferenceLinks ? obj.AllowReferenceLinks : false;
                obj.TemplateJson = string.IsNullOrWhiteSpace(obj.TemplateJson) ? parentCategory.TemplateJson : obj.TemplateJson;
            }

            obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
            ((await _unitOfWork.Category.GetAllAsync(c => c.ParentCategoryId == obj.ParentCategoryId))
            .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;

            _unitOfWork.Category.Add(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category created successfully";
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var categoryFromDb = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            categoryFromDb.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();

            // Ensure TemplateJson is available in ViewData
            ViewData["TemplateJson"] = categoryFromDb.TemplateJson ?? "{}";

            return View(categoryFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category obj)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                return View(obj);
            }

            obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

            if (obj.ParentCategoryId != null)
            {
                var parentCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == obj.ParentCategoryId);

                if (parentCategory == null)
                {
                    ModelState.AddModelError("ParentCategoryId", "The selected Parent Category does not exist.");
                    obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                    return View(obj);
                }

                // Ensure subcategory CANNOT enable what the parent has disabled
                if (!parentCategory.AllowAttachments) obj.AllowAttachments = false;
                if (!parentCategory.AllowReferenceLinks) obj.AllowReferenceLinks = false;

                // Inherit TemplateJson if empty
                obj.TemplateJson = string.IsNullOrWhiteSpace(obj.TemplateJson) ? parentCategory.TemplateJson : obj.TemplateJson;
            }

            // Auto-assign Display Order
            obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
            ((await _unitOfWork.Category.GetAllAsync(c => c.ParentCategoryId == obj.ParentCategoryId))
            .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;


            _unitOfWork.Category.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category updated successfully";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var categoryFromDb = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id,
                includeProperties: "ParentCategory"
                );

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

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // Use Soft Delete from Base Repository
            _unitOfWork.Category.SoftDelete(category, userId: 1);  // TODO: Replace 1 with logged-in user's ID
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Category deleted successfully (Soft Delete)";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorySettings(int id)
        {
            var parentCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id);

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