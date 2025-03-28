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

        public SolutionController(IUnitOfWork unitOfWork, UserManager<ExternalUser> userManager,
            IWebHostEnvironment webHostEnvironment, IOptions<AttachmentSettings> attachmentSettings)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _attachmentSettings = attachmentSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? issueId)
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

            if (issueId.HasValue)
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

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SolutionViewModel model, List<IFormFile>? files,
            string? ReferenceData, string? AttachmentData, string submitAction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                await PopulateViewModel(model);
                return View(model);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var solution = new Solution
                {
                    Title = model.Title,
                    CategoryId = model.CategoryId,
                    ProductId = model.ProductId,
                    SubCategoryId = model.SubCategoryId,
                    AuthorId = user.Id, // Kept as string since it’s from ExternalUser
                    Contributors = model.Contributors,
                    HtmlContent = model.HtmlContent,
                    PlainTextContent = ConvertHtmlToPlainText(model.HtmlContent),
                    IssueDescription = model.IssueDescription,
                    Status = submitAction == "Save" ? "Draft" : "Submitted"
                };

                if (submitAction == "Submit")
                {
                    solution.DocId = await GenerateDocId(solution);
                }

                solution.UpdateAudit(int.Parse(user.Id)); // Convert string Id to int for AuditableEntity
                _unitOfWork.Solution.Add(solution);
                await _unitOfWork.SaveAsync();

                if (!string.IsNullOrEmpty(AttachmentData) || (files?.Count > 0))
                {
                    await ProcessAttachmentsAsync(solution, files, AttachmentData, int.Parse(user.Id));
                }
                if (!string.IsNullOrEmpty(ReferenceData))
                {
                    var references = JsonConvert.DeserializeObject<List<SolutionReference>>(ReferenceData);
                    await SaveReferences(references, solution.Id, int.Parse(user.Id));
                }

                await _unitOfWork.CommitTransactionAsync();
                TempData["success"] = submitAction == "Save" ? "Solution saved as draft!" : "Solution submitted for review!";
                return submitAction == "Save" ? RedirectToAction("Create", new { id = solution.Id }) : RedirectToAction("Approvals");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                ModelState.AddModelError("", $"Error: {ex.Message}");
                await PopulateViewModel(model);
                return View(model);
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
        public async Task<IActionResult> MySolutions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var solutions = await _unitOfWork.Solution.GetAllAsync(s => s.AuthorId == user.Id && !s.IsDeleted,
                includeProperties: "Category,Product,SubCategory");
            return View(solutions);
        }

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

        private async Task ProcessAttachmentsAsync(Solution solution, List<IFormFile>? files, string? AttachmentData, int userId)
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

        private async Task SaveReferences(List<SolutionReference> references, int solutionId, int userId)
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