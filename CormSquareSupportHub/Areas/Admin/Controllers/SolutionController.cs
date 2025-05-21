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
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Security.Policy;

namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SolutionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ExternalUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AttachmentSettings _attachmentSettings;

        public SolutionController(
            IUnitOfWork unitOfWork,
            UserManager<ExternalUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IOptions<AttachmentSettings> attachmentSettings)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _attachmentSettings = attachmentSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> MySolutions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var solutions = await _unitOfWork.Solution.GetAllAsync(s => s.AuthorId == user.Id && !s.IsDeleted,
                includeProperties: "Category,Product,SubCategory,Author");
            return View(solutions);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? issueId, int? solutionId)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            Response.Headers["Vary"] = "Accept-Encoding";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var model = new SolutionViewModel
            {
                Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList(),
                Products = (await _unitOfWork.Product.GetAllAsync()).ToList(),
                SubCategories = new List<SubCategory>(),
                Attachments = new List<SolutionAttachment>(),
                References = new List<SolutionReference>()
            };

            if (solutionId.HasValue)
            {
                var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == solutionId.Value && s.AuthorId == user.Id && !s.IsDeleted,
                    includeProperties: "Attachments,References");
                if (solution == null) return NotFound();

                model.Id = solution.Id;
                model.Title = solution.Title;
                model.ProductId = solution.ProductId;
                model.SubCategoryId = solution.SubCategoryId;
                model.CategoryId = solution.CategoryId;
                model.Contributors = solution.Contributors;
                model.HtmlContent = solution.HtmlContent;
                model.IssueDescription = solution.IssueDescription;
                model.SubCategories = (await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == solution.ProductId)).ToList();
                model.Attachments = solution.Attachments.Where(a => !a.IsDeleted).ToList();
                model.References = solution.References.Where(r => !r.IsDeleted).ToList();

                // Generate attachment links for solution attachments
                var attachmentLinks = model.Attachments.Select(a => new
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    Url = Url.Action("DownloadAttachment", "Solution", new { attachmentId = a.Id, area = "Admin" }, protocol: Request.Scheme), // Use absolute URL
                    IsInternal = a.IsInternal
                }).ToList();
                Console.WriteLine($"Attachment Links: {JsonConvert.SerializeObject(attachmentLinks)}");
                ViewData["AttachmentLinks"] = attachmentLinks;

                ViewData["ReferenceLinks"] = model.References.Select(r => new
                {
                    Id = r.Id,
                    Url = r.Url,
                    Description = r.Description,
                    IsInternal = r.IsInternal,
                    OpenOption = r.OpenOption
                }).ToList();
            }
            else if (issueId.HasValue)
            {
                var issue = await _unitOfWork.Issue.GetFirstOrDefaultAsync(i => i.Id == issueId.Value,
                    includeProperties: "Product,SubCategory");
                if (issue != null)
                {
                    model.IssueDescription = issue.Description;
                    model.ProductId = issue.ProductId;
                    model.SubCategoryId = issue.SubCategoryId;
                    model.SubCategories = (await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == issue.ProductId)).ToList();
                }
            }

            ViewData["HtmlContent"] = model.HtmlContent ?? "";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert(SolutionViewModel model, List<IFormFile>? files,
    string? ReferenceData, string? AttachmentData, string submitAction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Remove unnecessary ModelState entries
            ModelState.Remove("Products");
            ModelState.Remove("Categories");
            ModelState.Remove("SubCategories");
            ModelState.Remove("Attachments");
            ModelState.Remove("References");
            ModelState.Remove("AuthorId");
            ModelState.Remove("Author");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("UpdatedBy");
            ModelState.Remove("UpdatedDate");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("PlainTextContent");
            ModelState.Remove("DocId");
            ModelState.Remove("Status");

            string redirectUrl = null;
            Solution solution = null;
            string userId = user.Id;
            var savedAttachments = new List<object>();
            var savedReferences = new List<object>();
            List<(SolutionAttachment Entity, string SourcePath, string DestPath, IFormFile File)> stagedAttachments = new List<(SolutionAttachment, string, string, IFormFile)>();

            if (submitAction == "Cancel")
            {
                Console.WriteLine("Cancel button clicked, no server-side changes.");
                redirectUrl = model.Id > 0 ? Url.Action("MySolutions") : Url.Action("IssueList", "Issue");
                return Json(new { success = true, redirectTo = redirectUrl });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine($"ModelState invalid: {string.Join(", ", errors)}");
                await PopulateViewModel(model);
                return Json(new { success = false, message = "Validation failed", errors });
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Save Solution
                if (model.Id > 0) // Edit
                {
                    solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == model.Id && !s.IsDeleted,
                        includeProperties: "Attachments,References");
                    if (solution == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return Json(new { success = false, message = "Solution not found" });
                    }

                    solution.Title = model.Title;
                    solution.CategoryId = model.CategoryId;
                    solution.ProductId = model.ProductId;
                    solution.SubCategoryId = model.SubCategoryId;
                    solution.Contributors = model.Contributors;
                    solution.HtmlContent = model.HtmlContent;
                    solution.PlainTextContent = ConvertHtmlToPlainText(model.HtmlContent);
                    solution.IssueDescription = model.IssueDescription;
                    solution.Status = submitAction == "Save" ? "Draft" : "Submitted";
                    solution.AuthorId = userId;
                    solution.UpdateAudit(userId);
                    _unitOfWork.Solution.Update(solution);
                }
                else // Create
                {
                    solution = new Solution
                    {
                        Title = model.Title,
                        CategoryId = model.CategoryId,
                        ProductId = model.ProductId,
                        SubCategoryId = model.SubCategoryId,
                        AuthorId = userId,
                        Contributors = model.Contributors,
                        HtmlContent = model.HtmlContent,
                        PlainTextContent = ConvertHtmlToPlainText(model.HtmlContent),
                        IssueDescription = model.IssueDescription,
                        Status = submitAction == "Save" ? "Draft" : "Submitted"
                    };
                    solution.UpdateAudit(userId);
                    _unitOfWork.Solution.Add(solution);
                }

                if (string.IsNullOrEmpty(solution.AuthorId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    Console.WriteLine("Validation failed: AuthorId is null or empty.");
                    return Json(new { success = false, message = "AuthorId is required." });
                }

                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Saved solution: {solution.Id}, AuthorId: {solution.AuthorId}");

                // Process References
                try
                {
                    if (!string.IsNullOrEmpty(ReferenceData))
                    {
                        savedReferences = await ProcessReferencesAsync(solution, ReferenceData, userId);
                        await _unitOfWork.SaveAsync();
                        Console.WriteLine($"Saved {savedReferences.Count} references for solution {solution.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reference processing error: {ex.Message}");
                    await _unitOfWork.RollbackTransactionAsync();
                    return Json(new { success = false, message = $"Reference processing failed: {ex.Message}" });
                }

                // Process Attachments
                try
                {
                    if (!string.IsNullOrEmpty(AttachmentData) || (files != null && files.Any()))
                    {
                        savedAttachments = await ProcessAttachmentsAsync(solution, files, AttachmentData, userId, stagedAttachments);
                        await _unitOfWork.SaveAsync();
                        Console.WriteLine($"Saved {savedAttachments.Count} attachments for solution {solution.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attachment processing error: {ex.Message}");
                    await _unitOfWork.RollbackTransactionAsync();
                    return Json(new { success = false, message = $"Attachment processing failed: {ex.Message}" });
                }

                // Save Files
                string solutionPath = Path.Combine(_attachmentSettings.UploadPath, "solutions", solution.Id.ToString());
                if (!Directory.Exists(solutionPath)) Directory.CreateDirectory(solutionPath);

                foreach (var (entity, sourcePath, destPath, file) in stagedAttachments)
                {
                    if (System.IO.File.Exists(destPath))
                    {
                        Console.WriteLine($"File already exists: {destPath}, skipping copy.");
                        continue;
                    }

                    try
                    {
                        if (file != null)
                        {
                            using (var fileStream = new FileStream(destPath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                                Console.WriteLine($"Uploaded file: {entity.FileName} to {destPath}");
                            }
                        }
                        else if (sourcePath != null && System.IO.File.Exists(sourcePath))
                        {
                            System.IO.File.Copy(sourcePath, destPath, overwrite: false);
                            Console.WriteLine($"Copied category file: {sourcePath} to {destPath}");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: No source file found for {entity.FileName} and it’s not uploaded.");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving file {entity.FileName} to {destPath}: {ex.Message}");
                        throw;
                    }
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                Console.WriteLine($"Committed transaction: {submitAction} solution {solution.Id}, {savedAttachments.Count} attachments, {savedReferences.Count} references");
                TempData["success"] = submitAction == "Save" ? "Solution saved as draft!" : "Solution submitted for review!";
                redirectUrl = submitAction == "Save" ? Url.Action("MySolutions") : Url.Action("Approvals");

                return Json(new { success = true, redirectTo = redirectUrl, attachments = savedAttachments, references = savedReferences });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Upsert: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Approvals()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var solutions = await _unitOfWork.Solution.GetAllAsync(
                s => !s.IsDeleted && (s.Status == "Submitted" || s.Status == "UnderReview" || s.Status == "Approved" || s.Status == "Rejected" || s.Status == "NeedsRevision"),
                includeProperties: "Category,Product,SubCategory,Author"
            );

            return View(solutions);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (id == null || id == 0) return NotFound();

            var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == id && s.AuthorId == user.Id && !s.IsDeleted,
                includeProperties: "Attachments,References,Category,Product,SubCategory");
            if (solution == null) return NotFound();

            var model = new SolutionViewModel
            {
                Id = solution.Id,
                Title = solution.Title,
                ProductId = solution.ProductId,
                SubCategoryId = solution.SubCategoryId,
                CategoryId = solution.CategoryId,
                Contributors = solution.Contributors,
                HtmlContent = solution.HtmlContent,
                IssueDescription = solution.IssueDescription,
                Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList(),
                Products = (await _unitOfWork.Product.GetAllAsync()).ToList(),
                SubCategories = (await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == solution.ProductId)).ToList(),
                Attachments = solution.Attachments.Where(a => !a.IsDeleted).ToList(),
                References = solution.References.Where(r => !r.IsDeleted).ToList()
            };

            ViewData["HtmlContent"] = model.HtmlContent ?? "";
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == id && s.AuthorId == user.Id && !s.IsDeleted);
            if (solution == null) return NotFound();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Solution.SoftDelete(solution, user.Id);
                var attachments = await _unitOfWork.SolutionAttachment.GetAllAsync(a => a.SolutionId == id && !a.IsDeleted);
                foreach (var attachment in attachments)
                {
                    _unitOfWork.SolutionAttachment.SoftDelete(attachment, user.Id);
                }

                var references = await _unitOfWork.SolutionReference.GetAllAsync(r => r.SolutionId == id && !r.IsDeleted);
                foreach (var reference in references)
                {
                    _unitOfWork.SolutionReference.Update(reference);
                    _unitOfWork.SolutionReference.SoftDelete(reference, user.Id);
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = "Solution deleted successfully (Soft Delete)!";
                return RedirectToAction("MySolutions");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Delete: {ex.Message}\nStackTrace: {ex.StackTrace}");
                TempData["error"] = "An error occurred while deleting the solution.";
                return RedirectToAction("MySolutions");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryTemplate(int categoryId)
        {
            Console.WriteLine($"GetCategoryTemplate called: categoryId={categoryId}");
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized access: User not found.");
                return Unauthorized();
            }

            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == categoryId && !c.IsDeleted,
                includeProperties: "Attachments,References"
            );
            if (category == null)
            {
                Console.WriteLine($"Category ID {categoryId} not found or deleted.");
                return Json(new { success = false, message = "Category not found." });
            }

            var data = new
            {
                htmlContent = category.HtmlContent ?? "",
                attachments = category.Attachments.Where(a => !a.IsDeleted).Select(a => new
                {
                    id = a.Id,
                    fileName = a.FileName,
                    url = Url.Action("OpenAttachment", "Category", new { area = "Admin", attachmentId = a.Id }),
                    caption = a.Caption,
                    isInternal = a.IsInternal,
                    originalFileName = a.FileName,
                    fromParent = true,
                    parentAttachmentId = a.Id
                }).ToList(),
                references = category.References.Where(r => !r.IsDeleted).Select(r => new
                {
                    id = r.Id,
                    url = r.Url,
                    description = r.Description,
                    isInternal = r.IsInternal,
                    openOption = r.OpenOption,
                    fromParent = true,
                    parentReferenceId = r.Id
                }).ToList()
            };

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            Console.WriteLine($"Returning category template: categoryId={categoryId}, attachments={data.attachments.Count}, references={data.references.Count}");
            return Json(new { success = true, data });
        }


        private async Task<List<object>> ProcessAttachmentsAsync(Solution solution, List<IFormFile>? files, string? attachmentData, string userId, List<(SolutionAttachment Entity, string SourcePath, string DestPath, IFormFile File)> stagedAttachments)
        {
            var attachments = new List<object>();
            var existingAttachments = (await _unitOfWork.SolutionAttachment.GetAllAsync(a => a.SolutionId == solution.Id && !a.IsDeleted)).ToList();
            Console.WriteLine($"Processing attachments for solution {solution.Id}. Existing attachments: {existingAttachments.Count}");

            if (!string.IsNullOrEmpty(attachmentData))
            {
                var stagedAttachmentData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(attachmentData);
                Console.WriteLine($"Received AttachmentData: {attachmentData}");

                foreach (var att in stagedAttachmentData)
                {
                    int attachmentId = att.ContainsKey("id") ? Convert.ToInt32(att["id"]) : 0;
                    string originalFileName = att["fileName"]?.ToString();
                    bool isDeleted = att.ContainsKey("isDeleted") && bool.Parse(att["isDeleted"].ToString());
                    if (isDeleted)
                    {
                        Console.WriteLine($"Skipping attachment ID {attachmentId} (fileName: {originalFileName}) marked as deleted.");
                        continue;
                    }

                    string guidFileName = att.ContainsKey("guidFileName") && !string.IsNullOrEmpty(att["guidFileName"]?.ToString())
                        ? att["guidFileName"].ToString()
                        : $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
                    string filePath = Path.Combine("solutions", solution.Id.ToString(), guidFileName).Replace("\\", "/");
                    bool isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString());
                    string caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null;
                    bool fromParent = att.ContainsKey("fromParent") && bool.Parse(att["fromParent"].ToString());
                    int? parentAttachmentId = att.ContainsKey("parentAttachmentId") && !string.IsNullOrEmpty(att["parentAttachmentId"]?.ToString())
                        ? Convert.ToInt32(att["parentAttachmentId"]) : null;

                    Console.WriteLine($"Processing attachment ID {attachmentId}: fileName={originalFileName}, guidFileName={guidFileName}, isInternal={isInternal}, fromParent={fromParent}");

                    var existingAttachment = attachmentId > 0 ? existingAttachments.FirstOrDefault(a => a.Id == attachmentId) : null;
                    SolutionAttachment entity;

                    if (existingAttachment != null)
                    {
                        existingAttachment.Caption = caption;
                        existingAttachment.IsInternal = isInternal;
                        existingAttachment.UpdateAudit(userId);
                        _unitOfWork.SolutionAttachment.Update(existingAttachment);
                        attachments.Add(new
                        {
                            id = existingAttachment.Id,
                            fileName = existingAttachment.FileName,
                            filePath = existingAttachment.FilePath,
                            caption,
                            isInternal,
                            originalFileName,
                            fromParent = false,
                            parentAttachmentId = (int?)null
                        });
                        stagedAttachments.Add((existingAttachment, null, Path.Combine(_attachmentSettings.UploadPath, existingAttachment.FilePath), null));
                        Console.WriteLine($"Updated attachment: ID={existingAttachment.Id}, fileName={existingAttachment.FileName}");
                    }
                    else
                    {
                        entity = new SolutionAttachment
                        {
                            FileName = guidFileName,
                            FilePath = filePath,
                            Caption = caption,
                            IsInternal = isInternal,
                            SolutionId = solution.Id
                        };
                        entity.UpdateAudit(userId);
                        _unitOfWork.SolutionAttachment.Add(entity);
                        await _unitOfWork.SaveAsync(); // Save to get ID

                        string sourcePath = null;
                        IFormFile uploadedFile = files?.FirstOrDefault(f => f.FileName == originalFileName);
                        if (uploadedFile == null && fromParent && parentAttachmentId.HasValue)
                        {
                            var categoryAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == parentAttachmentId.Value && a.CategoryId == solution.CategoryId && !a.IsDeleted);
                            sourcePath = categoryAttachment != null ? Path.Combine(_attachmentSettings.UploadPath, categoryAttachment.FilePath) : null;
                        }

                        attachments.Add(new
                        {
                            id = entity.Id,
                            fileName = guidFileName,
                            filePath,
                            caption,
                            isInternal,
                            originalFileName,
                            fromParent,
                            parentAttachmentId
                        });
                        stagedAttachments.Add((entity, sourcePath, Path.Combine(_attachmentSettings.UploadPath, filePath), uploadedFile));
                        Console.WriteLine($"Added new attachment: ID={entity.Id}, fileName={guidFileName}, filePath={filePath}");
                    }
                }

                // Soft delete attachments not in stagedAttachmentData
                foreach (var existing in existingAttachments)
                {
                    if (!stagedAttachmentData.Any(att => Convert.ToInt32(att["id"]) == existing.Id && !(att.ContainsKey("isDeleted") && bool.Parse(att["isDeleted"].ToString()))))
                    {
                        Console.WriteLine($"Marking attachment ID {existing.Id} for soft deletion.");
                        _unitOfWork.SolutionAttachment.SoftDelete(existing, userId);
                    }
                }
            }

            return attachments;
        }

        private async Task<List<object>> ProcessReferencesAsync(Solution solution, string? ReferenceData, string userId)
        {
            var savedReferences = new List<object>();
            if (string.IsNullOrEmpty(ReferenceData))
            {
                Console.WriteLine("ReferenceData is empty or null");
                return savedReferences;
            }

            Console.WriteLine($"Received ReferenceData: {ReferenceData}");
            try
            {
                var existingReferences = (await _unitOfWork.SolutionReference.GetAllAsync(r => r.SolutionId == solution.Id && !r.IsDeleted)).ToList();
                var references = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(ReferenceData);
                Console.WriteLine($"Deserialized {references.Count} references");

                foreach (var refDict in references)
                {
                    bool isDeleted = refDict.ContainsKey("isDeleted") && bool.Parse(refDict["isDeleted"].ToString());
                    if (isDeleted)
                    {
                        int refId = refDict.ContainsKey("id") ? Convert.ToInt32(refDict["id"]) : 0;
                        Console.WriteLine($"Skipping reference ID {refId} marked as deleted.");
                        continue;
                    }

                    int id = refDict.ContainsKey("id") ? Convert.ToInt32(refDict["id"]) : 0;
                    string url = refDict["url"]?.ToString();
                    string description = refDict.ContainsKey("description") ? refDict["description"]?.ToString() : null;
                    bool isInternal = refDict.ContainsKey("isInternal") && bool.Parse(refDict["isInternal"].ToString());
                    string openOption = refDict.ContainsKey("openOption") ? refDict["openOption"]?.ToString() : "_self";

                    if (string.IsNullOrEmpty(url))
                    {
                        Console.WriteLine("Skipping reference with empty URL.");
                        continue;
                    }

                    if (id > 0)
                    {
                        var existing = existingReferences.FirstOrDefault(r => r.Id == id);
                        if (existing != null)
                        {
                            existing.Url = url;
                            existing.Description = description;
                            existing.IsInternal = isInternal;
                            existing.OpenOption = openOption;
                            existing.UpdateAudit(userId);
                            _unitOfWork.SolutionReference.Update(existing);
                            savedReferences.Add(new
                            {
                                id = existing.Id,
                                url,
                                description,
                                isInternal,
                                openOption
                            });
                            Console.WriteLine($"Updated reference: ID={existing.Id}, url={url}, description={description}");
                        }
                        else
                        {
                            Console.WriteLine($"Reference ID {id} not found in existing references");
                        }
                    }
                    else
                    {
                        var newReference = new SolutionReference
                        {
                            Url = url,
                            Description = description,
                            IsInternal = isInternal,
                            OpenOption = openOption,
                            SolutionId = solution.Id
                        };
                        newReference.UpdateAudit(userId);
                        _unitOfWork.SolutionReference.Add(newReference);
                        await _unitOfWork.SaveAsync(); // Save immediately to get ID
                        savedReferences.Add(new
                        {
                            id = newReference.Id,
                            url,
                            description,
                            isInternal,
                            openOption
                        });
                        Console.WriteLine($"Added new reference: ID={newReference.Id}, url={url}, description={description}");
                    }
                }

                // Soft delete references not in the submitted list
                foreach (var existing in existingReferences)
                {
                    if (!references.Any(r => Convert.ToInt32(r["id"]) == existing.Id && !(r.ContainsKey("isDeleted") && bool.Parse(r["isDeleted"].ToString()))))
                    {
                        Console.WriteLine($"Marking reference ID {existing.Id} for soft deletion.");
                        _unitOfWork.SolutionReference.SoftDelete(existing, userId);
                    }
                }

                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Saved {savedReferences.Count} references for solution {solution.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing references: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }

            return savedReferences;
        }

        private async Task<string> GenerateDocId(Solution solution)
        {
            string year = DateTime.UtcNow.Year.ToString();
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == solution.ProductId && !p.IsDeleted);
            if (product == null)
            {
                throw new ArgumentException($"ProductId {solution.ProductId} not found.");
            }
            string productCode = product.Code;

            string subCategoryCode = "00";
            if (solution.SubCategoryId.HasValue)
            {
                var subCategory = await _unitOfWork.SubCategory.GetFirstOrDefaultAsync(sc => sc.Id == solution.SubCategoryId.Value && !sc.IsDeleted);
                if (subCategory == null)
                {
                    throw new ArgumentException($"SubCategoryId {solution.SubCategoryId} not found.");
                }
                subCategoryCode = subCategory.Code;
            }

            var existingSolutions = await _unitOfWork.Solution.GetAllAsync(s =>
                s.ProductId == solution.ProductId &&
                s.SubCategoryId == solution.SubCategoryId &&
                s.CreatedDate.Year == DateTime.UtcNow.Year &&
                s.IsDeleted == false &&
                s.DocId != null);

            int solutionNumber = existingSolutions.Count() + 1;
            string solutionNumberStr = solutionNumber.ToString("D2");

            return $"{year}{productCode}{subCategoryCode}.{solutionNumberStr}";
        }

        private async Task PopulateViewModel(SolutionViewModel model)
        {
            model.Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList();
            model.Products = (await _unitOfWork.Product.GetAllAsync()).ToList();
            model.SubCategories = (await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == model.ProductId)).ToList();
            model.Attachments = model.Attachments ?? new List<SolutionAttachment>();
            model.References = model.References ?? new List<SolutionReference>();
            ViewData["HtmlContent"] = model.HtmlContent ?? "";
        }

        private string ConvertHtmlToPlainText(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent)) return string.Empty;

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            var text = htmlDoc.DocumentNode.InnerText;
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            Console.WriteLine($"DownloadAttachment called: attachmentId={attachmentId}");
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized: No user found.");
                return Unauthorized();
            }

            var attachment = await _unitOfWork.SolutionAttachment.GetFirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
            if (attachment == null)
            {
                Console.WriteLine($"Attachment not found: Id={attachmentId}");
                return NotFound();
            }

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath).Replace('/', Path.DirectorySeparatorChar);
            Console.WriteLine($"Computed fullPath: {fullPath}");
            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine($"File not found: {fullPath}");
                return NotFound();
            }

            try
            {
                string mimeType = "application/octet-stream";
                string contentDisposition = "inline"; // Allow opening in browser
                string ext = Path.GetExtension(attachment.FileName)?.ToLowerInvariant();
                if (ext != null)
                {
                    mimeType = ext switch
                    {
                        ".pdf" => "application/pdf",
                        ".png" => "image/png",
                        ".jpg" => "image/jpeg",
                        ".jpeg" => "image/jpeg",
                        ".txt" => "text/plain",
                        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        _ => "application/octet-stream"
                    };
                    contentDisposition = ext switch
                    {
                        ".pdf" => "inline",
                        ".png" => "inline",
                        ".jpg" => "inline",
                        ".jpeg" => "inline",
                        ".txt" => "inline",
                        _ => "attachment"
                    };
                }

                Console.WriteLine($"Opening file: {fullPath}, Size: {new FileInfo(fullPath).Length} bytes, MIME: {mimeType}, Disposition: {contentDisposition}");
                var fileStream = System.IO.File.OpenRead(fullPath);
                Response.Headers.Add("Content-Disposition", $"{contentDisposition}; filename=\"{attachment.FileName}\"");
                return File(fileStream, mimeType);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO Error accessing file {fullPath}: {ex.Message}");
                return StatusCode(500, $"Error reading file: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied for file {fullPath}: {ex.Message}");
                return StatusCode(403, $"Access denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error for file {fullPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCategoryAttachment(int attachmentId)
        {
            Console.WriteLine($"DownloadCategoryAttachment called: attachmentId={attachmentId}");
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("Unauthorized: No user found.");
                return Unauthorized();
            }

            var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
            if (attachment == null)
            {
                Console.WriteLine($"Attachment not found: Id={attachmentId}");
                return NotFound();
            }

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath);
            Console.WriteLine($"Computed fullPath: {fullPath}");
            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine($"File not found: {fullPath}");
                return NotFound();
            }

            try
            {
                Console.WriteLine($"Opening file: {fullPath}, Size: {new FileInfo(fullPath).Length} bytes");
                var fileStream = System.IO.File.OpenRead(fullPath);
                var mimeType = attachment.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? "application/pdf"
                    : "application/octet-stream";
                Console.WriteLine($"Returning File result for {attachment.FileName} with MIME {mimeType}");
                return this.File(fileStream, mimeType, attachment.FileName);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO Error accessing file {fullPath}: {ex.Message}");
                return StatusCode(500, $"Error reading file: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied for file {fullPath}: {ex.Message}");
                return StatusCode(403, $"Access denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error for file {fullPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult TestFile()
        {
            return this.File(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test")), "text/plain", "test.txt");
        }
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted,
                includeProperties: "Attachments,References,Category,Product,SubCategory");
            if (solution == null)
            {
                TempData["error"] = "Solution not found.";
                return RedirectToAction("Approvals");
            }

            var model = new SolutionViewModel
            {
                Id = solution.Id,
                Title = solution.Title,
                ProductId = solution.ProductId,
                SubCategoryId = solution.SubCategoryId,
                CategoryId = solution.CategoryId,
                IssueDescription = solution.IssueDescription,
                HtmlContent = solution.HtmlContent,
                Feedback = solution.Feedback,
                Attachments = solution.Attachments?.Where(a => !a.IsDeleted).ToList(),
                References = solution.References?.Where(r => !r.IsDeleted).ToList(),
                Products = (await _unitOfWork.Product.GetAllAsync()).ToList(),
                Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList(),
                SubCategories = (await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == solution.ProductId)).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, string feedback, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["error"] = "Unauthorized access.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (string.IsNullOrEmpty(status))
            {
                TempData["error"] = "Please select a status.";
                return await ReloadReviewView(id, feedback);
            }

            var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (solution == null)
            {
                TempData["error"] = "Solution not found.";
                return RedirectToAction("Approvals", "Solution", new { area = "Admin" });
            }

            solution.Status = status;
            solution.Feedback = feedback;
            solution.UpdateAudit(user.Id);

            if (status == "Approved")
            {
                solution.ApprovedById = user.Id;
                solution.PublishedDate = DateTime.UtcNow;
                solution.DocId = await GenerateDocId(solution); // DocId generated here

                // Update the associated issue's status to "Published"
                var issue = await _unitOfWork.Issue.GetFirstOrDefaultAsync(i => i.Description == solution.IssueDescription && !i.IsDeleted);
                if (issue != null)
                {
                    issue.Status = "Published";
                    issue.UpdateAudit(user.Id);
                    _unitOfWork.Issue.Update(issue);
                }
                else
                {
                    Console.WriteLine($"No issue found with Description matching Solution.IssueDescription: {solution.IssueDescription}");
                }
            }
            else
            {
                solution.ApprovedById = null;
                solution.PublishedDate = null;
                solution.DocId = null; // Ensure DocId is cleared if not approved
            }

            try
            {
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Review saved successfully!";
                return RedirectToAction(status == "Approved" ? "MyApprovals" : "Approvals", "Solution", new { area = "Admin" });
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while saving the review: " + ex.Message;
                Console.WriteLine($"Error saving review: {ex}");
                return await ReloadReviewView(id, feedback);
            }
        }
        // Helper method to reload the Review view
        private async Task<IActionResult> ReloadReviewView(int id, string feedback)
        {
            var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted,
                includeProperties: "Attachments,References,Category,Product,SubCategory");
            if (solution == null)
            {
                TempData["error"] = "Solution not found.";
                return RedirectToAction("Approvals", "Solution", new { area = "Admin" });
            }

            var model = new SolutionViewModel
            {
                Id = solution.Id,
                Title = solution.Title,
                ProductId = solution.ProductId,
                SubCategoryId = solution.SubCategoryId,
                CategoryId = solution.CategoryId,
                IssueDescription = solution.IssueDescription,
                HtmlContent = solution.HtmlContent,
                Feedback = feedback,
                Attachments = solution.Attachments?.Where(a => !a.IsDeleted).ToList(),
                References = solution.References?.Where(r => !r.IsDeleted).ToList(),
                Products = (await _unitOfWork.Product.GetAllAsync()).ToList(),
                Categories = (await _unitOfWork.Category.GetAllAsync(c => !c.IsDeleted)).ToList(),
                SubCategories = (await _unitOfWork.SubCategory.GetAllAsync(s => s.ProductId == solution.ProductId)).ToList()
            };

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> MyApprovals()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var solutions = await _unitOfWork.Solution.GetAllAsync(
                s => !s.IsDeleted && s.Status == "Approved" && s.ApprovedById == user.Id,
                includeProperties: "Category,Product,SubCategory,Author"
            );

            return View(solutions);
        }
        [HttpPost]
        [Route("/Admin/Solution/ReleaseReview")]
        public async Task<IActionResult> ReleaseReview(int id, string status, string feedback)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, error = "Unauthorized access. Please log in." });
                }

                var solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
                if (solution == null)
                {
                    return Json(new { success = false, error = "Solution not found." });
                }

                // Ensure status is "Submitted"
                if (status != "Submitted")
                {
                    return Json(new { success = false, error = "Invalid status. Status must be 'Submitted' for release." });
                }

                solution.Status = status;
                solution.Feedback = feedback;
                solution.UpdateAudit(user.Id);

                await _unitOfWork.SaveAsync();

                return Json(new { success = true, message = "Review released successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReleaseReview: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, error = $"Error releasing review: {ex.Message}" });
            }
        }
        [HttpGet]
        //[Route("Admin/Solution/DownloadAttachment/{id?}")]
        public async Task<IActionResult> DownloadAttachmentaftercreate(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["error"] = "Please log in to download attachments.";
                Console.WriteLine("DownloadAttachment: Unauthorized access attempt.");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (id <= 0)
            {
                TempData["error"] = "Invalid attachment ID.";
                Console.WriteLine($"DownloadAttachment: Invalid ID {id}.");
                return NotFound();
            }

            var attachment = await _unitOfWork.SolutionAttachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (attachment == null)
            {
                TempData["error"] = $"Attachment with ID {id} not found.";
                Console.WriteLine($"DownloadAttachment: Attachment ID {id} not found or deleted.");
                return NotFound();
            }

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath).Replace('/', Path.DirectorySeparatorChar);
            Console.WriteLine($"DownloadAttachment: Attempting to access file at {fullPath} for ID {id}");

            if (!System.IO.File.Exists(fullPath))
            {
                TempData["error"] = $"Attachment file is missing on the server (ID: {id}). Please re-upload the file.";
                Console.WriteLine($"DownloadAttachment: File not found at {fullPath} for ID {id}");
                return NotFound();
            }

            // Determine MIME type
            string mimeType = "application/octet-stream";
            string ext = Path.GetExtension(attachment.FileName)?.ToLowerInvariant();
            if (ext != null)
            {
                mimeType = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".txt" => "text/plain",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "application/octet-stream"
                };
            }

            try
            {
                var fileStream = System.IO.File.OpenRead(fullPath);
                Console.WriteLine($"DownloadAttachment: Serving file {attachment.FileName} (ID: {id})");
                return File(fileStream, mimeType, attachment.FileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error accessing the attachment file.";
                Console.WriteLine($"DownloadAttachment: Error opening file {fullPath} for ID {id}: {ex.Message}");
                return StatusCode(500, "Error accessing the file.");
            }
        }

        [HttpGet]
        [Route("Admin/Solution/DownloadAttachmentForReview/{id?}")]
        public async Task<IActionResult> DownloadAttachmentForReview(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("DownloadAttachmentForReview: Unauthorized access attempt.");
                return Unauthorized();
            }

            var attachment = await _unitOfWork.SolutionAttachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (attachment == null)
            {
                Console.WriteLine($"DownloadAttachmentForReview: Attachment ID {id} not found or deleted.");
                return NotFound();
            }

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath).Replace('/', Path.DirectorySeparatorChar);
            Console.WriteLine($"DownloadAttachmentForReview: Attempting to access file at {fullPath} for ID {id}");

            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine($"DownloadAttachmentForReview: File not found at {fullPath} for ID {id}");
                return NotFound();
            }

            try
            {
                // Determine MIME type and Content-Disposition
                string mimeType = "application/octet-stream";
                string contentDisposition = "attachment"; // Default to download
                string ext = Path.GetExtension(attachment.FileName)?.ToLowerInvariant();

                if (ext != null)
                {
                    switch (ext)
                    {
                        case ".pdf":
                            mimeType = "application/pdf";
                            contentDisposition = "inline"; // Allow opening in browser
                            break;
                        case ".png":
                            mimeType = "image/png";
                            contentDisposition = "inline";
                            break;
                        case ".jpg":
                        case ".jpeg":
                            mimeType = "image/jpeg";
                            contentDisposition = "inline";
                            break;
                        case ".txt":
                            mimeType = "text/plain";
                            contentDisposition = "inline";
                            break;
                        case ".docx":
                            mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                            contentDisposition = "attachment"; // Force download for non-viewable types
                            break;
                        default:
                            mimeType = "application/octet-stream";
                            contentDisposition = "attachment";
                            break;
                    }
                }

                var fileStream = System.IO.File.OpenRead(fullPath);
                Console.WriteLine($"DownloadAttachmentForReview: Serving file {attachment.FileName} (ID: {id}), MIME: {mimeType}, Disposition: {contentDisposition}");
                Response.Headers.Add("Content-Disposition", $"{contentDisposition}; filename=\"{attachment.FileName}\"");
                return File(fileStream, mimeType);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"DownloadAttachmentForReview: IO Error accessing file {fullPath} for ID {id}: {ex.Message}");
                return StatusCode(500, "Error reading the attachment file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DownloadAttachmentForReview: Unexpected error for file {fullPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> OpenAttachment(int attachmentId)
        {
            Console.WriteLine($"OpenAttachment called: attachmentId={attachmentId}");
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("OpenAttachment: Unauthorized access attempt.");
                return Unauthorized();
            }

            var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
            if (attachment == null)
            {
                Console.WriteLine($"OpenAttachment: Attachment ID {attachmentId} not found or deleted.");
                return NotFound();
            }

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath).Replace('/', Path.DirectorySeparatorChar);
            Console.WriteLine($"OpenAttachment: Attempting to access file at {fullPath} for ID {attachmentId}");

            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine($"OpenAttachment: File not found at {fullPath} for ID {attachmentId}");
                return NotFound();
            }

            try
            {
                string mimeType = "application/octet-stream";
                string contentDisposition = "inline"; // Default to inline for opening in browser
                string ext = Path.GetExtension(attachment.FileName)?.ToLowerInvariant();

                if (ext != null)
                {
                    switch (ext)
                    {
                        case ".pdf":
                            mimeType = "application/pdf";
                            break;
                        case ".png":
                            mimeType = "image/png";
                            break;
                        case ".jpg":
                        case ".jpeg":
                            mimeType = "image/jpeg";
                            break;
                        case ".txt":
                            mimeType = "text/plain";
                            break;
                        case ".docx":
                            mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                            contentDisposition = "attachment"; // Force download for non-viewable types
                            break;
                        default:
                            contentDisposition = "attachment";
                            break;
                    }
                }

                var fileStream = System.IO.File.OpenRead(fullPath);
                Console.WriteLine($"OpenAttachment: Serving file {attachment.FileName} (ID: {attachmentId}), MIME: {mimeType}, Disposition: {contentDisposition}");
                Response.Headers.Add("Content-Disposition", $"{contentDisposition}; filename=\"{attachment.FileName}\"");
                Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                return File(fileStream, mimeType);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"OpenAttachment: IO Error accessing file {fullPath}: {ex.Message}");
                return StatusCode(500, "Error reading the attachment file.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"OpenAttachment: Access denied for file {fullPath}: {ex.Message}");
                return StatusCode(403, "Access denied.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAttachment: Unexpected error for file {fullPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }

}