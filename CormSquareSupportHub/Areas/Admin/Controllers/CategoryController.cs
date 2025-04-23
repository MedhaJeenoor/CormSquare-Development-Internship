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
using Azure;
using SupportHub.DataAccess.Repository;

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
                throw new InvalidOperationException("UploadPath is not configured in appsettings.json. Please specify AttachmentSettings:UploadPath.");
            }

            Console.WriteLine($"UploadPath configured: {_attachmentSettings.UploadPath} (Absolute: {Path.GetFullPath(_attachmentSettings.UploadPath)})");

            try
            {
                if (!Directory.Exists(_attachmentSettings.UploadPath))
                {
                    Directory.CreateDirectory(_attachmentSettings.UploadPath);
                    Console.WriteLine($"Created base upload directory: {_attachmentSettings.UploadPath} (Absolute: {Path.GetFullPath(_attachmentSettings.UploadPath)})");
                }
                else
                {
                    Console.WriteLine($"Base upload directory exists: {_attachmentSettings.UploadPath} (Absolute: {Path.GetFullPath(_attachmentSettings.UploadPath)})");
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
        public async Task<IActionResult> Create(Category category, IFormFile[] files, string AttachmentData, string ReferenceData)
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

            // Log raw form data for debugging
            Console.WriteLine($"Form submission: Name={category.Name}, ParentCategoryId={category.ParentCategoryId}, FilesCount={(files?.Length ?? 0)}, AttachmentData={AttachmentData}, ReferenceData={ReferenceData}");
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
                // Double-check for duplicates within transaction
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
                // Explicitly set ParentCategoryId (0 -> null)
                category.ParentCategoryId = category.ParentCategoryId == 0 ? null : category.ParentCategoryId;

                Console.WriteLine($"Saving category: Name={category.Name}, ParentCategoryId={category.ParentCategoryId}");

                // Set DisplayOrder if not provided
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
                        Console.WriteLine($"Created directory: {categoryPath} (Absolute: {Path.GetFullPath(categoryPath)})");
                    }
                    else
                    {
                        Console.WriteLine($"Directory already exists: {categoryPath} (Absolute: {Path.GetFullPath(categoryPath)})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating directory {categoryPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
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

                // Process References
                var savedReferences = new List<object>();
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    savedReferences = await ProcessReferencesAsync(category, ReferenceData, user.Id);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Saved {savedReferences.Count} references for category {category.Id}");
                }

                // Process Attachments
                var savedAttachments = new List<object>();
                List<(Attachment Entity, string SourcePath, string DestPath, string OriginalFileName)> stagedAttachments = new List<(Attachment, string, string, string)>();
                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Any() ?? false))
                {
                    savedAttachments = await ProcessAttachmentsAsync(category, files?.ToList(), AttachmentData, user.Id);
                    var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == category.Id && !a.IsDeleted)).ToList();

                    Console.WriteLine($"Processing {savedAttachments.Count} attachments, Uploaded files: {(files?.Length ?? 0)}");

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
                            Console.WriteLine($"No uploaded file matched for {originalFileName}, sourcePath: {sourcePath ?? "null"}");
                        }
                        else
                        {
                            Console.WriteLine($"Matched uploaded file for {originalFileName}: {uploadedFile.FileName}");
                        }
                        stagedAttachments.Add((entity, sourcePath, destPath, originalFileName));
                    }
                    await _unitOfWork.SaveAsync();
                }

                // Save Files
                Console.WriteLine($"Saving {stagedAttachments.Count} attachment files...");
                foreach (var (entity, sourcePath, destPath, originalFileName) in stagedAttachments)
                {
                    if (System.IO.File.Exists(destPath))
                    {
                        Console.WriteLine($"File already exists: {destPath}, skipping copy.");
                        continue;
                    }

                    string destDir = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(destDir))
                    {
                        try
                        {
                            Directory.CreateDirectory(destDir);
                            Console.WriteLine($"Created destination directory: {destDir} (Absolute: {Path.GetFullPath(destDir)})");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating destination directory {destDir}: {ex.Message}");
                            throw new Exception($"Failed to create destination directory: {destDir}", ex);
                        }
                    }

                    try
                    {
                        if (sourcePath != null && System.IO.File.Exists(sourcePath))
                        {
                            System.IO.File.Copy(sourcePath, destPath, overwrite: false);
                            Console.WriteLine($"Copied file: {sourcePath} to {destPath} (Absolute: {Path.GetFullPath(destPath)})");
                        }
                        else
                        {
                            var file = files?.FirstOrDefault(f => f.FileName == originalFileName || f.FileName.EndsWith(Path.GetFileName(originalFileName)));
                            if (file != null)
                            {
                                using (var fileStream = new FileStream(destPath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    Console.WriteLine($"Uploaded file: {originalFileName} to {destPath} (Absolute: {Path.GetFullPath(destPath)})");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Error: No source file or uploaded file found for {originalFileName} (GUID: {entity.FileName})");
                                entity.IsDeleted = true;
                                _unitOfWork.Attachment.Update(entity);
                                await _unitOfWork.SaveAsync();
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"File operation error for {originalFileName} (GUID: {entity.FileName}): {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Console.WriteLine($"Error in Create POST: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            Response.Headers["Vary"] = "Accept-Encoding";

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

            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category model, List<IFormFile>? files, string? ReferenceData, string? AttachmentData, string submitAction, List<int>? deletedAttachmentIds, List<int>? deletedReferenceIds)
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
                    try
                    {
                        savedReferences = await ProcessReferencesAsync(category, ReferenceData, user.Id);
                        await _unitOfWork.SaveAsync();
                        Console.WriteLine($"Saved {savedReferences.Count} references for category {category.Id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Reference processing error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        throw new Exception("Failed to process references.", ex);
                    }
                }

                var savedAttachments = new List<object>();
                List<(Attachment Entity, string SourcePath, string DestPath)> stagedAttachments = new List<(Attachment, string, string)>();
                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Any() ?? false))
                {
                    try
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
                                Console.WriteLine($"No uploaded file matched for {originalFileName}, sourcePath: {sourcePath}");
                            }
                            stagedAttachments.Add((entity, sourcePath, destPath));
                        }
                        await _unitOfWork.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Attachment processing error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        throw new Exception("Failed to process attachments.", ex);
                    }
                }

                if (deletedAttachmentIds?.Any() ?? false)
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

                if (deletedReferenceIds?.Any() ?? false)
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

                foreach (var (entity, sourcePath, destPath) in stagedAttachments)
                {
                    if (System.IO.File.Exists(destPath))
                    {
                        Console.WriteLine($"File already exists: {destPath}, skipping copy.");
                        continue;
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
                            var file = files?.FirstOrDefault(f => f.FileName == entity.FileName || f.FileName.EndsWith(Path.GetFileName(entity.FileName)));
                            if (file != null)
                            {
                                using (var fileStream = new FileStream(destPath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    Console.WriteLine($"Uploaded file: {entity.FileName} to {destPath}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No source file found for {entity.FileName} and it’s not uploaded.");
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"File operation error for {entity.FileName}: {ex.Message}");
                        throw new Exception($"Failed to save file {entity.FileName}.", ex);
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
                Console.WriteLine($"Error in Edit: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return Json(new { success = false, message = $"Error updating category: {ex.Message}", innerException = ex.InnerException?.Message });
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
                if (category.SubCategories?.Any(sc => !sc.IsDeleted) ?? false)
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
                Console.WriteLine($"Error in Delete: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return Json(new { success = false, message = $"Error deleting category: {ex.Message}", innerException = ex.InnerException?.Message });
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
                    originalFileName = a.FileName
                }).ToList(),
                references = category.References.Where(r => !r.IsDeleted).Select(r => new
                {
                    id = r.Id,
                    url = r.Url,
                    description = r.Description,
                    isInternal = r.IsInternal,
                    openOption = r.OpenOption
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
                Console.WriteLine($"File not found: {filePath}");
                return NotFound();
            }

            try
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = "application/octet-stream";
                Console.WriteLine($"Serving file: {attachment.FileName} from {filePath}");
                return File(fileBytes, contentType, attachment.FileName);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                return StatusCode(500, new { message = $"Error reading file: {ex.Message}" });
            }
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                attachment.IsDeleted = true;
                attachment.UpdateAudit(user.Id);
                _unitOfWork.Attachment.Update(attachment);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                Console.WriteLine($"Marked attachment {id} as deleted");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in RemoveAttachment: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error removing attachment: {ex.Message}" });
            }
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reference.IsDeleted = true;
                reference.UpdateAudit(user.Id);
                _unitOfWork.Reference.Update(reference);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                Console.WriteLine($"Marked reference {id} as deleted");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in RemoveReference: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error removing reference: {ex.Message}" });
            }
        }

        private async Task<List<object>> ProcessReferencesAsync(Category category, string referenceData, string userId)
        {
            var savedReferences = new List<object>();
            if (string.IsNullOrEmpty(referenceData))
            {
                Console.WriteLine("No reference data provided.");
                return savedReferences;
            }

            List<dynamic> references;
            try
            {
                references = JsonConvert.DeserializeObject<List<dynamic>>(referenceData);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Invalid reference data JSON: {ex.Message}");
                throw new Exception("Invalid reference data format.", ex);
            }

            if (references == null || !references.Any())
            {
                Console.WriteLine("No references to process after deserialization.");
                return savedReferences;
            }

            var existingReferences = (await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == category.Id && !r.IsDeleted)).ToList();

            foreach (var refItem in references)
            {
                int id = refItem.id ?? 0;
                string url = refItem.url?.ToString();
                string description = refItem.description?.ToString();
                bool isInternal = refItem.isInternal ?? false;
                string openOption = refItem.openOption?.ToString() ?? "_self";
                bool isDeleted = refItem.isDeleted ?? false;

                if (string.IsNullOrEmpty(url) || isDeleted)
                {
                    Console.WriteLine($"Skipping reference: URL is empty or marked as deleted.");
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

                savedReferences.Add(new
                {
                    id = reference.Id,
                    url = reference.Url,
                    description = reference.Description,
                    isInternal = reference.IsInternal,
                    openOption = reference.OpenOption
                });
            }

            return savedReferences;
        }

        private async Task<List<object>> ProcessAttachmentsAsync(Category category, List<IFormFile>? files, string attachmentData, string userId)
        {
            var savedAttachments = new List<object>();
            if (string.IsNullOrEmpty(attachmentData))
            {
                Console.WriteLine("No attachment data provided.");
                return savedAttachments;
            }

            List<dynamic> attachments;
            try
            {
                attachments = JsonConvert.DeserializeObject<List<dynamic>>(attachmentData);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Invalid attachment data JSON: {ex.Message}");
                throw new Exception("Invalid attachment data format.", ex);
            }

            if (attachments == null || !attachments.Any())
            {
                Console.WriteLine("No attachments to process after deserialization.");
                return savedAttachments;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".txt" };

            foreach (var attItem in attachments)
            {
                int id = attItem.id ?? 0;
                string fileName = attItem.fileName?.ToString();
                string caption = attItem.caption?.ToString();
                bool isInternal = attItem.isInternal ?? false;
                bool isDeleted = attItem.isDeleted ?? false;

                if (string.IsNullOrEmpty(fileName) || isDeleted)
                {
                    Console.WriteLine($"Skipping attachment: FileName is empty or marked as deleted.");
                    continue;
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    Console.WriteLine($"Skipping attachment: Invalid file extension {extension} for {fileName}.");
                    continue;
                }

                string guidFileName = Guid.NewGuid().ToString() + extension;
                string relativeFilePath = Path.Combine("categories", category.Id.ToString(), guidFileName);

                savedAttachments.Add(new
                {
                    id,
                    fileName = guidFileName,
                    filePath = relativeFilePath,
                    originalFileName = fileName,
                    caption,
                    isInternal
                });
            }

            return savedAttachments;
        }

        private async Task<string> ProcessHtmlContentMediaAsync(Category category, string htmlContent, string userId, List<IFormFile>? files)
        {
            if (string.IsNullOrEmpty(htmlContent))
            {
                Console.WriteLine("No HtmlContent to process.");
                return htmlContent;
            }

            var srcRegex = new Regex(@"(?:<img|<video|<audio)\s+[^>]*src=""([^""]+)""", RegexOptions.IgnoreCase);
            var matches = srcRegex.Matches(htmlContent);
            string updatedHtmlContent = htmlContent;

            foreach (Match match in matches)
            {
                string src = match.Groups[1].Value;
                if (src.StartsWith("http") || src.StartsWith("data:"))
                {
                    Console.WriteLine($"Skipping external or data URL in HtmlContent: {src}");
                    continue;
                }

                string fileName = Path.GetFileName(src);
                if (string.IsNullOrEmpty(fileName))
                {
                    Console.WriteLine($"Invalid src in HtmlContent: {src}");
                    continue;
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mp3", ".wav" };
                if (!allowedExtensions.Contains(extension))
                {
                    Console.WriteLine($"Invalid file extension in HtmlContent: {extension} for {fileName}");
                    continue;
                }

                string guidFileName = Guid.NewGuid().ToString() + extension;
                string relativeFilePath = Path.Combine("categories", category.Id.ToString(), guidFileName);
                string destPath = Path.Combine(_attachmentSettings.UploadPath, relativeFilePath);

                string destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    Console.WriteLine($"Created destination directory for HtmlContent media: {destDir} (Absolute: {Path.GetFullPath(destDir)})");
                }

                var file = files?.FirstOrDefault(f => f.FileName == fileName || f.FileName.EndsWith(fileName));
                if (file != null)
                {
                    try
                    {
                        using (var fileStream = new FileStream(destPath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                            Console.WriteLine($"Saved HtmlContent media: {fileName} to {destPath} (Absolute: {Path.GetFullPath(destPath)})");
                        }

                        var attachment = new Attachment
                        {
                            FileName = guidFileName,
                            FilePath = relativeFilePath,
                            Caption = $"Media from HtmlContent: {fileName}",
                            IsInternal = true,
                            CategoryId = category.Id
                        };
                        attachment.UpdateAudit(userId);
                        _unitOfWork.Attachment.Add(attachment);
                        await _unitOfWork.SaveAsync();

                        string newSrc = $"/Uploads/categories/{category.Id}/{guidFileName}";
                        updatedHtmlContent = updatedHtmlContent.Replace(src, newSrc);
                        Console.WriteLine($"Updated HtmlContent src: {src} to {newSrc}");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Error saving HtmlContent media {fileName}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        throw new Exception($"Failed to save HtmlContent media: {fileName}", ex);
                    }
                }
                else
                {
                    Console.WriteLine($"No uploaded file found for HtmlContent media: {fileName}");
                }
            }

            return updatedHtmlContent;
        }
    }
}