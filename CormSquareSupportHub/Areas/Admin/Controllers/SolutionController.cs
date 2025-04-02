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

        // GET: List Solutions
        public async Task<IActionResult> MySolutions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var solutions = await _unitOfWork.Solution.GetAllAsync(s => s.AuthorId == user.Id && !s.IsDeleted,
                includeProperties: "Category,Product,SubCategory");
            return View(solutions);
        }

        // GET: Upsert (Create/Edit) Solution
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

            if (solutionId.HasValue) // Editing an existing solution
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
            }
            else if (issueId.HasValue) // Creating from an issue
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

        // POST: Upsert (Create/Edit) Solution
        [HttpPost]
        public async Task<IActionResult> Upsert(SolutionViewModel model, List<IFormFile>? files,
            string? ReferenceData, string? AttachmentData, string submitAction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("User not authenticated in Upsert");
                return Unauthorized();
            }

            // Log incoming data
            Console.WriteLine($"POST Upsert called. Id: {model.Id}, Title: {model.Title}, ProductId: {model.ProductId}, CategoryId: {model.CategoryId}, HtmlContent: {(string.IsNullOrEmpty(model.HtmlContent) ? "null" : "present")}");
            Console.WriteLine($"SubCategoryId: {model.SubCategoryId}, Contributors: {model.Contributors}, IssueDescription: {model.IssueDescription}");
            Console.WriteLine($"AttachmentData: {AttachmentData}, ReferenceData: {ReferenceData}, FilesCount: {files?.Count ?? 0}, SubmitAction: {submitAction}");

            // Clear ModelState for fields not submitted or set server-side
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

            // Log ModelState details
            Console.WriteLine("ModelState details before validation (Upsert):");
            foreach (var key in ModelState.Keys)
            {
                var value = ModelState[key];
                Console.WriteLine($"Key: {key}, Value: {value.RawValue ?? "null"}, Errors: {string.Join(", ", value.Errors.Select(e => e.ErrorMessage))}");
            }

            // Manual validation
            if (string.IsNullOrEmpty(model.Title))
                ModelState.AddModelError("Title", "Title is required.");
            if (model.ProductId <= 0)
                ModelState.AddModelError("ProductId", "Product is required.");
            if (model.CategoryId <= 0)
                ModelState.AddModelError("CategoryId", "Category is required.");
            if (string.IsNullOrEmpty(model.HtmlContent))
                ModelState.AddModelError("HtmlContent", "Content is required.");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error in Upsert: {error.ErrorMessage}");
                }
                await PopulateViewModel(model);
                return View(model);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Solution solution;
                string userId = user.Id;
                if (model.Id > 0) // Edit
                {
                    solution = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.Id == model.Id && !s.IsDeleted,
                        includeProperties: "Attachments,References");
                    if (solution == null)
                    {
                        Console.WriteLine($"Solution with ID {model.Id} not found or already deleted");
                        return NotFound();
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

                if (submitAction == "Submit" && string.IsNullOrEmpty(solution.DocId))
                {
                    solution.DocId = await GenerateDocId(solution);
                }

                await _unitOfWork.SaveAsync();
                Console.WriteLine($"Solution {(model.Id > 0 ? "updated" : "added")} with ID: {solution.Id}");

                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Count > 0))
                {
                    await ProcessAttachmentsAsync(solution, files, AttachmentData, userId);
                }
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = JsonConvert.DeserializeObject<List<SolutionReference>>(ReferenceData);
                    await SaveReferences(references, solution.Id, userId);
                }

                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = submitAction == "Save" ? "Solution saved as draft!" : "Solution submitted for review!";
                Console.WriteLine($"Solution {(submitAction == "Save" ? "saved" : "submitted")}, redirecting to {(submitAction == "Save" ? "MySolutions" : "Approvals")}");
                return submitAction == "Save" ? RedirectToAction("MySolutions") : RedirectToAction("Approvals");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine($"Error in Upsert: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Error: {ex.Message}");
                await PopulateViewModel(model);
                return View(model);
            }
        }

        // GET: Approvals
        [HttpGet]
        public async Task<IActionResult> Approvals()
        {
            var solutions = await _unitOfWork.Solution.GetAllAsync(s => s.Status == "Submitted" && !s.IsDeleted,
                includeProperties: "Category,Product,SubCategory,Author");
            return View(solutions);
        }

        // GET: Delete Solution (Confirmation)
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

        // POST: Delete Solution (Soft Delete)
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

        // GET: Category Template for Dropdown
        [HttpGet]
        public async Task<IActionResult> GetCategoryTemplate(int categoryId)
        {
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted,
                includeProperties: "Attachments,References");
            if (category == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                htmlContent = category.HtmlContent,
                attachments = category.Attachments.Where(a => !a.IsDeleted).Select(a => new { a.Id, a.FileName, a.Caption, a.IsInternal }),
                references = category.References.Where(r => !r.IsDeleted).Select(r => new { r.Id, r.Url, r.Description, r.IsInternal, r.OpenOption })
            });
        }

        // DELETE Attachment (Soft Delete via AJAX)
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

        // DELETE Reference (Soft Delete via AJAX)
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

        // Private Methods
        private async Task ProcessAttachmentsAsync(Solution solution, List<IFormFile>? files, string? AttachmentData, string userId)
        {
            if (string.IsNullOrEmpty(AttachmentData) && (files == null || files.Count == 0)) return;

            string basePath = _attachmentSettings.UploadPath;
            string solutionPath = Path.Combine(basePath, "solutions", solution.Id.ToString());

            if (!Directory.Exists(solutionPath))
            {
                Directory.CreateDirectory(solutionPath);
            }

            var existingAttachments = (await _unitOfWork.SolutionAttachment.GetAllAsync(a => a.SolutionId == solution.Id && !a.IsDeleted)).ToList();
            var attachmentInfos = string.IsNullOrEmpty(AttachmentData)
                ? new List<SolutionAttachment>()
                : JsonConvert.DeserializeObject<List<SolutionAttachment>>(AttachmentData);

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
                        _unitOfWork.SolutionAttachment.Update(matchingAttachment);
                    }
                }
            }

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(solutionPath, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    var newAttachmentInfo = attachmentInfos.FirstOrDefault(ai => ai.Id == 0 && ai.FileName == file.FileName)
                        ?? new SolutionAttachment();
                    var newAttachment = new SolutionAttachment
                    {
                        FileName = fileName,
                        FilePath = Path.Combine("solutions", solution.Id.ToString(), fileName).Replace("\\", "/"),
                        Caption = newAttachmentInfo.Caption ?? "",
                        IsInternal = newAttachmentInfo.IsInternal,
                        SolutionId = solution.Id
                    };
                    newAttachment.UpdateAudit(userId);
                    _unitOfWork.SolutionAttachment.Add(newAttachment);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        private async Task SaveReferences(List<SolutionReference> references, int solutionId, string userId)
        {
            var existingReferences = (await _unitOfWork.SolutionReference.GetAllAsync(r => r.SolutionId == solutionId && !r.IsDeleted)).ToList();
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
                    }
                }
                else
                {
                    reference.SolutionId = solutionId;
                    reference.UpdateAudit(userId);
                    _unitOfWork.SolutionReference.Add(reference);
                }
            }
            await _unitOfWork.SaveAsync();
        }

        private async Task<string> GenerateDocId(Solution solution)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == solution.ProductId);
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == solution.CategoryId);
            var lastSolution = (await _unitOfWork.Solution.GetAllAsync(s => s.ProductId == solution.ProductId && s.CategoryId == solution.CategoryId && !s.IsDeleted))
                .OrderByDescending(s => s.Id).FirstOrDefault();
            int nextNumber = lastSolution != null ? int.Parse(lastSolution.DocId.Split('.')[0].Substring(product.ProductName.Length + category.Name.Length)) + 1 : 1;
            return $"{product.ProductName}{category.Name}{nextNumber}.1";
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
    }
}