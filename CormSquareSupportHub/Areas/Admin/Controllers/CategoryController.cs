using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Index - List Categories
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

//<<<<<<< HEAD
//=======
//        // GET: Create Category Page
//>>>>>>> e524f3233d44cfc1603dd81100fef59a05af4ae6
        public async Task<IActionResult> Create()
        {
            var categories = await _unitOfWork.Category.GetAllAsync();
            return View(new Category { Categories = categories.ToList() });
        }

        // POST: Create Category
        [HttpPost]
        public async Task<IActionResult> Create(Category obj, List<IFormFile> files, string ReferenceData)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                return View(obj);
            }
            return View();
            //            // Start Transaction
            //            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            //            try
            //            {
            //                // Handle Parent Category Logic
            //                obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

            //                // Auto-assign Display Order
            //                obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
            //                    ((await _unitOfWork.Category.GetAllAsync(c => c.ParentCategoryId == obj.ParentCategoryId))
            //                    .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;

            //                // Save Category
            //                _unitOfWork.Category.Add(obj);
            //                await _unitOfWork.SaveAsync();

            //                // Save Attachments
            //                await ProcessAttachmentsAsync(obj, files);

            //                // Process References
            //                if (!string.IsNullOrEmpty(ReferenceData))
            //                {
            //                    var references = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Reference>>(ReferenceData);
            //                    await SaveReferences(references, obj.Id);
            //                }

            //<<<<<<< HEAD
            //                obj.TemplateJson = string.IsNullOrWhiteSpace(obj.TemplateJson) ? parentCategory.TemplateJson : obj.TemplateJson;
            //            }

            //            _unitOfWork.Category.Add(obj);
            //            await _unitOfWork.SaveAsync();

            //            TempData["success"] = "Category created successfully";
            //            return RedirectToAction("Index");
            //=======
            //                // Commit Transaction
            //                await _unitOfWork.CommitTransactionAsync();

            //                TempData["success"] = "Category created successfully!";
            //                return RedirectToAction("Index");
            //            }
            //            catch (Exception)
            //            {
            //                await _unitOfWork.RollbackTransactionAsync();
            //                ModelState.AddModelError(string.Empty, "An error occurred while creating the category.");
            //                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
            //                return View(obj);
            //            }
            //>>>>>>> e524f3233d44cfc1603dd81100fef59a05af4ae6
        }

        // GET: Edit Category
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var categoryFromDb = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id,
                includeProperties: "Attachments,References"
            );
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            categoryFromDb.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
            ViewData["HtmlContent"] = categoryFromDb.HtmlContent ?? "";
            return View(categoryFromDb);
        }

        // POST: Edit Category
        [HttpPost]
        public async Task<IActionResult> Edit(Category obj, List<IFormFile> files, string? ReferenceData)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                return View(obj);
            }

            // Start Transaction
            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;
                obj.UpdatedBy = 1;
                obj.UpdatedDate = DateTime.Now;

                _unitOfWork.Category.Update(obj);
                await _unitOfWork.SaveAsync();

                // Save new attachments only (Don't delete existing ones)
                await ProcessAttachmentsAsync(obj, files);

                // Process References in Edit
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Reference>>(ReferenceData);
                    await SaveReferences(references, obj.Id);
                }

                // Commit Transaction
                await _unitOfWork.CommitTransactionAsync();

                TempData["success"] = "Category updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                ModelState.AddModelError(string.Empty, "An error occurred while updating the category.");
                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                return View(obj);
            }
        }

        // Reusable Attachment Logic (No Old File Deletion)
        private async Task ProcessAttachmentsAsync(Category obj, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return;

            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string categoryPath = Path.Combine(wwwRootPath, "uploads", "categories", obj.Id.ToString());

            // Create directory if it does not exist
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            // Save new files only
            foreach (var file in files)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(categoryPath, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var attachment = new Attachment
                {
                    FileName = fileName,
                    FilePath = Path.Combine("uploads/categories", obj.Id.ToString(), fileName).Replace("\\", "/"),
                    IsInternal = true,
                    CategoryId = obj.Id
                };

                _unitOfWork.Attachment.Add(attachment);
            }
            await _unitOfWork.SaveAsync();
        }

        // Save References Logic
        private async Task SaveReferences(List<Reference> references, int categoryId)
        {
            foreach (var reference in references)
            {
                var referenceEntity = new Reference
                {
                    Url = reference.Url,
                    Description = reference.Description,
                    IsInternal = reference.IsInternal,
                    OpenOption = reference.OpenOption,
                    CategoryId = categoryId
                };
                _unitOfWork.Reference.Add(referenceEntity);
            }
            await _unitOfWork.SaveAsync();
        }

        // DELETE Attachment (Called from AJAX)
        [HttpPost]
        public async Task<IActionResult> RemoveAttachment(int id)
        {
            var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == id);
            if (attachment == null)
            {
                return Json(new { success = false, message = "Error while deleting attachment." });
            }

            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string filePath = Path.Combine(wwwRootPath, attachment.FilePath.TrimStart('/'));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _unitOfWork.Attachment.Remove(attachment);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Attachment deleted successfully." });
        }
    }
}
