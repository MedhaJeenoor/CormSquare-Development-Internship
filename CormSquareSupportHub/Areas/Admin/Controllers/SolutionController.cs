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
                includeProperties: "Category,Product,SubCategory,Attachments");
            return View(solutions);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? issueId, int? solutionId)
        {
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

                // Set ViewData["AttachmentLinks"] with GUID-based FileName
                ViewData["AttachmentLinks"] = model.Attachments.Select(a => new
                {
                    Id = a.Id,
                    FileName = a.FileName, // GUID-based (e.g., "c2d13bc7-7b59-4b26-85a9-aad41e728972.png")
                    Url = Url.Action("DownloadAttachment", "Solution", new { attachmentId = a.Id, area = "Admin" }),
                    IsInternal = a.IsInternal
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

            ModelState.Remove("Products");
            ModelState.Remove("Categories");
            ModelState.Remove("SubCategories");
            ModelState.Remove("Attachments");
            ModelState.Remove("References");
            ModelState.Remove("AuthorId");
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
            List<(SolutionAttachment Entity, string SourcePath, string DestPath)> stagedAttachments = null;

            if (submitAction == "Cancel")
            {
                Console.WriteLine("Cancel button clicked, no changes will be saved.");
                TempData["info"] = "Action cancelled. All changes were rolled back.";
                redirectUrl = model.Id > 0 ? Url.Action("MySolutions") : Url.Action("IssueList", "Issue");
                return Json(new { success = true, redirectTo = redirectUrl });
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewModel(model);
                Console.WriteLine($"ModelState invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return Json(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
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
                    solution.Status = submitAction == "Save" ? "Draft" : "Submitted"; // Keep "Submitted"
                    solution.UpdateAudit(userId);
                    if (submitAction == "Submit" && solution.DocId == null)
                    {
                        solution.DocId = await GenerateDocId(solution);
                    }
                    _unitOfWork.Solution.Update(solution);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Updated and saved solution: {solution.Id}");
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
                        Status = submitAction == "Save" ? "Draft" : "Submitted" // Keep "Submitted"
                    };
                    solution.UpdateAudit(userId);
                    if (submitAction == "Submit")
                    {
                        solution.DocId = await GenerateDocId(solution);
                    }
                    _unitOfWork.Solution.Add(solution);
                    await _unitOfWork.SaveAsync();
                    Console.WriteLine($"Created and saved new solution: {solution.Id}");
                }

                // Process Attachments
                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Count > 0))
                {
                    savedAttachments = await ProcessAttachmentsAsync(solution, files, AttachmentData, userId);
                    stagedAttachments = new List<(SolutionAttachment, string, string)>();
                    var existingAttachments = (await _unitOfWork.SolutionAttachment.GetAllAsync(a => a.SolutionId == solution.Id && !a.IsDeleted)).ToList();

                    foreach (var item in savedAttachments)
                    {
                        var guidFileName = (string)item.GetType().GetProperty("fileName").GetValue(item);
                        var filePath = (string)item.GetType().GetProperty("filePath").GetValue(item);
                        var source = (string)item.GetType().GetProperty("source")?.GetValue(item);
                        var existingAttachment = existingAttachments.FirstOrDefault(a => a.FilePath == filePath); // Check by FilePath
                        SolutionAttachment entity;

                        if (existingAttachment != null)
                        {
                            entity = existingAttachment; // Update existing
                            entity.Caption = (string)item.GetType().GetProperty("caption")?.GetValue(item);
                            entity.IsInternal = (bool)item.GetType().GetProperty("isInternal").GetValue(item);
                            entity.Source = source;
                            entity.UpdateAudit(userId);
                            _unitOfWork.SolutionAttachment.Update(entity);
                        }
                        else
                        {
                            entity = new SolutionAttachment
                            {
                                FileName = guidFileName,
                                FilePath = filePath,
                                Caption = (string)item.GetType().GetProperty("caption")?.GetValue(item),
                                IsInternal = (bool)item.GetType().GetProperty("isInternal").GetValue(item),
                                SolutionId = solution.Id,
                                Source = source
                            };
                            entity.UpdateAudit(userId);
                            _unitOfWork.SolutionAttachment.Add(entity);
                            existingAttachments.Add(entity);
                        }

                        // Stage for file copying with original filename for category lookup
                        string originalFileName = source == "CategoryTemplate" && AttachmentData != null
                            ? Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(AttachmentData)
                                .FirstOrDefault(a => a["fileName"].ToString().EndsWith(Path.GetExtension(guidFileName)))?["fileName"]?.ToString()
                            : null;
                        string sourcePath = source == "CategoryTemplate"
                            ? Path.Combine(_attachmentSettings.UploadPath, (await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.FileName == originalFileName && a.CategoryId == solution.CategoryId && !a.IsDeleted))?.FilePath ?? "")
                            : null;
                        string destPath = Path.Combine(_attachmentSettings.UploadPath, entity.FilePath);
                        stagedAttachments.Add((entity, sourcePath, destPath));
                    }
                }

                // Process References
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    savedReferences = await ProcessReferencesAsync(solution, ReferenceData, userId);
                }

                switch (submitAction)
                {
                    case "Save":
                        string solutionPath = Path.Combine(_attachmentSettings.UploadPath, "solutions", solution.Id.ToString());
                        if (!Directory.Exists(solutionPath)) Directory.CreateDirectory(solutionPath);

                        if (stagedAttachments != null && stagedAttachments.Any())
                        {
                            foreach (var (entity, sourcePath, destPath) in stagedAttachments)
                            {
                                if (entity.Source == "CategoryTemplate" && !string.IsNullOrEmpty(sourcePath) && System.IO.File.Exists(sourcePath))
                                {
                                    System.IO.File.Copy(sourcePath, destPath, overwrite: true);
                                    Console.WriteLine($"Copied category file: {sourcePath} to {destPath}");
                                }
                                else if (entity.Source == "Upload" && files != null)
                                {
                                    var file = files.FirstOrDefault(f => f.FileName.EndsWith(Path.GetExtension(entity.FileName)));
                                    if (file != null && !System.IO.File.Exists(destPath))
                                    {
                                        using (var fileStream = new FileStream(destPath, FileMode.Create))
                                        {
                                            await file.CopyToAsync(fileStream);
                                            Console.WriteLine($"Uploaded file: {entity.FileName} to {destPath}");
                                        }
                                    }
                                }
                            }
                        }

                        await _unitOfWork.SaveAsync();
                        await _unitOfWork.CommitTransactionAsync();
                        Console.WriteLine($"Committed transaction: Saved solution {solution.Id}, {savedAttachments.Count} attachments, {savedReferences.Count} references");
                        TempData["success"] = "Solution saved as draft!";
                        redirectUrl = Url.Action("MySolutions");
                        break;

                    case "Submit":
                        solutionPath = Path.Combine(_attachmentSettings.UploadPath, "solutions", solution.Id.ToString());
                        if (!Directory.Exists(solutionPath)) Directory.CreateDirectory(solutionPath);

                        if (stagedAttachments != null && stagedAttachments.Any())
                        {
                            foreach (var (entity, sourcePath, destPath) in stagedAttachments)
                            {
                                if (entity.Source == "CategoryTemplate" && !string.IsNullOrEmpty(sourcePath) && System.IO.File.Exists(sourcePath))
                                {
                                    System.IO.File.Copy(sourcePath, destPath, overwrite: true);
                                    Console.WriteLine($"Copied category file: {sourcePath} to {destPath}");
                                }
                                else if (entity.Source == "Upload" && files != null)
                                {
                                    var file = files.FirstOrDefault(f => f.FileName.EndsWith(Path.GetExtension(entity.FileName)));
                                    if (file != null && !System.IO.File.Exists(destPath))
                                    {
                                        using (var fileStream = new FileStream(destPath, FileMode.Create))
                                        {
                                            await file.CopyToAsync(fileStream);
                                            Console.WriteLine($"Uploaded file: {entity.FileName} to {destPath}");
                                        }
                                    }
                                }
                            }
                        }

                        await _unitOfWork.SaveAsync();
                        await _unitOfWork.CommitTransactionAsync();
                        Console.WriteLine($"Committed transaction: Submitted solution {solution.Id}, {savedAttachments.Count} attachments, {savedReferences.Count} references");
                        TempData["success"] = "Solution submitted for review!";
                        redirectUrl = Url.Action("Approvals");
                        break;

                    default:
                        await _unitOfWork.RollbackTransactionAsync();
                        Console.WriteLine("Unknown submitAction: " + submitAction);
                        return Json(new { success = false, message = "Invalid submit action." });
                }

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
            var solutions = await _unitOfWork.Solution.GetAllAsync(s => s.Status == "Submitted" && !s.IsDeleted,
                includeProperties: "Category,Product,SubCategory,Author");
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
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted,
                includeProperties: "Attachments,References");
            if (category == null) return Json(new { success = false });

            var attachmentLinks = category.Attachments
                .Where(a => !a.IsDeleted)
                .Select(a => new
                {
                    a.Id,
                    a.FileName,
                    a.Caption,
                    a.IsInternal,
                    Url = Url.Action("DownloadCategoryAttachment", "Solution", new { attachmentId = a.Id, area = "Admin" })
                });

            return Json(new
            {
                success = true,
                htmlContent = category.HtmlContent,
                attachments = attachmentLinks,
                references = category.References.Where(r => !r.IsDeleted).Select(r => new { r.Id, r.Url, r.Description, r.IsInternal, r.OpenOption })
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAttachment(int id)
        {
            try
            {
                var attachment = await _unitOfWork.SolutionAttachment.GetFirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (attachment == null)
                {
                    return Json(new { success = false, message = "Attachment not found." });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                _unitOfWork.SolutionAttachment.SoftDelete(attachment, user.Id);
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
            try
            {
                var reference = await _unitOfWork.SolutionReference.GetFirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
                if (reference == null)
                {
                    return Json(new { success = false, message = "Reference not found." });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                _unitOfWork.SolutionReference.SoftDelete(reference, user.Id);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = "Reference deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveReference: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error deleting reference: {ex.Message}" });
            }
        }

        private async Task<List<object>> ProcessAttachmentsAsync(Solution solution, List<IFormFile>? files, string? attachmentData, string userId)
        {
            var attachments = new List<object>();

            if (!string.IsNullOrEmpty(attachmentData))
            {
                var stagedAttachments = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(attachmentData);
                foreach (var att in stagedAttachments)
                {
                    string originalFileName = att["fileName"].ToString();
                    string guidFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
                    string filePath = Path.Combine("solutions", solution.Id.ToString(), guidFileName).Replace("\\", "/");
                    attachments.Add(new
                    {
                        fileName = guidFileName,
                        filePath,
                        caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null,
                        isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString()),
                        source = "CategoryTemplate"
                    });
                }
            }

            if (files?.Count > 0)
            {
                foreach (var file in files)
                {
                    string guidFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    string filePath = Path.Combine("solutions", solution.Id.ToString(), guidFileName).Replace("\\", "/");
                    if (!attachments.Any(a => a.GetType().GetProperty("filePath").GetValue(a).ToString() == filePath))
                    {
                        attachments.Add(new
                        {
                            fileName = guidFileName,
                            filePath,
                            caption = "",
                            isInternal = false,
                            source = "Upload"
                        });
                    }
                }
            }

            return attachments;
        }

        private async Task<List<object>> ProcessReferencesAsync(Solution solution, string? ReferenceData, string userId)
        {
            var savedReferences = new List<object>();
            if (string.IsNullOrEmpty(ReferenceData)) return savedReferences;

            var existingReferences = (await _unitOfWork.SolutionReference.GetAllAsync(r => r.SolutionId == solution.Id && !r.IsDeleted)).ToList();
            var references = JsonConvert.DeserializeObject<List<SolutionReference>>(ReferenceData);

            foreach (var reference in references)
            {
                if (reference.Id > 0)
                {
                    var existing = existingReferences.FirstOrDefault(r => r.Id == reference.Id);
                    if (existing != null)
                    {
                        existing.Url = reference.Url;
                        existing.Description = reference.Description;
                        existing.IsInternal = reference.IsInternal;
                        existing.OpenOption = reference.OpenOption;
                        existing.UpdateAudit(userId);
                        _unitOfWork.SolutionReference.Update(existing);
                        savedReferences.Add(new { id = existing.Id, url = existing.Url, description = existing.Description, isInternal = existing.IsInternal, openOption = existing.OpenOption });
                        Console.WriteLine($"Updated reference: {existing.Url}, ID: {existing.Id}");
                    }
                }
                else
                {
                    var newReference = new SolutionReference
                    {
                        Url = reference.Url,
                        Description = reference.Description,
                        IsInternal = reference.IsInternal,
                        OpenOption = reference.OpenOption,
                        SolutionId = solution.Id
                    };
                    newReference.UpdateAudit(userId);
                    _unitOfWork.SolutionReference.Add(newReference);
                    savedReferences.Add(new { id = newReference.Id, url = newReference.Url, description = newReference.Description, isInternal = newReference.IsInternal, openOption = newReference.OpenOption });
                    Console.WriteLine($"Staged new reference: {newReference.Url}");
                }
            }

            return savedReferences;
        }

        private async Task<string> GenerateDocId(Solution solution)
        {
            // Get the current year
            string year = DateTime.UtcNow.Year.ToString(); // e.g., "2025"

            // Fetch product and its code
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == solution.ProductId && !p.IsDeleted);
            if (product == null)
            {
                throw new ArgumentException($"ProductId {solution.ProductId} not found.");
            }
            string productCode = product.Code; // e.g., "01" for HDFC

            // Fetch subcategory and its code (default to "00" if none)
            string subCategoryCode = "00";
            if (solution.SubCategoryId.HasValue)
            {
                var subCategory = await _unitOfWork.SubCategory.GetFirstOrDefaultAsync(sc => sc.Id == solution.SubCategoryId.Value && !sc.IsDeleted);
                if (subCategory == null)
                {
                    throw new ArgumentException($"SubCategoryId {solution.SubCategoryId} not found.");
                }
                subCategoryCode = subCategory.Code; // e.g., "01" for Sub1
            }

            // Count existing solutions for this product and subcategory in the current year
            var existingSolutions = await _unitOfWork.Solution.GetAllAsync(s =>
                s.ProductId == solution.ProductId &&
                s.SubCategoryId == solution.SubCategoryId &&
                s.CreatedDate.Year == DateTime.UtcNow.Year &&
                s.IsDeleted == false &&
                s.DocId != null); // Only count submitted solutions with DocId

            int solutionNumber = existingSolutions.Count() + 1; // Next number
            string solutionNumberStr = solutionNumber.ToString("D2"); // Pads to 2 digits, e.g., "03"

            // Combine: YYYY + PP + SS + .NN
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var attachment = await _unitOfWork.SolutionAttachment.GetFirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
            if (attachment == null) return NotFound();

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            using var fileStream = System.IO.File.OpenRead(fullPath);
            return this.File(fileStream, "application/octet-stream", attachment.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCategoryAttachment(int attachmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var attachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
            if (attachment == null) return NotFound();

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            using var fileStream = System.IO.File.OpenRead(fullPath);
            return this.File(fileStream, "application/octet-stream", attachment.FileName);
        }

        [HttpGet]
        public IActionResult TestFile()
        {
            return this.File(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test")), "text/plain", "test.txt");
        }
    }
}