using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Humanizer;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;
//using System.Net.Mail;
using System.Reflection.Metadata;
using System.Threading.Channels;

namespace SupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ExternalUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AttachmentSettings _attachmentSettings;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            IUnitOfWork unitOfWork,
            UserManager<ExternalUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IOptions<AttachmentSettings> attachmentSettings)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _attachmentSettings = attachmentSettings.Value;

            if (string.IsNullOrEmpty(_attachmentSettings.UploadPath))
            {
                throw new InvalidOperationException("UploadPath is not configured in appsettings.json.");
            }

            Console.WriteLine($"UploadPath configured: {_attachmentSettings.UploadPath} (Absolute: {Path.GetFullPath(_attachmentSettings.UploadPath)})");

            try
            {
                if (!Directory.Exists(_attachmentSettings.UploadPath))
                {
                    Directory.CreateDirectory(_attachmentSettings.UploadPath);
                    Console.WriteLine($"Created base upload directory: {_attachmentSettings.UploadPath}");
                }
                else
                {
                    Console.WriteLine($"Base upload directory exists: {_attachmentSettings.UploadPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating UploadPath {_attachmentSettings.UploadPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new InvalidOperationException($"Invalid UploadPath: {_attachmentSettings.UploadPath}", ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found.");
                return Unauthorized();
            }

            var categories = await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted, includeProperties: "ParentCategory");
            var viewModel = categories
                .Where(c => c.ParentCategoryId == null)
                .Select(c => new Category
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    SubCategories = categories
                        .Where(sc => sc.ParentCategoryId == c.Id && !sc.IsDeleted)
                        .ToList()
                }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in Create GET.");
                return Unauthorized();
            }

            var category = new Category
            {
                Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.ParentCategoryId == null)).ToList()
            };
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile[] files, string AttachmentData, string ReferenceData, List<int> deletedAttachmentIds, List<int> deletedReferenceIds)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            Response.Headers["Vary"] = "Accept-Encoding";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in Create POST.");
                return Unauthorized();
            }

            // Log form data
            Console.WriteLine($"Form submission: Name={category.Name}, ParentCategoryId={category.ParentCategoryId}, FilesCount={(files?.Length ?? 0)}, AttachmentData={AttachmentData}, ReferenceData={ReferenceData}, DeletedAttachmentIds={string.Join(",", deletedAttachmentIds ?? new List<int>())}, DeletedReferenceIds={string.Join(",", deletedReferenceIds ?? new List<int>())}");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"Form key: {key}, Value: {Request.Form[key]}");
            }

            // Remove unnecessary ModelState entries
            ModelState.Remove("Attachments");
            ModelState.Remove("References");
            ModelState.Remove("ParentCategory");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("UpdatedBy");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("IsDeleted");

            // Validate ParentCategoryId
            if (category.ParentCategoryId.HasValue && category.ParentCategoryId > 0)
            {
                var parentExists = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == category.ParentCategoryId.Value && !c.IsDeleted);
                if (parentExists == null)
                {
                    ModelState.AddModelError("ParentCategoryId", "Selected parent category does not exist or is deleted.");
                    Console.WriteLine($"Invalid ParentCategoryId: {category.ParentCategoryId}");
                }
            }

            // Check for duplicate category
            var existingCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Name == category.Name && c.ParentCategoryId == category.ParentCategoryId && !c.IsDeleted);
            if (existingCategory != null)
            {
                ModelState.AddModelError("Name", "A category with this name and parent already exists.");
                Console.WriteLine($"Duplicate category detected: Name={category.Name}, ParentCategoryId={category.ParentCategoryId}");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine($"ModelState invalid: {string.Join(", ", errors)}");
                category.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.ParentCategoryId == null)).ToList();
                return Json(new { success = false, message = "Validation failed.", errors });
            }

            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                // Double-check for duplicates
                existingCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Name == category.Name && c.ParentCategoryId == category.ParentCategoryId && !c.IsDeleted);
                if (existingCategory != null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    ModelState.AddModelError("Name", "A category with this name and parent was created concurrently.");
                    Console.WriteLine($"Concurrent duplicate detected: Name={category.Name}, ParentCategoryId={category.ParentCategoryId}");
                    category.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.ParentCategoryId == null)).ToList();
                    return Json(new { success = false, message = "Concurrent submission detected.", errors = new[] { "A category with this name and parent was created concurrently." } });
                }

                // Set audit fields
                category.UpdateAudit(user.Id);
                category.IsDeleted = false;
                category.ParentCategoryId = category.ParentCategoryId == 0 ? null : category.ParentCategoryId;

                Console.WriteLine($"Saving category: Name={category.Name}, ParentCategoryId={category.ParentCategoryId}");

                // Set DisplayOrder
                if (category.DisplayOrder == 0)
                {
                    var maxOrder = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).Max(c => (int?)c.DisplayOrder) ?? 0;
                    category.DisplayOrder = maxOrder + 1;
                }

                // Add category
                _unitOfWork.Category.Add(category);
                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Created category: Id={category.Id}, Name={category.Name}, ParentCategoryId={category.ParentCategoryId}");

                // Create category folder
                string categoryPath = Path.Combine(_attachmentSettings.UploadPath, "categories", category.Id.ToString());
                try
                {
                    if (!Directory.Exists(categoryPath))
                    {
                        Directory.CreateDirectory(categoryPath);
                        Console.WriteLine($"Created directory: {categoryPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating directory {categoryPath}: {ex.Message}");
                    throw new Exception($"Failed to create directory: {categoryPath}", ex);
                }

                // Process HtmlContent media
                if (!string.IsNullOrEmpty(category.HtmlContent))
                {
                    category.HtmlContent = await ProcessHtmlContentMediaAsync(category, category.HtmlContent, user.Id, files?.ToList());
                    _unitOfWork.Category.Update(category);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Processed HtmlContent media for category {category.Id}");
                }

                // Initialize collections
                var savedReferences = new List<object>();
                var savedAttachments = new List<object>();
                List<(Attachment Entity, string SourcePath, string DestPath, string OriginalFileName)> stagedAttachments = new();

                // Process Parent Category Attachments and References
                await ProcessParentCategoryAsync(category, user.Id, deletedAttachmentIds, deletedReferenceIds, AttachmentData, ReferenceData, savedAttachments, savedReferences, stagedAttachments);

                // Process Form References
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var formReferences = await ProcessReferencesAsync(category, ReferenceData, user.Id, isCreateAction: true);
                    savedReferences.AddRange(formReferences);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Saved {formReferences.Count} form references for category {category.Id}");
                }

                // Process New Attachments from AttachmentData and Files
                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Any() ?? false))
                {
                    var newAttachmentsFromData = new List<Dictionary<string, object>>();
                    if (!string.IsNullOrEmpty(AttachmentData))
                    {
                        var attachmentDataItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(AttachmentData);
                        newAttachmentsFromData = attachmentDataItems
                            .Where(att =>
                            {
                                int id = att.ContainsKey("id") ? Convert.ToInt32(att["id"]) : 0;
                                bool fromParent = att.ContainsKey("fromParent") && bool.Parse(att["fromParent"].ToString());
                                bool isDeleted = att.ContainsKey("isDeleted") && bool.Parse(att["isDeleted"].ToString());
                                bool isMarkedWithX = att.ContainsKey("isMarkedWithX") && bool.Parse(att["isMarkedWithX"].ToString());
                                return id == 0 && !fromParent && !isDeleted && !isMarkedWithX;
                            })
                            .ToList();
                    }

                    // Process new attachments
                    foreach (var att in newAttachmentsFromData)
                    {
                        string originalFileName = att["fileName"]?.ToString() ?? "";
                        bool isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString());
                        string caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null;

                        string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFileName);
                        string filePath = Path.Combine("categories", category.Id.ToString(), guidFileName).Replace("\\", "/");

                        var newAttachment = new Attachment
                        {
                            FileName = guidFileName,
                            FilePath = filePath,
                            Caption = caption,
                            IsInternal = isInternal,
                            CategoryId = category.Id
                        };
                        newAttachment.UpdateAudit(user.Id);
                        _unitOfWork.Attachment.Add(newAttachment);

                        savedAttachments.Add(new
                        {
                            id = 0,
                            fileName = guidFileName,
                            filePath = filePath,
                            originalFileName = originalFileName,
                            caption = caption,
                            isInternal = isInternal,
                            fromParent = false,
                            parentAttachmentId = 0,
                            isMarkedWithX = false
                        });

                        string destPath = Path.Combine(_attachmentSettings.UploadPath, filePath);
                        stagedAttachments.Add((newAttachment, null, destPath, originalFileName));
                        Console.WriteLine($"Staged new attachment: fileName={originalFileName}, path={filePath}");
                    }

                    // Process existing and parent-sourced attachments via ProcessAttachmentsAsync
                    var existingAttachments = new List<Attachment>(); // New category, so no existing attachments
                    var formAttachments = await ProcessAttachmentsAsync(category, files?.ToList(), AttachmentData, user.Id, existingAttachments, isCreateAction: true);
                    savedAttachments.AddRange(formAttachments);

                    foreach (var item in formAttachments)
                    {
                        var id = (int)item.GetType().GetProperty("id").GetValue(item);
                        var guidFileName = (string)item.GetType().GetProperty("fileName").GetValue(item);
                        var filePath = (string)item.GetType().GetProperty("filePath").GetValue(item);
                        var originalFileName = (string)item.GetType().GetProperty("originalFileName").GetValue(item);
                        var caption = (string)item.GetType().GetProperty("caption")?.GetValue(item);
                        var isInternal = (bool)item.GetType().GetProperty("isInternal").GetValue(item);
                        var fromParent = (bool)item.GetType().GetProperty("fromParent").GetValue(item);
                        var parentAttachmentId = (int)item.GetType().GetProperty("parentAttachmentId").GetValue(item);

                        // Skip parent-sourced attachments (already processed)
                        if (fromParent && parentAttachmentId > 0)
                        {
                            Console.WriteLine($"Skipping parent-sourced attachment in form loop: parentAttachmentId={parentAttachmentId}, fileName={originalFileName}");
                            continue;
                        }

                        var entity = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                        if (entity != null)
                        {
                            entity.Caption = caption;
                            entity.IsInternal = isInternal;
                            entity.UpdateAudit(user.Id);
                            _unitOfWork.Attachment.Update(entity);
                        }
                        else
                        {
                            Console.WriteLine($"Attachment ID {id} not found, skipping.");
                            continue;
                        }

                        string destPath = Path.Combine(_attachmentSettings.UploadPath, entity.FilePath);
                        string sourcePath = null;
                        var uploadedFile = files?.FirstOrDefault(f => f.FileName == originalFileName || f.FileName.EndsWith(Path.GetFileName(originalFileName)));
                        if (uploadedFile == null)
                        {
                            var categoryAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.FileName == originalFileName && a.CategoryId == category.Id && !a.IsDeleted);
                            sourcePath = categoryAttachment != null ? Path.Combine(_attachmentSettings.UploadPath, categoryAttachment.FilePath) : null;
                            Console.WriteLine($"No uploaded file for {originalFileName}, sourcePath: {sourcePath ?? "null"}");
                        }

                        stagedAttachments.Add((entity, sourcePath, destPath, originalFileName));
                    }
                    await _unitOfWork.SaveAsync();
                }

                // Save Files
                foreach (var (entity, sourcePath, destPath, originalFileName) in stagedAttachments)
                {
                    if (System.IO.File.Exists(destPath))
                    {
                        Console.WriteLine($"File already exists: {destPath}, skipping.");
                        continue;
                    }

                    string destDir = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                        Console.WriteLine($"Created destination directory: {destDir}");
                    }

                    try
                    {
                        if (sourcePath != null && System.IO.File.Exists(sourcePath))
                        {
                            System.IO.File.Copy(sourcePath, destPath, overwrite: false);
                            Console.WriteLine($"Copied file: {sourcePath} to {destPath}");
                        }
                        else
                        {
                            var file = files?.FirstOrDefault(f => f.FileName == originalFileName || f.FileName.EndsWith(Path.GetFileName(originalFileName)));
                            if (file != null)
                            {
                                using (var fileStream = new FileStream(destPath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    Console.WriteLine($"Uploaded file: {originalFileName} to {destPath}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No source or uploaded file for {originalFileName} (GUID: {entity.FileName})");
                                entity.SoftDelete(user.Id);
                                _unitOfWork.Attachment.Update(entity);
                                await _unitOfWork.SaveAsync();
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"File operation error for {originalFileName}: {ex.Message}");
                        throw new Exception($"Failed to save file {originalFileName}.", ex);
                    }
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                Console.WriteLine($"Committed transaction: Created category {category.Id}, {savedAttachments.Count} attachments, {savedReferences.Count} references");

                // Log final database state
                var finalAttachments = await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == category.Id && !a.IsDeleted);
                var finalReferences = await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == category.Id && !r.IsDeleted);
                Console.WriteLine($"Final Attachments: {JsonConvert.SerializeObject(finalAttachments.Select(a => new { a.Id, a.FileName }))}");
                Console.WriteLine($"Final References: {JsonConvert.SerializeObject(finalReferences.Select(r => new { r.Id, r.Url }))}");

                return Json(new { success = true, redirectUrl = "/Admin/Category/Index", attachments = savedAttachments });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Create POST: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in Edit GET.");
                return Unauthorized();
            }

            if (!id.HasValue || id == 0)
            {
                Console.WriteLine("Invalid category ID in Edit GET.");
                return NotFound();
            }

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id.Value && !c.IsDeleted,
                includeProperties: "Attachments,References,ParentCategory"
            );
            if (category == null)
            {
                Console.WriteLine($"Category ID {id} not found or deleted.");
                return NotFound();
            }

            // Initialize collections for view model
            var attachments = category.Attachments.Where(a => !a.IsDeleted).ToList();
            var references = category.References.Where(r => !r.IsDeleted).ToList();
            var parentAttachments = new List<Attachment>();
            var parentReferences = new List<Reference>();

            // Fetch parent attachments and references for display, without creating new records
            if (category.ParentCategoryId.HasValue)
            {
                var parentCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                    c => c.Id == category.ParentCategoryId.Value && !c.IsDeleted,
                    includeProperties: "Attachments,References"
                );
                if (parentCategory != null)
                {
                    parentAttachments = parentCategory.Attachments.Where(a => !a.IsDeleted).ToList();
                    parentReferences = parentCategory.References.Where(r => !r.IsDeleted).ToList();
                }
            }

            category.Categories = (await _unitOfWork.Category.GetAllAsync(
                c => !c.IsDeleted && c.ParentCategoryId == null && c.Id != id.Value
            )).ToList();

            // Combine own and parent attachments/references for the view, marking parent-sourced items
            ViewData["AttachmentLinks"] = attachments.Select(a => new
            {
                Id = a.Id,
                FileName = a.FileName,
                Url = Url.Action("OpenAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }),
                IsInternal = a.IsInternal,
                OriginalFileName = a.FileName,
                Caption = a.Caption ?? "", // Include Caption, default to empty string if null
                FromParent = false,
                ParentAttachmentId = 0
            }).Concat(parentAttachments.Select(pa => new
            {
                Id = 0,
                FileName = pa.FileName,
                Url = Url.Action("OpenAttachment", "Category", new { attachmentId = pa.Id, area = "Admin" }),
                IsInternal = pa.IsInternal,
                OriginalFileName = pa.FileName,
                Caption = pa.Caption ?? "", // Include Caption, default to empty string if null
                FromParent = true,
                ParentAttachmentId = pa.Id
            })).ToList();

            ViewData["ReferenceLinks"] = references.Select(r => new
            {
                Id = r.Id,
                Url = r.Url,
                Description = r.Description ?? "",
                IsInternal = r.IsInternal,
                OpenOption = r.OpenOption ?? "_self",
                FromParent = false,
                ParentReferenceId = 0
            }).Concat(parentReferences.Select(pr => new
            {
                Id = 0,
                Url = pr.Url,
                Description = pr.Description ?? "",
                IsInternal = pr.IsInternal,
                OpenOption = pr.OpenOption ?? "_self",
                FromParent = true,
                ParentReferenceId = pr.Id
            })).ToList();

            Console.WriteLine($"CategoryController.Edit: Fetched category ID {id} with {attachments.Count} own attachments, {parentAttachments.Count} parent attachments, {references.Count} own references, and {parentReferences.Count} parent references");
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category model, List<IFormFile> files, string ReferenceData, string AttachmentData, string submitAction, List<int> deletedAttachmentIds, List<int> deletedReferenceIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in Edit POST.");
                return Unauthorized();
            }

            ModelState.Remove("Categories");
            ModelState.Remove("Attachments");
            ModelState.Remove("References");
            ModelState.Remove("ParentCategory");
            ModelState.Remove("SubCategories");
            ModelState.Remove("UploadAttachments");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("UpdatedBy");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("IsDeleted");

            if (submitAction == "Cancel")
            {
                Console.WriteLine("Cancel button clicked, no server-side changes.");
                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }

            if (!ModelState.IsValid)
            {
                model.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.Id != model.Id)).ToList();
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine($"ModelState invalid: {string.Join(", ", errors)}");
                return Json(new { success = false, message = "Validation failed", errors });
            }

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == model.Id && !c.IsDeleted,
                includeProperties: "Attachments,References");
            if (category == null)
            {
                Console.WriteLine($"Category ID {model.Id} not found or deleted.");
                return NotFound();
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Update category fields
                category.Name = model.Name;
                category.Description = model.Description;
                category.ParentCategoryId = model.ParentCategoryId == 0 ? null : model.ParentCategoryId;
                category.DisplayOrder = model.DisplayOrder;
                category.HtmlContent = model.HtmlContent;
                category.UpdateAudit(user.Id);
                _unitOfWork.Category.Update(category);
                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Updated category: {category.Id}");

                // Ensure category directory exists
                string categoryPath = Path.Combine(_attachmentSettings.UploadPath, "categories", category.Id.ToString());
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    Console.WriteLine($"Created directory: {categoryPath}");
                }

                // Process HtmlContent media
                if (!string.IsNullOrEmpty(category.HtmlContent))
                {
                    category.HtmlContent = await ProcessHtmlContentMediaAsync(category, category.HtmlContent, user.Id, files);
                    _unitOfWork.Category.Update(category);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Processed HtmlContent media for category {category.Id}");
                }

                var savedReferences = new List<object>();
                var savedAttachments = new List<object>();
                List<(Attachment Entity, string SourcePath, string DestPath, string OriginalFileName)> stagedAttachments = new();

                // Fetch existing attachments and references
                var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == category.Id && !a.IsDeleted)).ToList();
                var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == category.Id && !r.IsDeleted)).ToList();

                // Process Form References
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    savedReferences.AddRange(await ProcessReferencesAsync(category, ReferenceData, user.Id));
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Saved {savedReferences.Count} references for category {category.Id}");
                }

                // Process Attachments from AttachmentData and Files
                if (!string.IsNullOrEmpty(AttachmentData) || files?.Any() == true)
                {
                    var attachmentDataItems = !string.IsNullOrEmpty(AttachmentData)
                        ? JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(AttachmentData)
                        : new List<Dictionary<string, object>>();

                    // Process new attachments (id == 0, !fromParent)
                    foreach (var att in attachmentDataItems.Where(a =>
                    {
                        int id = a.ContainsKey("id") ? Convert.ToInt32(a["id"]) : 0;
                        bool fromParent = a.ContainsKey("fromParent") && bool.Parse(a["fromParent"].ToString());
                        bool isDeleted = a.ContainsKey("isDeleted") && bool.Parse(a["isDeleted"].ToString());
                        bool isMarkedWithX = a.ContainsKey("isMarkedWithX") && bool.Parse(a["isMarkedWithX"].ToString());
                        return id == 0 && !fromParent && !isDeleted && !isMarkedWithX;
                    }))
                    {
                        string originalFileName = att["fileName"]?.ToString() ?? "";
                        bool isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString());
                        string caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null;

                        // Check if an attachment with the same FileName already exists
                        var existingAttachment = existingAttachments.FirstOrDefault(a => a.FileName == originalFileName);
                        if (existingAttachment != null)
                        {
                            // Update existing attachment
                            existingAttachment.Caption = caption;
                            existingAttachment.IsInternal = isInternal;
                            existingAttachment.UpdateAudit(user.Id);
                            _unitOfWork.Attachment.Update(existingAttachment);
                            savedAttachments.Add(new
                            {
                                id = existingAttachment.Id,
                                fileName = existingAttachment.FileName,
                                filePath = existingAttachment.FilePath,
                                originalFileName,
                                caption,
                                isInternal,
                                fromParent = false,
                                parentAttachmentId = 0,
                                isMarkedWithX = false
                            });
                            Console.WriteLine($"Updated existing attachment: id={existingAttachment.Id}, fileName={originalFileName}");
                            continue;
                        }

                        // Create new attachment
                        string newGuidFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFileName);
                        string newFilePath = Path.Combine("categories", category.Id.ToString(), newGuidFileName).Replace("\\", "/");

                        var newAttachment = new Attachment
                        {
                            FileName = newGuidFileName,
                            FilePath = newFilePath,
                            Caption = caption,
                            IsInternal = isInternal,
                            CategoryId = category.Id
                        };
                        newAttachment.UpdateAudit(user.Id);
                        _unitOfWork.Attachment.Add(newAttachment);
                        existingAttachments.Add(newAttachment);

                        savedAttachments.Add(new
                        {
                            id = 0,
                            fileName = newGuidFileName,
                            filePath = newFilePath,
                            originalFileName,
                            caption,
                            isInternal,
                            fromParent = false,
                            parentAttachmentId = 0,
                            isMarkedWithX = false
                        });

                        string newDestPath = Path.Combine(_attachmentSettings.UploadPath, newFilePath);
                        stagedAttachments.Add((newAttachment, null, newDestPath, originalFileName));
                        Console.WriteLine($"Staged new attachment: fileName={originalFileName}, path={newFilePath}");
                    }

                    // Process existing and parent-sourced attachments
                    savedAttachments.AddRange(await ProcessAttachmentsAsync(category, files, AttachmentData, user.Id, existingAttachments));

                    foreach (var item in savedAttachments)
                    {
                        var id = (int)item.GetType().GetProperty("id").GetValue(item);
                        var itemFileName = (string)item.GetType().GetProperty("fileName").GetValue(item);
                        var itemFilePath = (string)item.GetType().GetProperty("filePath").GetValue(item);
                        var originalFileName = (string)item.GetType().GetProperty("originalFileName").GetValue(item);
                        var caption = (string)item.GetType().GetProperty("caption")?.GetValue(item);
                        var isInternal = (bool)item.GetType().GetProperty("isInternal").GetValue(item);
                        var fromParent = (bool)item.GetType().GetProperty("fromParent").GetValue(item);
                        var parentAttachmentId = (int)item.GetType().GetProperty("parentAttachmentId").GetValue(item);

                        Attachment entity;
                        if (id > 0)
                        {
                            entity = existingAttachments.FirstOrDefault(a => a.Id == id);
                            if (entity != null)
                            {
                                entity.Caption = caption;
                                entity.IsInternal = isInternal;
                                entity.UpdateAudit(user.Id);
                                _unitOfWork.Attachment.Update(entity);
                                Console.WriteLine($"Updated attachment: id={id}, fileName={originalFileName}");
                            }
                            else
                            {
                                Console.WriteLine($"Attachment ID {id} not found, skipping.");
                                continue;
                            }
                        }
                        else if (fromParent && parentAttachmentId > 0)
                        {
                            // Handle parent-sourced attachments
                            var parentAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == parentAttachmentId && !a.IsDeleted);
                            if (parentAttachment == null)
                            {
                                Console.WriteLine($"Parent attachment ID {parentAttachmentId} not found, skipping.");
                                continue;
                            }

                            // Check if an attachment with the same FileName already exists
                            entity = existingAttachments.FirstOrDefault(a => a.FileName == parentAttachment.FileName);
                            if (entity != null)
                            {
                                // Update existing attachment
                                entity.Caption = caption;
                                entity.IsInternal = isInternal;
                                entity.UpdateAudit(user.Id);
                                _unitOfWork.Attachment.Update(entity);
                                Console.WriteLine($"Updated existing parent-sourced attachment: id={entity.Id}, fileName={parentAttachment.FileName}");
                            }
                            else
                            {
                                // Create new attachment for parent-sourced item
                                string parentGuidFileName = Guid.NewGuid().ToString() + Path.GetExtension(parentAttachment.FileName);
                                string parentFilePath = Path.Combine("categories", category.Id.ToString(), parentGuidFileName).Replace("\\", "/");

                                entity = new Attachment
                                {
                                    FileName = parentGuidFileName,
                                    FilePath = parentFilePath,
                                    Caption = caption,
                                    IsInternal = isInternal,
                                    CategoryId = category.Id
                                };
                                entity.UpdateAudit(user.Id);
                                _unitOfWork.Attachment.Add(entity);
                                existingAttachments.Add(entity);

                                string parentSourcePath = Path.Combine(_attachmentSettings.UploadPath, parentAttachment.FilePath);
                                string parentDestPath = Path.Combine(_attachmentSettings.UploadPath, parentFilePath);
                                stagedAttachments.Add((entity, parentSourcePath, parentDestPath, parentAttachment.FileName));
                                Console.WriteLine($"Staged new parent-sourced attachment: parentAttachmentId={parentAttachmentId}, fileName={parentAttachment.FileName}, newPath={parentFilePath}");
                            }
                        }
                        else
                        {
                            // Already handled new attachments above
                            continue;
                        }

                        string entityDestPath = Path.Combine(_attachmentSettings.UploadPath, entity.FilePath);
                        string entitySourcePath = null;
                        var uploadedFile = files?.FirstOrDefault(f => f.FileName == originalFileName || f.FileName.EndsWith(Path.GetFileName(originalFileName)));
                        if (uploadedFile == null && !fromParent)
                        {
                            var categoryAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.FileName == originalFileName && a.CategoryId == category.Id && !a.IsDeleted);
                            entitySourcePath = categoryAttachment != null ? Path.Combine(_attachmentSettings.UploadPath, categoryAttachment.FilePath) : null;
                            Console.WriteLine($"No uploaded file for {originalFileName}, sourcePath: {entitySourcePath ?? "null"}");
                        }

                        if (fromParent)
                        {
                            var parentAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == parentAttachmentId && !a.IsDeleted);
                            entitySourcePath = parentAttachment != null ? Path.Combine(_attachmentSettings.UploadPath, parentAttachment.FilePath) : null;
                        }

                        stagedAttachments.Add((entity, entitySourcePath, entityDestPath, originalFileName));
                    }
                    await _unitOfWork.SaveAsync();
                }

                // Soft-delete attachments
                if (deletedAttachmentIds?.Any() == true)
                {
                    foreach (var id in deletedAttachmentIds)
                    {
                        var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                        if (attachment != null)
                        {
                            attachment.SoftDelete(user.Id);
                            _unitOfWork.Attachment.Update(attachment);
                            Console.WriteLine($"Marked attachment {id} as deleted for category {category.Id}");
                        }
                    }
                    await _unitOfWork.SaveAsync();
                }

                // Soft-delete references
                if (deletedReferenceIds?.Any() == true)
                {
                    foreach (var id in deletedReferenceIds)
                    {
                        var reference = await _unitOfWork.Reference.GetFirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                        if (reference != null)
                        {
                            reference.SoftDelete(user.Id);
                            _unitOfWork.Reference.Update(reference);
                            Console.WriteLine($"Marked reference {id} as deleted for category {category.Id}");
                        }
                    }
                    await _unitOfWork.SaveAsync();
                }

                // Save Files
                foreach (var (entity, fileSourcePath, fileDestPath, originalFileName) in stagedAttachments)
                {
                    if (System.IO.File.Exists(fileDestPath))
                    {
                        Console.WriteLine($"File already exists: {fileDestPath}, skipping.");
                        continue;
                    }

                    string destDir = Path.GetDirectoryName(fileDestPath);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                        Console.WriteLine($"Created destination directory: {destDir}");
                    }

                    try
                    {
                        if (fileSourcePath != null && System.IO.File.Exists(fileSourcePath))
                        {
                            System.IO.File.Copy(fileSourcePath, fileDestPath, overwrite: false);
                            Console.WriteLine($"Copied file: {fileSourcePath} to {fileDestPath}");
                        }
                        else
                        {
                            var file = files?.FirstOrDefault(f => f.FileName == originalFileName || f.FileName.EndsWith(Path.GetFileName(originalFileName)));
                            if (file != null)
                            {
                                using (var fileStream = new FileStream(fileDestPath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    Console.WriteLine($"Uploaded file: {originalFileName} to {fileDestPath}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No source or uploaded file for {originalFileName} (GUID: {entity.FileName})");
                                entity.SoftDelete(user.Id);
                                _unitOfWork.Attachment.Update(entity);
                                await _unitOfWork.SaveAsync();
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"File operation error for {originalFileName}: {ex.Message}");
                        throw new Exception($"Failed to save file {originalFileName}.", ex);
                    }
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                Console.WriteLine($"Committed transaction: Updated category {category.Id}, {savedAttachments.Count} attachments, {savedReferences.Count} references");
                TempData["success"] = "Category updated successfully!";
                return Json(new { success = true, redirectUrl = Url.Action("Index"), attachments = savedAttachments, references = savedReferences });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Edit: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error updating category: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in Delete GET.");
                return Unauthorized();
            }

            if (!id.HasValue || id == 0)
            {
                Console.WriteLine("Invalid category ID in Delete GET.");
                return NotFound();
            }

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id.Value && !c.IsDeleted,
                includeProperties: "Attachments,References");
            if (category == null)
            {
                Console.WriteLine($"Category ID {id} not found or deleted.");
                return NotFound();
            }

            category.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.ParentCategoryId == null)).ToList();

            // Add ViewData["AttachmentLinks"] to provide download URLs
            ViewData["AttachmentLinks"] = category.Attachments.Where(a => !a.IsDeleted).Select(a => new
            {
                Id = a.Id,
                FileName = a.FileName,
                Url = Url.Action("OpenAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }), // Changed to OpenAttachment
                IsInternal = a.IsInternal,
                OriginalFileName = a.FileName
            }).ToList();

            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in Delete POST.");
                return Unauthorized();
            }

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted,
                includeProperties: "Attachments,References,SubCategories");
            if (category == null)
            {
                Console.WriteLine($"Category ID {id} not found or deleted.");
                return NotFound();
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (category.SubCategories?.Any(sc => !sc.IsDeleted) == true)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    Console.WriteLine($"Cannot delete category {id}: Active subcategories found.");
                    return Json(new { success = false, message = "Cannot delete category with active subcategories." });
                }

                var solutions = await _unitOfWork.Solution.GetAllAsync(s => s.CategoryId == id && !s.IsDeleted);
                if (solutions.Any())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    Console.WriteLine($"Cannot delete category {id}: Associated solutions found.");
                    return Json(new { success = false, message = "Cannot delete category with associated solutions." });
                }

                //category.IsDeleted = true;
                category.SoftDelete(user.Id);
                //_unitOfWork.Category.Update(category);

                foreach (var attachment in category.Attachments.Where(a => !a.IsDeleted))
                {
                    //attachment.IsDeleted = true;
                    attachment.SoftDelete(user.Id);
                    //_unitOfWork.Attachment.Update(attachment);
                }

                foreach (var reference in category.References.Where(r => !r.IsDeleted))
                {
                    //reference.IsDeleted = true;
                    reference.SoftDelete(user.Id);
                    //_unitOfWork.Reference.Update(reference);
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                Console.WriteLine($"Deleted category {id}");
                TempData["success"] = "Category deleted successfully!";
                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Delete: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error deleting category: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryData(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in GetCategoryData.");
                return Unauthorized();
            }

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted,
                includeProperties: "Attachments,References");
            if (category == null)
            {
                Console.WriteLine($"Category ID {id} not found or deleted in GetCategoryData.");
                return Json(new { success = false, message = "Category not found." });
            }

            var data = new
            {
                htmlContent = category.HtmlContent ?? "",
                attachments = category.Attachments.Where(a => !a.IsDeleted).Select(a => new
                {
                    id = a.Id,
                    fileName = a.FileName,
                    url = Url.Action("OpenAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }), // Changed to OpenAttachment
                    caption = a.Caption,
                    isInternal = a.IsInternal,
                    originalFileName = a.FileName,
                    fromParent = false, // Parent data for UI
                    parentAttachmentId = a.Id
                }).ToList(),
                references = category.References.Where(r => !r.IsDeleted).Select(r => new
                {
                    id = r.Id,
                    url = r.Url,
                    description = r.Description,
                    isInternal = r.IsInternal,
                    openOption = r.OpenOption,
                    fromParent = false, // Parent data for UI
                    parentReferenceId = r.Id
                }).ToList()
            };

            return Json(new { success = true, data });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in DownloadAttachment.");
                return Unauthorized();
            }

            var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
            if (attachment == null)
            {
                Console.WriteLine($"Attachment ID {attachmentId} not found or deleted.");
                return NotFound();
            }

            var filePath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath);
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"Attachment file not found: {filePath}");
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = "application/octet-stream";
            return File(memory, contentType, attachment.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAttachment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in RemoveAttachment.");
                return Unauthorized();
            }

            var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (attachment == null)
            {
                Console.WriteLine($"Attachment ID {id} not found or deleted.");
                return Json(new { success = false, message = "Attachment not found." });
            }

            // Ensure the attachment belongs to a category being edited (optional, depending on requirements)
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == attachment.CategoryId && !c.IsDeleted);
            if (category == null)
            {
                Console.WriteLine($"Category for attachment ID {id} not found or deleted.");
                return Json(new { success = false, message = "Invalid category for attachment." });
            }

            //attachment.IsDeleted = true;
            attachment.SoftDelete(user.Id);
            //_unitOfWork.Attachment.Update(attachment);
            await _unitOfWork.SaveAsync();
            Console.WriteLine($"Marked attachment {id} as deleted.");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveReference(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in RemoveReference.");
                return Unauthorized();
            }

            var reference = await _unitOfWork.Reference.GetFirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (reference == null)
            {
                Console.WriteLine($"Reference ID {id} not found or deleted.");
                return Json(new { success = false, message = "Reference not found." });
            }

            // Ensure the reference belongs to a category being edited (optional, depending on requirements)
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == reference.CategoryId && !c.IsDeleted);
            if (category == null)
            {
                Console.WriteLine($"Category for reference ID {id} not found or deleted.");
                return Json(new { success = false, message = "Invalid category for reference." });
            }

            //reference.IsDeleted = true;
            reference.SoftDelete(user.Id);
            //_unitOfWork.Reference.Update(reference);
            await _unitOfWork.SaveAsync();
            Console.WriteLine($"Marked reference {id} as deleted.");
            return Json(new { success = true });
        }

        private async Task<string> ProcessHtmlContentMediaAsync(Category category, string htmlContent, string userId, List<IFormFile> files)
        {
            if (string.IsNullOrEmpty(htmlContent) || files == null || !files.Any())
            {
                return htmlContent;
            }

            var regex = new Regex("<img[^>]+src=[\"'](data:image/[^\"']+)[\"'][^>]*>");
            var matches = regex.Matches(htmlContent);
            foreach (Match match in matches)
            {
                var base64String = match.Groups[1].Value;
                if (!base64String.StartsWith("data:image/"))
                {
                    continue;
                }

                try
                {
                    var parts = base64String.Split(',');
                    if (parts.Length < 2)
                    {
                        Console.WriteLine($"Invalid base64 image data: {base64String.Substring(0, Math.Min(50, base64String.Length))}...");
                        continue;
                    }

                    var mimeType = parts[0].Split(';')[0].Replace("data:", "");
                    var extension = mimeType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        _ => null
                    };

                    if (extension == null)
                    {
                        Console.WriteLine($"Unsupported image MIME type: {mimeType}");
                        continue;
                    }

                    var bytes = Convert.FromBase64String(parts[1]);
                    var guidFileName = Guid.NewGuid().ToString() + extension;
                    var relativeFilePath = Path.Combine("categories", category.Id.ToString(), guidFileName);
                    var absoluteFilePath = Path.Combine(_attachmentSettings.UploadPath, relativeFilePath);

                    var destDir = Path.GetDirectoryName(absoluteFilePath);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                        Console.WriteLine($"Created directory for image: {destDir}");
                    }

                    await System.IO.File.WriteAllBytesAsync(absoluteFilePath, bytes);
                    Console.WriteLine($"Saved base64 image: {absoluteFilePath}");

                    var attachment = new Attachment
                    {
                        FileName = guidFileName,
                        FilePath = relativeFilePath,
                        CategoryId = category.Id,
                        IsInternal = false
                    };
                    attachment.UpdateAudit(userId);
                    _unitOfWork.Attachment.Add(attachment);
                    await _unitOfWork.SaveAsync();

                    var newSrc = Url.Action("DownloadAttachment", "Category", new { attachmentId = attachment.Id, area = "Admin" });
                    htmlContent = htmlContent.Replace(base64String, newSrc);
                    Console.WriteLine($"Replaced base64 image with URL: {newSrc}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing base64 image: {ex.Message}");
                    continue;
                }
            }

            return htmlContent;
        }

        private async Task<List<object>> ProcessReferencesAsync(Category category, string referenceData, string userId, bool isCreateAction = false)
        {
            var references = new List<object>();
            if (string.IsNullOrEmpty(referenceData))
            {
                return references;
            }

            try
            {
                var stagedReferences = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(referenceData);
                Console.WriteLine($"Processing ReferenceData: {JsonConvert.SerializeObject(stagedReferences)}");
                var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == category.Id && !r.IsDeleted)).ToList();

                foreach (var refItem in stagedReferences)
                {
                    int id = refItem.ContainsKey("id") ? Convert.ToInt32(refItem["id"]) : 0;
                    string url = refItem["url"]?.ToString() ?? "";
                    string description = refItem["description"]?.ToString();
                    bool isInternal = refItem.ContainsKey("isInternal") && bool.Parse(refItem["isInternal"].ToString());
                    string openOption = refItem["openOption"]?.ToString() ?? "";
                    bool isDeleted = refItem.ContainsKey("isDeleted") && bool.Parse(refItem["isDeleted"].ToString());
                    bool isMarkedWithX = refItem.ContainsKey("isMarkedWithX") && bool.Parse(refItem["isMarkedWithX"].ToString());
                    bool fromParent = refItem.ContainsKey("fromParent") && bool.Parse(refItem["fromParent"].ToString());
                    int parentReferenceId = refItem.ContainsKey("parentReferenceId") ? Convert.ToInt32(refItem["parentReferenceId"]) : 0;

                    // Skip deleted or marked references
                    if (isDeleted || isMarkedWithX)
                    {
                        Console.WriteLine($"Skipping reference: url={url}, parentReferenceId={parentReferenceId}, isDeleted={isDeleted}, isMarkedWithX={isMarkedWithX}");
                        continue;
                    }

                    Reference reference;
                    if (id > 0)
                    {
                        // Update existing reference
                        reference = existingReferences.FirstOrDefault(r => r.Id == id);
                        if (reference != null)
                        {
                            reference.Url = url;
                            reference.Description = description;
                            reference.IsInternal = isInternal;
                            reference.OpenOption = openOption;
                            reference.UpdateAudit(userId);
                            _unitOfWork.Reference.Update(reference);
                            Console.WriteLine($"Updated reference: id={reference.Id}, url={url}");
                        }
                        else
                        {
                            Console.WriteLine($"Reference ID {id} not found, skipping.");
                            continue;
                        }
                    }
                    else if (fromParent && parentReferenceId > 0)
                    {
                        // Handle parent-sourced reference
                        var parentReference = await _unitOfWork.Reference.GetFirstOrDefaultAsync(r => r.Id == parentReferenceId && !r.IsDeleted);
                        if (parentReference == null)
                        {
                            Console.WriteLine($"Parent reference ID {parentReferenceId} not found, skipping.");
                            continue;
                        }

                        // Check if a reference with the same Url already exists
                        reference = existingReferences.FirstOrDefault(r => r.Url == parentReference.Url);
                        if (reference != null)
                        {
                            // Update existing reference
                            reference.Description = description;
                            reference.IsInternal = isInternal;
                            reference.OpenOption = openOption;
                            reference.UpdateAudit(userId);
                            _unitOfWork.Reference.Update(reference);
                            Console.WriteLine($"Updated existing parent-sourced reference: id={reference.Id}, url={url}");
                        }
                        else
                        {
                            // Create new reference for parent-sourced item
                            reference = new Reference
                            {
                                Url = url,
                                Description = description,
                                IsInternal = isInternal,
                                OpenOption = openOption,
                                CategoryId = category.Id
                            };
                            reference.UpdateAudit(userId);
                            _unitOfWork.Reference.Add(reference);
                            existingReferences.Add(reference);
                            Console.WriteLine($"Added new parent-sourced reference: url={url}, parentReferenceId={parentReferenceId}");
                        }
                    }
                    else
                    {
                        // Create new reference
                        reference = new Reference
                        {
                            Url = url,
                            Description = description,
                            IsInternal = isInternal,
                            OpenOption = openOption,
                            CategoryId = category.Id
                        };
                        reference.UpdateAudit(userId);
                        _unitOfWork.Reference.Add(reference);
                        existingReferences.Add(reference);
                        Console.WriteLine($"Added new reference: url={url}");
                    }

                    references.Add(new
                    {
                        id = reference.Id,
                        url,
                        description,
                        isInternal,
                        openOption,
                        fromParent,
                        parentReferenceId,
                        isMarkedWithX = false
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing references: {ex.Message}");
                throw new Exception("Failed to process references.", ex);
            }

            return references;
        }

        private async Task<List<object>> ProcessAttachmentsAsync(Category category, List<IFormFile> files, string attachmentData, string userId, List<Attachment> existingAttachments, bool isCreateAction = false)
        {
            var attachments = new List<object>();
            if (string.IsNullOrEmpty(attachmentData))
            {
                return attachments;
            }

            try
            {
                var stagedAttachments = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(attachmentData);
                Console.WriteLine($"Processing AttachmentData: {JsonConvert.SerializeObject(stagedAttachments)}");

                foreach (var att in stagedAttachments)
                {
                    int id = att.ContainsKey("id") ? Convert.ToInt32(att["id"]) : 0;
                    string originalFileName = att["fileName"]?.ToString() ?? "";
                    bool isDeleted = att.ContainsKey("isDeleted") && bool.Parse(att["isDeleted"].ToString());
                    bool isMarkedWithX = att.ContainsKey("isMarkedWithX") && bool.Parse(att["isMarkedWithX"].ToString());
                    bool fromParent = att.ContainsKey("fromParent") && bool.Parse(att["fromParent"].ToString());
                    int parentAttachmentId = att.ContainsKey("parentAttachmentId") ? Convert.ToInt32(att["parentAttachmentId"]) : 0;
                    bool isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString());
                    string caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null;

                    // Skip deleted or marked attachments
                    if (isDeleted || isMarkedWithX)
                    {
                        Console.WriteLine($"Skipping attachment: fileName={originalFileName}, parentAttachmentId={parentAttachmentId}, isDeleted={isDeleted}, isMarkedWithX={isMarkedWithX}");
                        continue;
                    }

                    // Skip new attachments (id == 0 and !fromParent) - these are handled in the Edit action
                    if (id == 0 && !fromParent)
                    {
                        Console.WriteLine($"Skipping new attachment (handled by Edit action): fileName={originalFileName}");
                        continue;
                    }

                    Attachment attachment;
                    if (id > 0)
                    {
                        // Update existing attachment
                        attachment = existingAttachments.FirstOrDefault(a => a.Id == id);
                        if (attachment != null)
                        {
                            attachment.Caption = caption;
                            attachment.IsInternal = isInternal;
                            attachment.UpdateAudit(userId);
                            _unitOfWork.Attachment.Update(attachment);
                            Console.WriteLine($"Updated existing attachment: id={attachment.Id}, fileName={originalFileName}");
                        }
                        else
                        {
                            Console.WriteLine($"Attachment ID {id} not found, skipping.");
                            continue;
                        }
                    }
                    else if (fromParent && parentAttachmentId > 0)
                    {
                        // Handle parent-sourced attachment
                        var parentAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == parentAttachmentId && !a.IsDeleted);
                        if (parentAttachment == null)
                        {
                            Console.WriteLine($"Parent attachment ID {parentAttachmentId} not found, skipping.");
                            continue;
                        }

                        // Check if an attachment with the same FileName already exists
                        attachment = existingAttachments.FirstOrDefault(a => a.FileName == parentAttachment.FileName);
                        if (attachment != null)
                        {
                            // Update existing attachment
                            attachment.Caption = caption;
                            attachment.IsInternal = isInternal;
                            attachment.UpdateAudit(userId);
                            _unitOfWork.Attachment.Update(attachment);
                            Console.WriteLine($"Updated existing parent-sourced attachment: id={attachment.Id}, fileName={parentAttachment.FileName}");
                        }
                        else
                        {
                            // Create new attachment for parent-sourced item
                            string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(parentAttachment.FileName);
                            string filePath = Path.Combine("categories", category.Id.ToString(), guidFileName).Replace("\\", "/");

                            attachment = new Attachment
                            {
                                FileName = guidFileName,
                                FilePath = filePath,
                                Caption = caption,
                                IsInternal = isInternal,
                                CategoryId = category.Id
                            };
                            attachment.UpdateAudit(userId);
                            _unitOfWork.Attachment.Add(attachment);
                            existingAttachments.Add(attachment);
                            Console.WriteLine($"Added new parent-sourced attachment: fileName={parentAttachment.FileName}, parentAttachmentId={parentAttachmentId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipping invalid attachment: id={id}, fileName={originalFileName}, fromParent={fromParent}");
                        continue;
                    }

                    attachments.Add(new
                    {
                        id = attachment.Id,
                        fileName = attachment.FileName,
                        filePath = attachment.FilePath,
                        originalFileName,
                        caption,
                        isInternal,
                        fromParent,
                        parentAttachmentId,
                        isMarkedWithX = false
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing attachments: {ex.Message}");
                throw new Exception("Failed to process attachments.", ex);
            }

            return attachments;
        }

        [HttpGet]
        public async Task<IActionResult> OpenAttachment(int attachmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found in OpenAttachment.");
                return Unauthorized();
            }

            var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
            if (attachment == null)
            {
                Console.WriteLine($"Attachment ID {attachmentId} not found or deleted.");
                return NotFound();
            }

            var filePath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath);
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"Attachment file not found: {filePath}");
                return NotFound($"Attachment file not found: {attachment.FilePath}");
            }

            string contentType = GetContentType(attachment.FileName);
            Console.WriteLine($"Serving attachment ID {attachmentId}, FileName: {attachment.FileName}, FilePath: {filePath}, Content-Type: {contentType}");

            try
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                Response.Headers["Content-Disposition"] = "inline";
                Response.Headers["Content-Type"] = contentType;
                Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                Console.WriteLine($"OpenAttachment Response Headers - Content-Disposition: {Response.Headers["Content-Disposition"]}, Content-Type: {Response.Headers["Content-Type"]}, FileName: {attachment.FileName}");
                return File(memory, contentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serving attachment ID {attachmentId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error serving attachment: {ex.Message}");
            }
        }

        // Helper method to determine content type based on file extension
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream" // Fallback for unknown types
            };
            Console.WriteLine($"GetContentType - FileName: {fileName}, Extension: {extension}, Content-Type: {contentType}");
            return contentType;
        }

        private async Task ProcessParentCategoryAsync(Category category, string userId, List<int> deletedAttachmentIds, List<int> deletedReferenceIds, string AttachmentData, string ReferenceData, List<object> savedAttachments, List<object> savedReferences, List<(Attachment Entity, string SourcePath, string DestPath, string OriginalFileName)> stagedAttachments)
        {
            if (!category.ParentCategoryId.HasValue) return;

            var parentCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == category.ParentCategoryId.Value && !c.IsDeleted,
                includeProperties: "Attachments,References");

            if (parentCategory == null)
            {
                Console.WriteLine($"Parent category not found: ParentCategoryId={category.ParentCategoryId}");
                return;
            }

            var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == category.Id && !a.IsDeleted)).ToList();
            var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == category.Id && !r.IsDeleted)).ToList();

            // Parse AttachmentData and ReferenceData
            var attachmentDataItems = !string.IsNullOrEmpty(AttachmentData)
                ? JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(AttachmentData)
                : new List<Dictionary<string, object>>();
            var referenceDataItems = !string.IsNullOrEmpty(ReferenceData)
                ? JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(ReferenceData)
                : new List<Dictionary<string, object>>();

            // Process parent attachments
            var parentAttachments = attachmentDataItems
                .Where(att =>
                {
                    bool fromParent = att.ContainsKey("fromParent") && bool.Parse(att["fromParent"].ToString());
                    int parentAttachmentId = att.ContainsKey("parentAttachmentId") ? Convert.ToInt32(att["parentAttachmentId"]) : 0;
                    bool isDeleted = att.ContainsKey("isDeleted") && bool.Parse(att["isDeleted"].ToString());
                    bool isMarkedWithX = att.ContainsKey("isMarkedWithX") && bool.Parse(att["isMarkedWithX"].ToString());
                    return fromParent && parentAttachmentId > 0 && !isDeleted && !isMarkedWithX;
                })
                .ToList();

            foreach (var att in parentAttachments)
            {
                int parentAttachmentId = Convert.ToInt32(att["parentAttachmentId"]);
                string originalFileName = att["fileName"]?.ToString() ?? "";
                bool isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString());
                string caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null;

                var parentAttachment = parentCategory.Attachments.FirstOrDefault(a => a.Id == parentAttachmentId && !a.IsDeleted);
                if (parentAttachment == null)
                {
                    Console.WriteLine($"Parent attachment ID {parentAttachmentId} not found, skipping.");
                    continue;
                }

                // Check if an attachment with the same FileName already exists
                var existingAttachment = existingAttachments.FirstOrDefault(a => a.FileName == parentAttachment.FileName);
                if (existingAttachment != null)
                {
                    // Update existing attachment
                    existingAttachment.Caption = caption;
                    existingAttachment.IsInternal = isInternal;
                    existingAttachment.UpdateAudit(userId);
                    _unitOfWork.Attachment.Update(existingAttachment);
                    savedAttachments.Add(new
                    {
                        id = existingAttachment.Id,
                        fileName = existingAttachment.FileName,
                        filePath = existingAttachment.FilePath,
                        originalFileName = parentAttachment.FileName,
                        caption,
                        isInternal,
                        fromParent = true,
                        parentAttachmentId,
                        isMarkedWithX = false
                    });
                    Console.WriteLine($"Updated existing parent-sourced attachment: id={existingAttachment.Id}, fileName={parentAttachment.FileName}");
                    continue;
                }

                // Create new attachment
                string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(parentAttachment.FileName);
                string relativeFilePath = Path.Combine("categories", category.Id.ToString(), guidFileName);
                string destPath = Path.Combine(_attachmentSettings.UploadPath, relativeFilePath);
                string sourcePath = Path.Combine(_attachmentSettings.UploadPath, parentAttachment.FilePath);

                var newAttachment = new Attachment
                {
                    FileName = guidFileName,
                    FilePath = relativeFilePath,
                    Caption = caption,
                    IsInternal = isInternal,
                    CategoryId = category.Id
                };
                newAttachment.UpdateAudit(userId);
                _unitOfWork.Attachment.Add(newAttachment);
                existingAttachments.Add(newAttachment);

                savedAttachments.Add(new
                {
                    id = 0,
                    fileName = guidFileName,
                    filePath = relativeFilePath,
                    originalFileName = parentAttachment.FileName,
                    caption,
                    isInternal,
                    fromParent = true,
                    parentAttachmentId,
                    isMarkedWithX = false
                });

                stagedAttachments.Add((newAttachment, sourcePath, destPath, parentAttachment.FileName));
                Console.WriteLine($"Staged new parent-sourced attachment: {parentAttachment.FileName} -> {relativeFilePath}");
            }

            // Process parent references
            var parentReferences = referenceDataItems
                .Where(refItem =>
                {
                    bool fromParent = refItem.ContainsKey("fromParent") && bool.Parse(refItem["fromParent"].ToString());
                    int parentReferenceId = refItem.ContainsKey("parentReferenceId") ? Convert.ToInt32(refItem["parentReferenceId"]) : 0;
                    bool isDeleted = refItem.ContainsKey("isDeleted") && bool.Parse(refItem["isDeleted"].ToString());
                    bool isMarkedWithX = refItem.ContainsKey("isMarkedWithX") && bool.Parse(refItem["isMarkedWithX"].ToString());
                    return fromParent && parentReferenceId > 0 && !isDeleted && !isMarkedWithX;
                })
                .ToList();

            foreach (var refItem in parentReferences)
            {
                int parentReferenceId = Convert.ToInt32(refItem["parentReferenceId"]);
                string url = refItem["url"]?.ToString() ?? "";
                string description = refItem["description"]?.ToString();
                bool isInternal = refItem.ContainsKey("isInternal") && bool.Parse(refItem["isInternal"].ToString());
                string openOption = refItem["openOption"]?.ToString() ?? "";

                var parentReference = parentCategory.References.FirstOrDefault(r => r.Id == parentReferenceId && !r.IsDeleted);
                if (parentReference == null)
                {
                    Console.WriteLine($"Parent reference ID {parentReferenceId} not found, skipping.");
                    continue;
                }

                // Check if a reference with the same Url already exists
                var existingReference = existingReferences.FirstOrDefault(r => r.Url == parentReference.Url);
                if (existingReference != null)
                {
                    // Update existing reference
                    existingReference.Description = description;
                    existingReference.IsInternal = isInternal;
                    existingReference.OpenOption = openOption;
                    existingReference.UpdateAudit(userId);
                    _unitOfWork.Reference.Update(existingReference);
                    savedReferences.Add(new
                    {
                        id = existingReference.Id,
                        url,
                        description,
                        isInternal,
                        openOption,
                        fromParent = true,
                        parentReferenceId,
                        isMarkedWithX = false
                    });
                    Console.WriteLine($"Updated existing parent-sourced reference: id={existingReference.Id}, url={url}");
                    continue;
                }

                // Create new reference
                var newReference = new Reference
                {
                    Url = url,
                    Description = description,
                    IsInternal = isInternal,
                    OpenOption = openOption,
                    CategoryId = category.Id
                };
                newReference.UpdateAudit(userId);
                _unitOfWork.Reference.Add(newReference);
                existingReferences.Add(newReference);

                savedReferences.Add(new
                {
                    id = 0,
                    url,
                    description,
                    isInternal,
                    openOption,
                    fromParent = true,
                    parentReferenceId,
                    isMarkedWithX = false
                });
                Console.WriteLine($"Added new parent-sourced reference: url={url}");
            }

            await _unitOfWork.SaveAsync();
        }
    }
}