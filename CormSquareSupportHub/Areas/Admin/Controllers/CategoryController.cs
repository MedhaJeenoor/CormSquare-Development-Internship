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

            // Only include the category's own attachments and references
            var attachments = category.Attachments.Where(a => !a.IsDeleted).ToList();
            var references = category.References.Where(r => !r.IsDeleted).ToList();

            // Copy parent attachments and references only if they don't already exist
            if (category.ParentCategoryId.HasValue)
            {
                var parentCategory = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                    c => c.Id == category.ParentCategoryId.Value && !c.IsDeleted,
                    includeProperties: "Attachments,References"
                );
                if (parentCategory != null)
                {
                    foreach (var parentAttachment in parentCategory.Attachments.Where(a => !a.IsDeleted))
                    {
                        // Check if an attachment with the same FileName and FilePath already exists
                        if (!attachments.Any(a => a.FileName == parentAttachment.FileName && a.FilePath == parentAttachment.FilePath && !a.IsDeleted))
                        {
                            var newAttachment = new Attachment
                            {
                                FileName = parentAttachment.FileName,
                                FilePath = Path.Combine("categories", category.Id.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(parentAttachment.FileName)).Replace("\\", "/"),
                                Caption = parentAttachment.Caption,
                                IsInternal = parentAttachment.IsInternal,
                                CategoryId = category.Id,
                                IsDeleted = false
                            };
                            newAttachment.UpdateAudit(user.Id);
                            _unitOfWork.Attachment.Add(newAttachment);
                            attachments.Add(newAttachment);

                            // Copy the physical file
                            string sourcePath = Path.Combine(_attachmentSettings.UploadPath, parentAttachment.FilePath);
                            string destPath = Path.Combine(_attachmentSettings.UploadPath, newAttachment.FilePath);
                            if (System.IO.File.Exists(sourcePath))
                            {
                                string destDir = Path.GetDirectoryName(destPath);
                                if (!Directory.Exists(destDir))
                                {
                                    Directory.CreateDirectory(destDir);
                                }
                                System.IO.File.Copy(sourcePath, destPath, overwrite: false);
                                Console.WriteLine($"Copied parent attachment: {sourcePath} to {destPath}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Skipping duplicate parent attachment: FileName={parentAttachment.FileName}");
                        }
                    }

                    foreach (var parentReference in parentCategory.References.Where(r => !r.IsDeleted))
                    {
                        // Check if a reference with the same Url and Description already exists
                        if (!references.Any(r => r.Url == parentReference.Url && r.Description == parentReference.Description && !r.IsDeleted))
                        {
                            var newReference = new Reference
                            {
                                Url = parentReference.Url,
                                Description = parentReference.Description,
                                IsInternal = parentReference.IsInternal,
                                OpenOption = parentReference.OpenOption,
                                CategoryId = category.Id,
                                IsDeleted = false
                            };
                            newReference.UpdateAudit(user.Id);
                            _unitOfWork.Reference.Add(newReference);
                            references.Add(newReference);
                        }
                        else
                        {
                            Console.WriteLine($"Skipping duplicate parent reference: Url={parentReference.Url}");
                        }
                    }

                    await _unitOfWork.SaveAsync();
                }
            }

            category.Categories = (await _unitOfWork.Category.GetAllAsync(
                c => !c.IsDeleted && c.ParentCategoryId == null && c.Id != id.Value
            )).ToList();

            ViewData["AttachmentLinks"] = attachments.Select(a => new
            {
                Id = a.Id,
                FileName = a.FileName,
                Url = Url.Action("DownloadAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }),
                IsInternal = a.IsInternal,
                OriginalFileName = a.FileName
            }).ToList();

            ViewData["ReferenceLinks"] = references.Select(r => new
            {
                Id = r.Id,
                Url = r.Url,
                Description = r.Description,
                IsInternal = r.IsInternal,
                OpenOption = r.OpenOption
            }).ToList();

            Console.WriteLine($"CategoryController.Edit: Fetched category ID {id} with {attachments.Count} attachments and {references.Count} references");
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
                model.Attachments = model.Attachments ?? new List<Attachment>();
                model.References = model.References ?? new List<Reference>();
                ViewData["AttachmentLinks"] = model.Attachments.Where(a => !a.IsDeleted).Select(a => new
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    Url = Url.Action("OpenAttachment", "Category", new { attachmentId = a.Id, area = "Admin" }),
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
                var savedAttachments = new List<object>();
                List<(Attachment Entity, string SourcePath, string DestPath, string OriginalFileName)> stagedAttachments = new();

                // Process Parent Category Attachments and References
                await ProcessParentCategoryAsync(category, user.Id, deletedAttachmentIds, deletedReferenceIds, AttachmentData, ReferenceData, savedAttachments, savedReferences, stagedAttachments);

                // Process Form References
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    savedReferences.AddRange(await ProcessReferencesAsync(category, ReferenceData, user.Id));
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Saved {savedReferences.Count} references for category {category.Id}");
                }

                // Process New Attachments from AttachmentData and Files
                if (!string.IsNullOrEmpty(AttachmentData) || files?.Any() == true)
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

                    // Fetch existing attachments before calling ProcessAttachmentsAsync
                    var existingAttachments = (await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == category.Id && !a.IsDeleted)).ToList();

                    // Process existing and parent-sourced attachments via ProcessAttachmentsAsync
                    savedAttachments.AddRange(await ProcessAttachmentsAsync(category, files, AttachmentData, user.Id, existingAttachments));

                    foreach (var item in savedAttachments)
                    {
                        var id = (int)item.GetType().GetProperty("id").GetValue(item);
                        var guidFileName = (string)item.GetType().GetProperty("fileName").GetValue(item);
                        var filePath = (string)item.GetType().GetProperty("filePath").GetValue(item);
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
                            }
                            else
                            {
                                Console.WriteLine($"Attachment ID {id} not found, skipping.");
                                continue;
                            }
                        }
                        else if (!fromParent) // New attachment, already handled above
                        {
                            entity = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.FileName == guidFileName && a.CategoryId == category.Id && !a.IsDeleted);
                            if (entity == null)
                            {
                                Console.WriteLine($"New attachment not found after creation: fileName={guidFileName}");
                                continue;
                            }
                        }
                        else
                        {
                            continue; // Parent-sourced attachments are already staged
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

                    // Skip parent-sourced references in Create action
                    if (isCreateAction && fromParent && parentReferenceId > 0)
                    {
                        Console.WriteLine($"Skipping parent-sourced reference in Create: parentReferenceId={parentReferenceId}, url={url}");
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
                            Console.WriteLine($"Updated reference: id={reference.Id}, url={url}");
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
                        Console.WriteLine($"Added new reference: url={url}");
                    }

                    references.Add(new
                    {
                        id = 0,
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

                    // Skip parent-sourced attachments in Create action (handled separately)
                    if (isCreateAction && fromParent && parentAttachmentId > 0)
                    {
                        Console.WriteLine($"Skipping parent-sourced attachment in Create: parentAttachmentId={parentAttachmentId}, fileName={originalFileName}");
                        continue;
                    }

                    // Skip new attachments (id == 0 and !fromParent) - these are handled in Create/Edit via the files array
                    if (id == 0 && !fromParent)
                    {
                        Console.WriteLine($"Skipping new attachment (handled by files array): fileName={originalFileName}");
                        continue;
                    }

                    string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFileName);
                    string filePath = Path.Combine("categories", category.Id.ToString(), guidFileName).Replace("\\", "/");

                    Attachment attachment;
                    if (id > 0)
                    {
                        attachment = existingAttachments.FirstOrDefault(a => a.Id == id);
                        if (attachment != null)
                        {
                            attachment.Caption = caption;
                            attachment.IsInternal = isInternal;
                            attachment.UpdateAudit(userId);
                            _unitOfWork.Attachment.Update(attachment);
                            Console.WriteLine($"Updated attachment: id={attachment.Id}, fileName={originalFileName}");
                        }
                        else
                        {
                            Console.WriteLine($"Attachment ID {id} not found, skipping.");
                            continue;
                        }
                    }
                    else
                    {
                        // Only handle parent-sourced attachments here (id == 0 && fromParent)
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
                        Console.WriteLine($"Added new parent-sourced attachment: fileName={originalFileName}, parentAttachmentId={parentAttachmentId}");
                    }

                    attachments.Add(new
                    {
                        id,
                        fileName = guidFileName,
                        filePath,
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
                return NotFound();
            }

            // Determine the content type based on file extension
            string contentType = GetContentType(attachment.FileName);
            Console.WriteLine($"Serving attachment ID {attachmentId}, FileName: {attachment.FileName}, Content-Type: {contentType}");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            // Explicitly set Content-Disposition to inline, without filename to avoid download prompts
            Response.Headers["Content-Disposition"] = "inline";
            Response.Headers["Content-Type"] = contentType;
            // Additional headers to prevent caching issues
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            Console.WriteLine($"OpenAttachment Response Headers - Content-Disposition: {Response.Headers["Content-Disposition"]}, Content-Type: {Response.Headers["Content-Type"]}, FileName: {attachment.FileName}");

            return File(memory, contentType);
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

            // Log parent category attachments and references
            Console.WriteLine($"Parent Category ID: {parentCategory.Id}, Attachments: {JsonConvert.SerializeObject(parentCategory.Attachments.Select(a => new { a.Id, a.FileName, a.IsDeleted }))}");
            Console.WriteLine($"Parent Category ID: {parentCategory.Id}, References: {JsonConvert.SerializeObject(parentCategory.References.Select(r => new { r.Id, r.Url, r.IsDeleted }))}");
            Console.WriteLine($"DeletedAttachmentIds: {string.Join(",", deletedAttachmentIds ?? new List<int>())}");
            Console.WriteLine($"DeletedReferenceIds: {string.Join(",", deletedReferenceIds ?? new List<int>())}");
            Console.WriteLine($"AttachmentData: {AttachmentData}");
            Console.WriteLine($"ReferenceData: {ReferenceData}");

            // Parse AttachmentData and ReferenceData
            var attachmentDataItems = !string.IsNullOrEmpty(AttachmentData)
                ? JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(AttachmentData)
                : new List<Dictionary<string, object>>();
            var referenceDataItems = !string.IsNullOrEmpty(ReferenceData)
                ? JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(ReferenceData)
                : new List<Dictionary<string, object>>();

            // Log the deserialized data for debugging
            Console.WriteLine($"Deserialized AttachmentDataItems: {JsonConvert.SerializeObject(attachmentDataItems)}");
            Console.WriteLine($"Deserialized ReferenceDataItems: {JsonConvert.SerializeObject(referenceDataItems)}");

            // Copy Parent Attachments
            var parentAttachments = parentCategory.Attachments
                .Where(a => !a.IsDeleted && !(deletedAttachmentIds?.Contains(a.Id) ?? false))
                .Where(a =>
                {
                    var attData = attachmentDataItems.FirstOrDefault(d => d.ContainsKey("parentAttachmentId") && d["parentAttachmentId"] != null && Convert.ToInt32(d["parentAttachmentId"]) == a.Id);
                    if (attData == null)
                    {
                        Console.WriteLine($"No AttachmentData entry found for parent attachment: Id={a.Id}, FileName={a.FileName}. Including by default.");
                        return true; // Include by default if no data is provided (adjust based on requirements)
                    }

                    bool isDeletedInData = attData.ContainsKey("isDeleted") && bool.Parse(attData["isDeleted"].ToString());
                    bool isMarkedWithX = attData.ContainsKey("isMarkedWithX") && bool.Parse(attData["isMarkedWithX"].ToString());
                    Console.WriteLine($"Evaluating parent attachment: Id={a.Id}, FileName={a.FileName}, isDeletedInData={isDeletedInData}, isMarkedWithX={isMarkedWithX}");

                    if (isDeletedInData || isMarkedWithX)
                    {
                        Console.WriteLine($"Skipping parent attachment: Id={a.Id}, FileName={a.FileName}, isDeletedInData={isDeletedInData}, isMarkedWithX={isMarkedWithX}");
                        return false;
                    }
                    return true;
                })
                .ToList();

            if (!parentAttachments.Any())
            {
                Console.WriteLine("No non-deleted parent attachments to copy after filtering.");
            }

            foreach (var parentAttachment in parentAttachments)
            {
                Console.WriteLine($"Processing parent attachment: Id={parentAttachment.Id}, FileName={parentAttachment.FileName}");
                string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(parentAttachment.FileName);
                string relativeFilePath = Path.Combine("categories", category.Id.ToString(), guidFileName);
                string destPath = Path.Combine(_attachmentSettings.UploadPath, relativeFilePath);
                string sourcePath = Path.Combine(_attachmentSettings.UploadPath, parentAttachment.FilePath);

                var newAttachment = new Attachment
                {
                    FileName = guidFileName,
                    FilePath = relativeFilePath,
                    Caption = parentAttachment.Caption,
                    IsInternal = parentAttachment.IsInternal,
                    CategoryId = category.Id
                };
                newAttachment.UpdateAudit(userId);
                _unitOfWork.Attachment.Add(newAttachment);

                savedAttachments.Add(new
                {
                    id = 0,
                    fileName = guidFileName,
                    filePath = relativeFilePath,
                    originalFileName = parentAttachment.FileName,
                    caption = parentAttachment.Caption,
                    isInternal = parentAttachment.IsInternal,
                    fromParent = true,
                    parentAttachmentId = parentAttachment.Id,
                    isMarkedWithX = false
                });

                stagedAttachments.Add((newAttachment, sourcePath, destPath, parentAttachment.FileName));
                Console.WriteLine($"Staged parent attachment: {parentAttachment.FileName} -> {relativeFilePath}");
            }

            // Copy Parent References
            var parentReferences = parentCategory.References
                .Where(r => !r.IsDeleted && !(deletedReferenceIds?.Contains(r.Id) ?? false))
                .Where(r =>
                {
                    var refData = referenceDataItems.FirstOrDefault(d => d.ContainsKey("parentReferenceId") && d["parentReferenceId"] != null && Convert.ToInt32(d["parentReferenceId"]) == r.Id);
                    if (refData == null)
                    {
                        Console.WriteLine($"No ReferenceData entry found for parent reference: Id={r.Id}, Url={r.Url}. Including by default.");
                        return true; // Include by default if no data is provided (adjust based on requirements)
                    }

                    bool isDeletedInData = refData.ContainsKey("isDeleted") && bool.Parse(refData["isDeleted"].ToString());
                    bool isMarkedWithX = refData.ContainsKey("isMarkedWithX") && bool.Parse(refData["isMarkedWithX"].ToString());
                    Console.WriteLine($"Evaluating parent reference: Id={r.Id}, Url={r.Url}, isDeletedInData={isDeletedInData}, isMarkedWithX={isMarkedWithX}");

                    if (isDeletedInData || isMarkedWithX)
                    {
                        Console.WriteLine($"Skipping parent reference: Id={r.Id}, Url={r.Url}, isDeletedInData={isDeletedInData}, isMarkedWithX={isMarkedWithX}");
                        return false;
                    }
                    return true;
                })
                .ToList();

            if (!parentReferences.Any())
            {
                Console.WriteLine("No non-deleted parent references to copy after filtering.");
            }

            foreach (var parentReference in parentReferences)
            {
                Console.WriteLine($"Processing parent reference: Id={parentReference.Id}, Url={parentReference.Url}");
                var newReference = new Reference
                {
                    Url = parentReference.Url,
                    Description = parentReference.Description,
                    IsInternal = parentReference.IsInternal,
                    OpenOption = parentReference.OpenOption,
                    CategoryId = category.Id
                };
                newReference.UpdateAudit(userId);
                _unitOfWork.Reference.Add(newReference);

                savedReferences.Add(new
                {
                    id = 0,
                    url = parentReference.Url,
                    description = parentReference.Description,
                    isInternal = parentReference.IsInternal,
                    openOption = parentReference.OpenOption,
                    fromParent = true,
                    parentReferenceId = parentReference.Id,
                    isMarkedWithX = false
                });
                Console.WriteLine($"Added parent reference: {parentReference.Url}");
            }

            await _unitOfWork.SaveAsync();
        }
    }
}