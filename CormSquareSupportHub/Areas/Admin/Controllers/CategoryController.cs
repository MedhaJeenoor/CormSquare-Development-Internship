using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AttachmentSettings _attachmentSettings;

        public CategoryController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IOptions<AttachmentSettings> attachmentSettings)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _attachmentSettings = attachmentSettings.Value;
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

        // GET: Create Category Page
        public async Task<IActionResult> Create()
        {
            var categories = await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted);
            return View(new Category { Categories = categories.ToList() });
        }

        // POST: Create Category
        [HttpPost]
        public async Task<IActionResult> Create(Category obj, List<IFormFile>? files, string? ReferenceData, string? AttachmentData)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
                return View(obj);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;
                obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
                    ((await _unitOfWork.Category.GetAllAsync(c => c.ParentCategoryId == obj.ParentCategoryId && !c.IsDeleted))
                    .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;

                obj.UpdateAudit(1); // Assuming userId 1
                _unitOfWork.Category.Add(obj);
                await _unitOfWork.SaveAsync();

                // Process attachments if provided
                if (files?.Count > 0 || !string.IsNullOrEmpty(AttachmentData))
                {
                    await ProcessAttachmentsAsync(obj, files, AttachmentData);
                }

                // Process references if provided
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = JsonConvert.DeserializeObject<List<Reference>>(ReferenceData);
                    await SaveReferences(references, obj.Id);
                }

                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = "Category created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                // Log the exception for debugging
                Console.WriteLine($"Error in Create: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the category: {ex.Message}");
                obj.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
                return View(obj);
            }
        }

        // GET: Edit Category
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id && !c.IsDeleted,
                includeProperties: "Attachments,References"
            );
            if (category == null) return NotFound();

            category.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.Id != id)).ToList();
            ViewData["HtmlContent"] = category.HtmlContent ?? "";
            return View(category);
        }

        // POST: Edit Category
        [HttpPost]
        public async Task<IActionResult> Edit(Category obj, List<IFormFile>? files, string? ReferenceData, string? AttachmentData)
        {
            if (!ModelState.IsValid)
            {
                obj.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
                return View(obj);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                    c => c.Id == obj.Id && !c.IsDeleted,
                    includeProperties: "Attachments,References"
                );
                if (existingCategory == null) return NotFound();

                // Update all fields from the form
                existingCategory.Name = obj.Name;
                existingCategory.Description = obj.Description;
                existingCategory.HtmlContent = obj.HtmlContent;
                existingCategory.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;
                existingCategory.DisplayOrder = existingCategory.DisplayOrder;
                existingCategory.UpdateAudit(1); // Assuming userId 1

                _unitOfWork.Category.Update(existingCategory);
                await _unitOfWork.SaveAsync();

                // Process attachments
                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Count > 0))
                {
                    Console.WriteLine($"Processing attachments. Files count: {files?.Count}, AttachmentData: {AttachmentData}");
                    await ProcessAttachmentsAsync(existingCategory, files, AttachmentData);
                }

                // Process references
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = JsonConvert.DeserializeObject<List<Reference>>(ReferenceData);
                    await SaveReferences(references, existingCategory.Id);
                }

                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = "Category updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Edit: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the category: {ex.Message}");
                obj.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
                return View(obj);
            }
        }
        // GET: Delete Category (Confirmation Page)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id && !c.IsDeleted,
                includeProperties: "Attachments,References"
            );
            if (category == null) return NotFound();

            category.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.Id != id)).ToList();
            ViewData["HtmlContent"] = category.HtmlContent ?? "";
            return View(category);
        }

        // POST: Delete Category (Soft Delete)
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) return NotFound();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Category.SoftDelete(category, 1); // Assuming userId 1
                var attachments = await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == id && !a.IsDeleted);
                foreach (var attachment in attachments)
                {
                    _unitOfWork.Attachment.SoftDelete(attachment, 1);
                }

                var references = await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == id && !r.IsDeleted);
                foreach (var reference in references)
                {
                    _unitOfWork.Reference.SoftDelete(reference, 1);
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = "Category deleted successfully (Soft Delete)!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                TempData["error"] = "An error occurred while deleting the category.";
                return RedirectToAction("Index");
            }
        }

        // Process Attachments
        private async Task ProcessAttachmentsAsync(Category obj, List<IFormFile>? files, string? AttachmentData)
        {
            if (string.IsNullOrEmpty(AttachmentData) && (files == null || files.Count == 0)) return;

            string basePath = _attachmentSettings.UploadPath;
            string categoryPath = Path.Combine(basePath, "categories", obj.Id.ToString());

            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == obj.Id && !a.IsDeleted)).ToList();
            var attachmentInfos = string.IsNullOrEmpty(AttachmentData)
                ? new List<Attachment>()
                : JsonConvert.DeserializeObject<List<Attachment>>(AttachmentData);

            // Update existing attachments
            foreach (var attachmentInfo in attachmentInfos.Where(ai => ai.Id > 0))
            {
                var matchingAttachment = existingAttachments.FirstOrDefault(a => a.Id == attachmentInfo.Id);
                if (matchingAttachment != null)
                {
                    if (matchingAttachment.Caption != attachmentInfo.Caption || matchingAttachment.IsInternal != attachmentInfo.IsInternal)
                    {
                        matchingAttachment.Caption = attachmentInfo.Caption;
                        matchingAttachment.IsInternal = attachmentInfo.IsInternal;
                        matchingAttachment.UpdateAudit(1);
                        _unitOfWork.Attachment.Update(matchingAttachment);
                    }
                }
            }

            // Add new attachments
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(categoryPath, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    var newAttachmentInfo = attachmentInfos.FirstOrDefault(ai => ai.Id == 0 && ai.FileName == file.FileName)
                        ?? new Attachment();
                    var newAttachment = new Attachment
                    {
                        FileName = fileName,
                        FilePath = Path.Combine("categories", obj.Id.ToString(), fileName).Replace("\\", "/"),
                        Caption = newAttachmentInfo.Caption ?? "",
                        IsInternal = newAttachmentInfo.IsInternal,
                        CategoryId = obj.Id
                    };
                    newAttachment.UpdateAudit(1);
                    _unitOfWork.Attachment.Add(newAttachment);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        // Process References
        private async Task SaveReferences(List<Reference> references, int categoryId)
        {
            var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == categoryId && !r.IsDeleted)).ToList();

            foreach (var reference in references)
            {
                var existingReference = existingReferences.FirstOrDefault(r => r.Id == reference.Id);
                if (existingReference != null && reference.Id > 0) // Update existing
                {
                    if (existingReference.Url != reference.Url ||
                        existingReference.Description != reference.Description ||
                        existingReference.IsInternal != reference.IsInternal ||
                        existingReference.OpenOption != reference.OpenOption)
                    {
                        existingReference.Url = reference.Url;
                        existingReference.Description = reference.Description;
                        existingReference.IsInternal = reference.IsInternal;
                        existingReference.OpenOption = reference.OpenOption;
                        existingReference.UpdateAudit(1); // Assuming userId 1
                        _unitOfWork.Reference.Update(existingReference);
                    }
                }
                else if (reference.Id == 0) // Add new
                {
                    var newReference = new Reference
                    {
                        Url = reference.Url,
                        Description = reference.Description,
                        IsInternal = reference.IsInternal,
                        OpenOption = reference.OpenOption,
                        CategoryId = categoryId
                    };
                    newReference.UpdateAudit(1); // Assuming userId 1
                    _unitOfWork.Reference.Add(newReference);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        // DELETE Attachment (Soft Delete via AJAX)
        [HttpPost]
        public async Task<IActionResult> RemoveAttachment(int id)
        {
            try
            {
                var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (attachment == null)
                {
                    return Json(new { success = false, message = "Attachment not found." });
                }

                _unitOfWork.Attachment.SoftDelete(attachment, 1); // Assuming userId 1
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = "Attachment deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveAttachment: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error deleting attachment: {ex.Message}" });
            }
        }

        // DELETE Reference (Soft Delete via AJAX)
        [HttpPost]
        public async Task<IActionResult> RemoveReference(int id)
        {
            try
            {
                var reference = await _unitOfWork.Reference.GetFirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (reference == null)
                {
                    return Json(new { success = false, message = "Reference not found." });
                }

                _unitOfWork.Reference.SoftDelete(reference, 1); // Assuming userId 1
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = "Reference deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveReference: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error deleting reference: {ex.Message}" });
            }
        }
    }
}