﻿using Microsoft.AspNetCore.Identity;
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
            // Disable caching and BFCache
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

                ViewData["AttachmentLinks"] = model.Attachments.Select(a => new
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    Url = Url.Action("DownloadAttachment", "Solution", new { attachmentId = a.Id, area = "Admin" }),
                    IsInternal = a.IsInternal
                }).ToList();

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
                Console.WriteLine("Cancel button clicked, no server-side changes.");
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
                    solution.Status = submitAction == "Save" ? "Draft" : "Submitted";
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
                        Status = submitAction == "Save" ? "Draft" : "Submitted"
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
                    if (!string.IsNullOrEmpty(AttachmentData))
                    {
                        savedAttachments = await ProcessAttachmentsAsync(solution, files, AttachmentData, userId);
                        stagedAttachments = new List<(SolutionAttachment, string, string)>();
                        var existingAttachments = (await _unitOfWork.SolutionAttachment.GetAllAsync(a => a.SolutionId == solution.Id && !a.IsDeleted)).ToList();

                        foreach (var item in savedAttachments)
                        {
                            var id = (int)item.GetType().GetProperty("id").GetValue(item);
                            var guidFileName = (string)item.GetType().GetProperty("fileName").GetValue(item);
                            var filePath = (string)item.GetType().GetProperty("filePath").GetValue(item);
                            var originalFileName = (string)item.GetType().GetProperty("originalFileName").GetValue(item);
                            var caption = (string)item.GetType().GetProperty("caption")?.GetValue(item);
                            var isInternal = (bool)item.GetType().GetProperty("isInternal").GetValue(item);

                            SolutionAttachment entity;
                            if (id > 0)
                            {
                                entity = existingAttachments.FirstOrDefault(a => a.Id == id);
                                if (entity != null)
                                {
                                    entity.Caption = caption;
                                    entity.IsInternal = isInternal;
                                    entity.UpdateAudit(userId);
                                    _unitOfWork.SolutionAttachment.Update(entity);
                                }
                                else
                                {
                                    continue;
                                }
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
                                existingAttachments.Add(entity);
                            }

                            string destPath = Path.Combine(_attachmentSettings.UploadPath, entity.FilePath);
                            string sourcePath = null;
                            var uploadedFile = files?.FirstOrDefault(f => f.FileName == originalFileName);
                            if (uploadedFile == null)
                            {
                                var categoryAttachment = await _unitOfWork.Attachment.GetFirstOrDefaultAsync(a => a.FileName == originalFileName && a.CategoryId == solution.CategoryId && !a.IsDeleted);
                                sourcePath = categoryAttachment != null ? Path.Combine(_attachmentSettings.UploadPath, categoryAttachment.FilePath) : null;
                            }
                            stagedAttachments.Add((entity, sourcePath, destPath));
                        }
                        await _unitOfWork.SaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attachment processing error: {ex.Message}");
                    await _unitOfWork.RollbackTransactionAsync();
                    return Json(new { success = false, message = $"Attachment processing failed: {ex.Message}" });
                }

                switch (submitAction)
                {
                    case "Save":
                    case "Submit":
                        string solutionPath = Path.Combine(_attachmentSettings.UploadPath, "solutions", solution.Id.ToString());
                        if (!Directory.Exists(solutionPath)) Directory.CreateDirectory(solutionPath);

                        if (stagedAttachments != null && stagedAttachments.Any())
                        {
                            foreach (var (entity, sourcePath, destPath) in stagedAttachments)
                            {
                                if (System.IO.File.Exists(destPath))
                                {
                                    Console.WriteLine($"File already exists: {destPath}, skipping copy.");
                                    continue;
                                }

                                if (sourcePath != null && System.IO.File.Exists(sourcePath))
                                {
                                    System.IO.File.Copy(sourcePath, destPath, overwrite: false);
                                    Console.WriteLine($"Copied category file: {sourcePath} to {destPath}");
                                }
                                else
                                {
                                    var file = files?.FirstOrDefault(f => f.FileName.EndsWith(Path.GetExtension(entity.FileName)));
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
                        }

                        await _unitOfWork.SaveAsync();
                        await _unitOfWork.CommitTransactionAsync();
                        Console.WriteLine($"Committed transaction: {submitAction} solution {solution.Id}, {savedAttachments.Count} attachments, {savedReferences.Count} references");
                        TempData["success"] = submitAction == "Save" ? "Solution saved as draft!" : "Solution submitted for review!";
                        redirectUrl = submitAction == "Save" ? Url.Action("MySolutions") : Url.Action("Approvals");
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
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);
            if (category == null) return Json(new { success = false, message = "Category not found." });

            var attachments = await _unitOfWork.Attachment.GetAllAsync(a => a.CategoryId == categoryId && !a.IsDeleted);
            var references = await _unitOfWork.Reference.GetAllAsync(r => r.CategoryId == categoryId && !r.IsDeleted);

            var response = new
            {
                success = true,
                htmlContent = category.HtmlContent,
                attachments = attachments.Select(a => new
                {
                    a.Id,
                    a.FileName,
                    a.Caption,
                    a.IsInternal,
                    Url = $"/Admin/Solution/DownloadCategoryAttachment?attachmentId={a.Id}",
                    a.FilePath
                }),
                references = references.Select(r => new
                {
                    r.Id,
                    r.Url,
                    r.Description,
                    r.IsInternal,
                    r.OpenOption
                })
            };

            Console.WriteLine($"GetCategoryTemplate for categoryId={categoryId}: {JsonConvert.SerializeObject(response.attachments, Formatting.Indented)}");
            return Json(response);
        }

        private async Task<List<object>> ProcessAttachmentsAsync(Solution solution, List<IFormFile>? files, string? attachmentData, string userId)
        {
            var attachments = new List<object>();
            var existingAttachments = (await _unitOfWork.SolutionAttachment.GetAllAsync(a => a.SolutionId == solution.Id && !a.IsDeleted)).ToList();

            if (!string.IsNullOrEmpty(attachmentData))
            {
                var stagedAttachments = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(attachmentData);
                foreach (var att in stagedAttachments)
                {
                    int attachmentId = att.ContainsKey("id") ? Convert.ToInt32(att["id"]) : 0;
                    string originalFileName = att["fileName"].ToString();
                    bool isDeleted = att.ContainsKey("isDeleted") && bool.Parse(att["isDeleted"].ToString());
                    if (isDeleted) continue; // Skip deleted attachments

                    string guidFileName = att.ContainsKey("guidFileName") && !string.IsNullOrEmpty(att["guidFileName"]?.ToString())
                        ? att["guidFileName"].ToString()
                        : $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
                    string filePath = Path.Combine("solutions", solution.Id.ToString(), guidFileName).Replace("\\", "/");
                    bool isInternal = att.ContainsKey("isInternal") && bool.Parse(att["isInternal"].ToString());
                    string caption = att.ContainsKey("caption") ? att["caption"]?.ToString() : null;

                    var existingAttachment = attachmentId > 0 ? existingAttachments.FirstOrDefault(a => a.Id == attachmentId) : null;

                    if (existingAttachment != null)
                    {
                        attachments.Add(new
                        {
                            id = existingAttachment.Id,
                            fileName = existingAttachment.FileName,
                            filePath = existingAttachment.FilePath,
                            caption,
                            isInternal,
                            originalFileName
                        });
                    }
                    else if (!existingAttachments.Any(a => a.FilePath == filePath) && !attachments.Any(a => (string)a.GetType().GetProperty("filePath").GetValue(a) == filePath))
                    {
                        attachments.Add(new
                        {
                            id = 0,
                            fileName = guidFileName,
                            filePath,
                            caption,
                            isInternal,
                            originalFileName
                        });
                    }
                }

                // Process deletions
                foreach (var existing in existingAttachments)
                {
                    if (!stagedAttachments.Any(att => Convert.ToInt32(att["id"]) == existing.Id))
                    {
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
                var references = JsonConvert.DeserializeObject<List<SolutionReference>>(ReferenceData);
                Console.WriteLine($"Deserialized {references.Count} references");

                foreach (var reference in references)
                {
                    if (reference.IsDeleted) continue; // Skip deleted references

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
                        else
                        {
                            Console.WriteLine($"Reference ID {reference.Id} not found in existing references");
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
                        savedReferences.Add(new { id = 0, url = newReference.Url, description = newReference.Description, isInternal = newReference.IsInternal, openOption = newReference.OpenOption });
                        Console.WriteLine($"Staged new reference: {newReference.Url}");
                    }
                }

                // Process deletions
                foreach (var existing in existingReferences)
                {
                    if (!references.Any(r => r.Id == existing.Id))
                    {
                        _unitOfWork.SolutionReference.SoftDelete(existing, userId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing or processing references: {ex.Message}\nStackTrace: {ex.StackTrace}");
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

            string fullPath = Path.Combine(_attachmentSettings.UploadPath, attachment.FilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine($"File not found: {fullPath}");
                return NotFound();
            }

            try
            {
                Console.WriteLine($"Opening file: {fullPath}, Size: {new FileInfo(fullPath).Length} bytes");
                var fileStream = System.IO.File.OpenRead(fullPath);
                Console.WriteLine($"Returning File result for {attachment.FileName}");
                return this.File(fileStream, "application/octet-stream", attachment.FileName);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO Error accessing file {fullPath}: {ex.Message}");
                return StatusCode(500, "Error reading file.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied for file {fullPath}: {ex.Message}");
                return StatusCode(403, "Access denied.");
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
    }
}