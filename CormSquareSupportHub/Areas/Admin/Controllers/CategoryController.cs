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
using Microsoft.AspNetCore.Identity;

namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AttachmentSettings _attachmentSettings;
        private readonly UserManager<ExternalUser> _userManager;

        public CategoryController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            IOptions<AttachmentSettings> attachmentSettings,
            UserManager<ExternalUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _attachmentSettings = attachmentSettings.Value;
            _userManager = userManager;
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("User not authenticated");
                return Unauthorized();
            }

            Console.WriteLine($"POST Create called. Category Name: {obj.Name}, Files: {files?.Count ?? 0}, ReferenceData: {ReferenceData}, AttachmentData: {AttachmentData}");

            // Set audit fields and other required fields before validation
            obj.UpdateAudit(user.Id);
            obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;
            obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
                ((await _unitOfWork.Category.GetAllAsync(c => c.ParentCategoryId == obj.ParentCategoryId && !c.IsDeleted))
                .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;

            // Log ModelState details
            Console.WriteLine("ModelState details before validation:");
            foreach (var key in ModelState.Keys)
            {
                var value = ModelState[key];
                Console.WriteLine($"Key: {key}, Value: {value.RawValue ?? "null"}, Errors: {string.Join(", ", value.Errors.Select(e => e.ErrorMessage))}");
            }

            // Clear ModelState for server-set fields to avoid false positives
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("DisplayOrder");
            ModelState.Remove("IsDeleted");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                obj.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
                return View(obj);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Category.Add(obj);
                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Category added with ID: {obj.Id}");

                if (files?.Count > 0 || !string.IsNullOrEmpty(AttachmentData))
                {
                    await ProcessAttachmentsAsync(obj, files, AttachmentData, user.Id);
                }

                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = JsonConvert.DeserializeObject<List<Reference>>(ReferenceData);
                    await SaveReferences(references, obj.Id, user.Id);
                }

                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = "Category created successfully!";
                Console.WriteLine("Category creation successful, redirecting to Index");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("User not authenticated in Edit");
                return Unauthorized();
            }

            Console.WriteLine($"POST Edit called. Category ID: {obj.Id}, Name: {obj.Name}, Files: {files?.Count ?? 0}, ReferenceData: {ReferenceData}, AttachmentData: {AttachmentData}");

            // Set audit fields before validation
            obj.UpdateAudit(user.Id);

            // Log ModelState details
            Console.WriteLine("ModelState details before validation (Edit):");
            foreach (var key in ModelState.Keys)
            {
                var value = ModelState[key];
                Console.WriteLine($"Key: {key}, Value: {value.RawValue ?? "null"}, Errors: {string.Join(", ", value.Errors.Select(e => e.ErrorMessage))}");
            }

            // Clear ModelState for server-set fields
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("UpdatedBy");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("DisplayOrder");
            ModelState.Remove("IsDeleted");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error in Edit: {error.ErrorMessage}");
                }
                obj.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
                ViewData["HtmlContent"] = obj.HtmlContent ?? "";
                return View(obj);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                    c => c.Id == obj.Id && !c.IsDeleted,
                    includeProperties: "Attachments,References"
                );
                if (existingCategory == null)
                {
                    Console.WriteLine($"Category with ID {obj.Id} not found or already deleted");
                    return NotFound();
                }

                // Update only the fields that should change
                existingCategory.Name = obj.Name;
                existingCategory.Description = obj.Description;
                existingCategory.HtmlContent = obj.HtmlContent;
                existingCategory.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;
                existingCategory.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder : existingCategory.DisplayOrder; // Preserve if not submitted
                existingCategory.UpdateAudit(user.Id); // Ensure audit fields are updated

                _unitOfWork.Category.Update(existingCategory);
                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Category updated with ID: {existingCategory.Id}");

                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Count > 0))
                {
                    await ProcessAttachmentsAsync(existingCategory, files, AttachmentData, user.Id);
                }

                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = JsonConvert.DeserializeObject<List<Reference>>(ReferenceData);
                    await SaveReferences(references, existingCategory.Id, user.Id);
                }

                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = "Category updated successfully!";
                Console.WriteLine("Category update successful, redirecting to Index");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Edit: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the category: {ex.Message}");
                obj.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
                ViewData["HtmlContent"] = obj.HtmlContent ?? "";
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) return NotFound();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Category.SoftDelete(category, user.Id);
                var attachments = await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == id && !a.IsDeleted);
                foreach (var attachment in attachments)
                {
                    _unitOfWork.Attachment.SoftDelete(attachment, user.Id);
                }

                var references = await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == id && !r.IsDeleted);
                foreach (var reference in references)
                {
                    _unitOfWork.Reference.SoftDelete(reference, user.Id);
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

        private async Task ProcessAttachmentsAsync(Category obj, List<IFormFile>? files, string? AttachmentData, string userId)
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

            foreach (var attachmentInfo in attachmentInfos.Where(ai => ai.Id > 0))
            {
                var matchingAttachment = existingAttachments.FirstOrDefault(a => a.Id == attachmentInfo.Id);
                if (matchingAttachment != null)
                {
                    if (matchingAttachment.Caption != attachmentInfo.Caption || matchingAttachment.IsInternal != attachmentInfo.IsInternal)
                    {
                        matchingAttachment.Caption = attachmentInfo.Caption;
                        matchingAttachment.IsInternal = attachmentInfo.IsInternal;
                        matchingAttachment.UpdateAudit(userId);
                        _unitOfWork.Attachment.Update(matchingAttachment);
                    }
                }
            }

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
                    newAttachment.UpdateAudit(userId);
                    _unitOfWork.Attachment.Add(newAttachment);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        private async Task SaveReferences(List<Reference> references, int categoryId, string userId)
        {
            var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == categoryId && !r.IsDeleted)).ToList();

            foreach (var reference in references)
            {
                var existingReference = existingReferences.FirstOrDefault(r => r.Id == reference.Id);
                if (existingReference != null && reference.Id > 0)
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
                        existingReference.UpdateAudit(userId);
                        _unitOfWork.Reference.Update(existingReference);
                    }
                }
                else if (reference.Id == 0)
                {
                    var newReference = new Reference
                    {
                        Url = reference.Url,
                        Description = reference.Description,
                        IsInternal = reference.IsInternal,
                        OpenOption = reference.OpenOption,
                        CategoryId = categoryId
                    };
                    newReference.UpdateAudit(userId);
                    _unitOfWork.Reference.Add(newReference);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAttachment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (attachment == null)
                {
                    return Json(new { success = false, message = "Attachment not found." });
                }

                _unitOfWork.Attachment.SoftDelete(attachment, user.Id);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = "Attachment deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveAttachment: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error deleting attachment: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveReference(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var reference = await _unitOfWork.Reference.GetFirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (reference == null)
                {
                    return Json(new { success = false, message = "Reference not found." });
                }

                _unitOfWork.Reference.SoftDelete(reference, user.Id);
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