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

namespace SupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ExternalUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AttachmentSettings _attachmentSettings;

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
            Console.WriteLine($"Form submission: Name={category.Name}, ParentCategoryId={category.ParentCategoryId}, FilesCount={(files?.Length ?? 0)}, AttachmentData={AttachmentData}, ReferenceData={ReferenceData}, DeletedAttachmentIds={string.Join(",", deletedAttachmentIds ?? new List<int>())}");
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
                if (category.ParentCategoryId.HasValue)
                {
                    var parentCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                        c => c.Id == category.ParentCategoryId.Value && !c.IsDeleted,
                        includeProperties: "Attachments,References");

                    if (parentCategory != null)
                    {
                        // Copy Parent Attachments
                        var parentAttachments = parentCategory.Attachments
                            .Where(a => !a.IsDeleted && !(deletedAttachmentIds?.Contains(a.Id) ?? false))
                            .ToList();

                        foreach (var parentAttachment in parentAttachments)
                        {
                            string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(parentAttachment.FileName);
                            string relativeFilePath = Path.Combine("categories", category.Id.ToString(), guidFileName);
                            string destPath = Path.Combine(_attachmentSettings.UploadPath, relativeFilePath);
                            string sourcePath = Path.Combine(_attachmentSettings.UploadPath, parentAttachment.FilePath);

                            var newAttachment = new Attachment
                            {
                                FileName = parentAttachment.FileName,
                                FilePath = relativeFilePath,
                                Caption = parentAttachment.Caption,
                                IsInternal = parentAttachment.IsInternal,
                                CategoryId = category.Id
                            };
                            newAttachment.UpdateAudit(user.Id);
                            _unitOfWork.Attachment.Add(newAttachment);

                            savedAttachments.Add(new
                            {
                                id = 0,
                                fileName = guidFileName,
                                filePath = relativeFilePath,
                                originalFileName = parentAttachment.FileName,
                                caption = parentAttachment.Caption,
                                isInternal = parentAttachment.IsInternal
                            });

                            stagedAttachments.Add((newAttachment, sourcePath, destPath, parentAttachment.FileName));
                            Console.WriteLine($"Staged parent attachment: {parentAttachment.FileName} -> {relativeFilePath}");
                        }

                        // Copy Parent References
                        var parentReferences = parentCategory.References
                            .Where(r => !r.IsDeleted && !(deletedReferenceIds?.Contains(r.Id) ?? false))
                            .ToList();

                        foreach (var parentReference in parentReferences)
                        {
                            var newReference = new Reference
                            {
                                Url = parentReference.Url,
                                Description = parentReference.Description,
                                IsInternal = parentReference.IsInternal,
                                OpenOption = parentReference.OpenOption,
                                CategoryId = category.Id
                            };
                            newReference.UpdateAudit(user.Id);
                            _unitOfWork.Reference.Add(newReference);

                            savedReferences.Add(new
                            {
                                id = 0,
                                url = parentReference.Url,
                                description = parentReference.Description,
                                isInternal = parentReference.IsInternal,
                                openOption = parentReference.OpenOption
                            });
                            Console.WriteLine($"Added parent reference: {parentReference.Url}");
                        }

                        await _unitOfWork.SaveAsync();
                    }
                }

                // Process Form References
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var formReferences = await ProcessReferencesAsync(category, ReferenceData, user.Id);
                    savedReferences.AddRange(formReferences);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Saved {formReferences.Count} form references for category {category.Id}");
                }

                // Process Form Attachments
                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Any() ?? false))
                {
                    var formAttachments = await ProcessAttachmentsAsync(category, files?.ToList(), AttachmentData, user.Id);
                    var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == category.Id && !a.IsDeleted)).ToList();

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

                        // Skip parent attachments already processed
                        if (fromParent && parentAttachmentId > 0)
                        {
                            continue;
                        }

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
                            }
                            else
                            {
                                Console.WriteLine($"Attachment ID {id} not found, skipping.");
                                continue;
                            }
                        }
                        else
                        {
                            entity = new Attachment
                            {
                                FileName = guidFileName,
                                FilePath = filePath,
                                Caption = caption,
                                IsInternal = isInternal,
                                CategoryId = category.Id
                            };
                            entity.UpdateAudit(user.Id);
                            _unitOfWork.Attachment.Add(entity);
                            existingAttachments.Add(entity);
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
                                Console.WriteLine($"Error: No source or uploaded file for {originalFileName} (GUID: {entity.FileName})");
                                entity.IsDeleted = true;
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

                return Json(new { success = true, redirectUrl = "/Admin/Category/Index" });
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

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id.Value && !c.IsDeleted,
                includeProperties: "Attachments,References");
            if (category == null)
            {
                Console.WriteLine($"Category ID {id} not found or deleted.");
                return NotFound();
            }

            category.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted && c.ParentCategoryId == null && c.Id != id.Value)).ToList();

            ViewData["AttachmentLinks"] = category.Attachments.Where(a => !a.IsDeleted).Select(a => new
            {
                Id = a.Id,
                FileName = a.FileName,
                Url = Url.Action("DownloadAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }),
                IsInternal = a.IsInternal,
                OriginalFileName = a.FileName
            }).ToList();

            Console.WriteLine($"CategoryController.Edit: Fetched category ID {id} with {category.References.Count} references");
            return View(category);
        }

        [HttpPost]
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
                model.Attachments = model.Attachments ?? new List<Attachment>();
                model.References = model.References ?? new List<Reference>();
                ViewData["AttachmentLinks"] = model.Attachments.Where(a => !a.IsDeleted).Select(a => new
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    Url = Url.Action("DownloadAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }),
                    IsInternal = a.IsInternal,
                    OriginalFileName = a.FileName
                }).ToList();
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
                category.Name = model.Name;
                category.Description = model.Description;
                category.ParentCategoryId = model.ParentCategoryId == 0 ? null : model.ParentCategoryId;
                category.DisplayOrder = model.DisplayOrder;
                category.HtmlContent = model.HtmlContent;
                category.UpdateAudit(user.Id);
                _unitOfWork.Category.Update(category);
                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Updated category: {category.Id}");

                string categoryPath = Path.Combine(_attachmentSettings.UploadPath, "categories", category.Id.ToString());
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    Console.WriteLine($"Created directory: {categoryPath}");
                }

                if (!string.IsNullOrEmpty(category.HtmlContent))
                {
                    category.HtmlContent = await ProcessHtmlContentMediaAsync(category, category.HtmlContent, user.Id, files);
                    _unitOfWork.Category.Update(category);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Processed HtmlContent media for category {category.Id}");
                }

                var savedReferences = new List<object>();
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    savedReferences = await ProcessReferencesAsync(category, ReferenceData, user.Id);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Saved {savedReferences.Count} references for category {category.Id}");
                }

                var savedAttachments = new List<object>();
                List<(Attachment Entity, string SourcePath, string DestPath, string OriginalFileName)> stagedAttachments = new();
                if (!string.IsNullOrEmpty(AttachmentData) || files?.Any() == true)
                {
                    savedAttachments = await ProcessAttachmentsAsync(category, files, AttachmentData, user.Id);
                    var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == category.Id && !a.IsDeleted)).ToList();

                    foreach (var item in savedAttachments)
                    {
                        var id = (int)item.GetType().GetProperty("id").GetValue(item);
                        var guidFileName = (string)item.GetType().GetProperty("fileName").GetValue(item);
                        var filePath = (string)item.GetType().GetProperty("filePath").GetValue(item);
                        var originalFileName = (string)item.GetType().GetProperty("originalFileName").GetValue(item);
                        var caption = (string)item.GetType().GetProperty("caption")?.GetValue(item);
                        var isInternal = (bool)item.GetType().GetProperty("isInternal").GetValue(item);

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
                            }
                            else
                            {
                                Console.WriteLine($"Attachment ID {id} not found, skipping.");
                                continue;
                            }
                        }
                        else
                        {
                            entity = new Attachment
                            {
                                FileName = guidFileName,
                                FilePath = filePath,
                                Caption = caption,
                                IsInternal = isInternal,
                                CategoryId = category.Id
                            };
                            entity.UpdateAudit(user.Id);
                            _unitOfWork.Attachment.Add(entity);
                            existingAttachments.Add(entity);
                        }

                        string destPath = Path.Combine(_attachmentSettings.UploadPath, entity.FilePath);
                        string sourcePath = null;
                        var uploadedFile = files?.FirstOrDefault(f => f.FileName == originalFileName || f.FileName.EndsWith(Path.GetFileName(originalFileName)));
                        if (uploadedFile == null)
                        {
                            var categoryAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.FileName == originalFileName && a.CategoryId == category.Id && !a.IsDeleted);
                            sourcePath = categoryAttachment != null ? Path.Combine(_attachmentSettings.UploadPath, categoryAttachment.FilePath) : null;
                            Console.WriteLine($"No uploaded file for {originalFileName}, sourcePath: {sourcePath}");
                        }

                        stagedAttachments.Add((entity, sourcePath, destPath, originalFileName));
                    }
                    await _unitOfWork.SaveAsync();
                }

                if (deletedAttachmentIds?.Any() == true)
                {
                    foreach (var id in deletedAttachmentIds)
                    {
                        var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                        if (attachment != null)
                        {
                            attachment.IsDeleted = true;
                            attachment.UpdateAudit(user.Id);
                            _unitOfWork.Attachment.Update(attachment);
                            Console.WriteLine($"Marked attachment {id} as deleted for category {category.Id}");
                        }
                    }
                    await _unitOfWork.SaveAsync();
                }

                if (deletedReferenceIds?.Any() == true)
                {
                    foreach (var id in deletedReferenceIds)
                    {
                        var reference = await _unitOfWork.Reference.GetFirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                        if (reference != null)
                        {
                            reference.IsDeleted = true;
                            reference.UpdateAudit(user.Id);
                            _unitOfWork.Reference.Update(reference);
                            Console.WriteLine($"Marked reference {id} as deleted for category {category.Id}");
                        }
                    }
                    await _unitOfWork.SaveAsync();
                }

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
                                Console.WriteLine($"Warning: No source file for {originalFileName} and not uploaded.");
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

                category.IsDeleted = true;
                category.UpdateAudit(user.Id);
                _unitOfWork.Category.Update(category);

                foreach (var attachment in category.Attachments.Where(a => !a.IsDeleted))
                {
                    attachment.IsDeleted = true;
                    attachment.UpdateAudit(user.Id);
                    _unitOfWork.Attachment.Update(attachment);
                }

                foreach (var reference in category.References.Where(r => !r.IsDeleted))
                {
                    reference.IsDeleted = true;
                    reference.UpdateAudit(user.Id);
                    _unitOfWork.Reference.Update(reference);
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
                    url = Url.Action("DownloadAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }),
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

            attachment.IsDeleted = true;
            attachment.UpdateAudit(user.Id);
            _unitOfWork.Attachment.Update(attachment);
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

            reference.IsDeleted = true;
            reference.UpdateAudit(user.Id);
            _unitOfWork.Reference.Update(reference);
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

        private async Task<List<object>> ProcessReferencesAsync(Category category, string referenceData, string userId)
        {
            var references = new List<object>();
            if (string.IsNullOrEmpty(referenceData))
            {
                return references;
            }

            try
            {
                var stagedReferences = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(referenceData);
                var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == category.Id && !r.IsDeleted)).ToList();

                foreach (var refItem in stagedReferences)
                {
                    int id = refItem.ContainsKey("id") ? Convert.ToInt32(refItem["id"]) : 0;
                    string url = refItem["url"]?.ToString() ?? "";
                    string description = refItem["description"]?.ToString();
                    bool isInternal = refItem.ContainsKey("isInternal") && bool.Parse(refItem["isInternal"].ToString());
                    string openOption = refItem["openOption"]?.ToString() ?? "";
                    bool isDeleted = refItem.ContainsKey("isDeleted") && bool.Parse(refItem["isDeleted"].ToString());
                    bool fromParent = refItem.ContainsKey("fromParent") && bool.Parse(refItem["fromParent"].ToString());
                    int parentReferenceId = refItem.ContainsKey("parentReferenceId") ? Convert.ToInt32(refItem["parentReferenceId"]) : 0;

                    // Skip parent references already processed
                    if (fromParent && parentReferenceId > 0)
                    {
                        continue;
                    }

                    if (isDeleted)
                    {
                        continue;
                    }

                    Reference reference;
                    if (id > 0)
                    {
                        reference = existingReferences.FirstOrDefault(r => r.Id == id);
                        if (reference != null)
                        {
                            reference.Url = url;
                            reference.Description = description;
                            reference.IsInternal = isInternal;
                            reference.OpenOption = openOption;
                            reference.UpdateAudit(userId);
                            _unitOfWork.Reference.Update(reference);
                        }
                        else
                        {
                            Console.WriteLine($"Reference ID {id} not found, skipping.");
                            continue;
                        }
                    }
                    else
                    {
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
                    }

                    references.Add(new
                    {
                        id = 0,
                        url,
                        description,
                        isInternal,
                        openOption
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
        private async Task<List<object>> ProcessAttachmentsAsync(Category category, List<IFormFile> files, string attachmentData, string userId)
        {
            var attachments = new List<object>();
            if (string.IsNullOrEmpty(attachmentData))
            {
                return attachments;
            }

            try
            {
                var stagedAttachments = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(attachmentData);
                foreach (var att in stagedAttachments)
                {
                    int id = att.ContainsKey("id") ? Convert.ToInt32(att["id"]) : 0;
                    string originalFileName = att["fileName"]?.ToString() ?? "";
                    bool isDeleted = att.ContainsKey("isDeleted") && bool.Parse(att["isDeleted"].ToString());
                    bool fromParent = att.ContainsKey("fromParent") && bool.Parse(att["fromParent"].ToString());
                    int parentAttachmentId = att.ContainsKey("parentAttachmentId") ? Convert.ToInt32(att["parentAttachmentId"]) : 0;

                    if (isDeleted)
                    {
                        continue;
                    }

                    // Skip parent attachments already processed
                    if (fromParent && parentAttachmentId > 0)
                    {
                        continue;
                    }

                    string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFileName);
                    string filePath = Path.Combine("categories", category.Id.ToString(), guidFileName).Replace("\\", "/");
                    bool isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString());
                    string caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null;

                    attachments.Add(new
                    {
                        id,
                        fileName = guidFileName,
                        filePath,
                        originalFileName,
                        caption,
                        isInternal,
                        fromParent,
                        parentAttachmentId
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
    }
}