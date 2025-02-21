using CormSquareSupportHub.Data;
using CormSquareSupportHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CormSquareSupportHub.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CategoryController(ApplicationDbContext db)
        {
           _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .Where(c => c.ParentCategoryId == null)  // Fetch only parent categories
                .Include(c => c.SubCategories)  // Include subcategories
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}
