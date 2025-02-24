using CormSquareSupportHub.Data;
using CormSquareSupportHub.Models;
using CormSquareSupportHub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CormSquareSupportHub.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            var categories = _db.Categories.ToList();
            return View(new CategoryViewModel { Categories = categories });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid) //Ensure client-side validation works
            {
                model.Categories = await _db.Categories.ToListAsync();
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name,
                ParentCategoryId = model.ParentCategoryId == 0 ? null : model.ParentCategoryId,
                OptimalCreationTime = model.OptimalCreationTime,
                Description = model.Description,
                DisplayOrder = model.DisplayOrder,
                AllowAttachments = model.AllowAttachments,
                TemplateJson = model.TemplateJson
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            //Handle File Uploads
            if (model.Attachments != null && model.Attachments.Any())
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var file in model.Attachments)
                {
                    var filePath = Path.Combine(uploadsFolder, file.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _db.CategoryAttachments.Add(new CategoryAttachment
                    {
                        FileName = file.FileName,
                        FilePath = "/uploads/" + file.FileName,
                        CategoryId = category.Id
                    });
                }
                await _db.SaveChangesAsync();
            }

            //Save Reference Links
            if (!string.IsNullOrEmpty(model.ReferenceLinks))
            {
                var links = model.ReferenceLinks.Split(',').Select(l => l.Trim()).ToList();
                foreach (var link in links)
                {
                    _db.CategoryReferences.Add(new CategoryReference
                    {
                        Url = link,
                        CategoryId = category.Id
                    });
                }
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }



    }
}
