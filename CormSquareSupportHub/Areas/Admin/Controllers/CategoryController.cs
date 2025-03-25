using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

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

        // GET: Create Category Page
        public async Task<IActionResult> Create()
        {
            var categories = await _unitOfWork.Category.GetAllAsync();
            return View(new Category { Categories = categories.ToList() });
        }

        // POST: Create Category
        [HttpPost]
        public async Task<IActionResult> Create(Category obj, List<IFormFile> files, string ReferenceData, string AttachmentData)
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
                // Handle Parent Category Logic
                obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;

                // Auto-assign Display Order
                obj.DisplayOrder = obj.DisplayOrder > 0 ? obj.DisplayOrder :
                    ((await _unitOfWork.Category.GetAllAsync(c => c.ParentCategoryId == obj.ParentCategoryId))
                    .Max(c => (int?)c.DisplayOrder) ?? 0) + 1;

                // Save Category
                _unitOfWork.Category.Add(obj);
                await _unitOfWork.SaveAsync();

                // Save Attachments
                await ProcessAttachmentsAsync(obj, files, AttachmentData);

                // Process References
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Reference>>(ReferenceData);
                    await SaveReferences(references, obj.Id);
                }

                // Commit Transaction
                await _unitOfWork.CommitTransactionAsync();

                TempData["success"] = "Category created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                ModelState.AddModelError(string.Empty, "An error occurred while creating the category.");
                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                return View(obj);
            }
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

            // Load Parent Categories
            categoryFromDb.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
            ViewData["HtmlContent"] = categoryFromDb.HtmlContent ?? "";
            return View(categoryFromDb);
        }

        //POST: Edit Category
        [HttpPost]
        public async Task<IActionResult> Edit(Category obj, List<IFormFile> files, string referenceData, string attachmentData)
        {
            Debug.WriteLine($"Attachment Data Received: {referenceData}");
            Debug.WriteLine($"Reference Data Received: {attachmentData}");

            if (!ModelState.IsValid)
            {
                obj.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
                return View(obj);
            }

            // Start Transaction
            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // Fetch the existing category to keep important values (like DisplayOrder)
                var existingCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == obj.Id);
                if (existingCategory == null)
                {
                    return NotFound();
                }

                // Retain the DisplayOrder during Edit
                obj.DisplayOrder = existingCategory.DisplayOrder;

                obj.ParentCategoryId = obj.ParentCategoryId == 0 ? null : obj.ParentCategoryId;
                obj.UpdatedBy = 1;
                obj.UpdatedDate = DateTime.Now;

                // Update Category
                _unitOfWork.Category.Update(obj);
                await _unitOfWork.SaveAsync();

                // Process Attachments (Update or Add New)
                await ProcessAttachmentsAsync(obj, files, attachmentData);

                // Process References (Update or Add New)
                if (!string.IsNullOrEmpty(referenceData))
                {
                    var references = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Reference>>(referenceData);
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

        // Process Attachments (Create and Edit Logic)
        private async Task ProcessAttachmentsAsync(Category obj, List<IFormFile> files, string attachmentData)
        {
            if (files == null && string.IsNullOrEmpty(attachmentData))
                return;
            Debug.WriteLine($"Attachment Data Received: {attachmentData}");

            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string categoryPath = Path.Combine(wwwRootPath, "uploads", "categories", obj.Id.ToString());

            // Create directory if it doesn't exist
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            // Deserialize attachmentData to get metadata (Caption, IsInternal, etc.)
            List<Attachment> attachmentInfos = string.IsNullOrEmpty(attachmentData)
                ? new List<Attachment>()
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<Attachment>>(attachmentData);


            // Get existing attachments from the database
            var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == obj.Id)).ToList();

            // ✅ Update existing attachments if metadata has changed
            foreach (var attachmentInfo in attachmentInfos)
            {
                // Match by Id if provided
                var matchingAttachment = existingAttachments.FirstOrDefault(a => a.Id == attachmentInfo.Id);

                if (matchingAttachment != null)
                {
                    // Check if metadata has changed before updating
                    if (matchingAttachment.IsInternal != attachmentInfo.IsInternal ||
                        matchingAttachment.Caption != attachmentInfo.Caption)
                    {
                        matchingAttachment.IsInternal = attachmentInfo.IsInternal;
                        matchingAttachment.Caption = attachmentInfo.Caption;

                        _unitOfWork.Attachment.Update(matchingAttachment); // ✅ Update the attachment
                    }
                }
            }

            // ✅ Save new files with corresponding metadata
            if (files != null && files.Count > 0)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(categoryPath, fileName);

                    // Save the file to disk
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    // Get metadata for the new file if available
                    var newAttachmentInfo = attachmentInfos.ElementAtOrDefault(i);

                    var newAttachment = new Attachment
                    {
                        FileName = fileName,
                        FilePath = Path.Combine("uploads/categories", obj.Id.ToString(), fileName).Replace("\\", "/"),
                        Caption = newAttachmentInfo?.Caption ?? "",  // ✅ Get correct Caption
                        IsInternal = newAttachmentInfo?.IsInternal ?? false,  // ✅ Get correct IsInternal
                        CategoryId = obj.Id
                    };

                    _unitOfWork.Attachment.Add(newAttachment); // ✅ Add new attachment
                }
            }

            // ✅ Save changes after adding/updating attachments
            await _unitOfWork.SaveAsync();
        }


        // Process References (Create and Edit Logic)
        private async Task SaveReferences(List<Reference> references, int categoryId)
        {
            var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == categoryId)).ToList();

            foreach (var reference in references)
            {
                var existingReference = existingReferences.FirstOrDefault(r => r.Id == reference.Id);

                if (existingReference != null)
                {
                    // ✅ Update if the reference exists and has changed
                    if (existingReference.Url != reference.Url ||
                        existingReference.Description != reference.Description ||
                        existingReference.IsInternal != reference.IsInternal ||
                        existingReference.OpenOption != reference.OpenOption)
                    {
                        existingReference.Url = reference.Url;
                        existingReference.Description = reference.Description;
                        existingReference.IsInternal = reference.IsInternal;
                        existingReference.OpenOption = reference.OpenOption;

                        _unitOfWork.Reference.Update(existingReference);
                    }
                }
                else
                {
                    // ✅ Add new reference if it does not exist
                    var newReference = new Reference
                    {
                        Url = reference.Url,
                        Description = reference.Description,
                        IsInternal = reference.IsInternal,
                        OpenOption = reference.OpenOption,
                        CategoryId = categoryId
                    };

                    _unitOfWork.Reference.Add(newReference);
                }
            }

            // ✅ Save changes after adding/updating references
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
