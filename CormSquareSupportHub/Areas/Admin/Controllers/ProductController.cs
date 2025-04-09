using Microsoft.AspNetCore.Mvc;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace CormSquareSupportHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await _unitOfWork.Product.GetAllAsync(includeProperties: "SubCategories");
            foreach (var product in products)
            {
                Console.WriteLine($"Product: {product.ProductName}, Code: {product.Code}, Subcategories: {string.Join(", ", product.SubCategories.Select(s => $"{s.Name} ({s.Code})"))}");
            }
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new ProductViewModel
            {
                Code = GetNextProductCode().Result // Suggest next available code
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var subCategoryNames = ParseSubCategoryNames(model.SubCategoryNames);

                // Check for duplicate product code
                if (await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Code == model.Code && !p.IsDeleted) != null)
                {
                    ModelState.AddModelError("Code", "This product code is already in use.");
                    return View(model);
                }

                var product = new Product
                {
                    ProductName = model.ProductName,
                    Description = model.Description,
                    Code = model.Code,
                    SubCategories = new List<SubCategory>()
                };

                // Auto-assign subcategory codes
                for (int i = 0; i < subCategoryNames.Count; i++)
                {
                    var subCategory = new SubCategory
                    {
                        Name = subCategoryNames[i],
                        Product = product,
                        Code = (i + 1).ToString("D2") // e.g., "01", "02", etc.
                    };
                    product.SubCategories.Add(subCategory);
                }

                await _unitOfWork.Product.AddAsync(product);
                await _unitOfWork.SaveAsync();

                var savedProduct = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    p => p.Id == product.Id, includeProperties: "SubCategories");
                Console.WriteLine($"After Create - Product: {savedProduct.ProductName}, Code: {savedProduct.Code}");
                Console.WriteLine($"After Create - Subcategories: {string.Join(", ", savedProduct.SubCategories.Select(s => $"{s.Name} ({s.Code})"))}");
                Console.WriteLine($"After Create - Subcategories ProductIds: {string.Join(", ", savedProduct.SubCategories.Select(s => s.ProductId))}");

                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == id, includeProperties: "SubCategories");
            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Code = product.Code,
                SubCategoryNames = product.SubCategories.Select(sc => sc.Name).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    p => p.Id == id, includeProperties: "SubCategories");

                if (product == null)
                {
                    return NotFound();
                }

                // Check for duplicate product code (excluding this product)
                if (await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Code == model.Code && p.Id != id && !p.IsDeleted) != null)
                {
                    ModelState.AddModelError("Code", "This product code is already in use by another product.");
                    return View(model);
                }

                // Update product details
                product.ProductName = model.ProductName;
                product.Description = model.Description;
                product.Code = model.Code;

                var newSubcategories = ParseSubCategoryNames(model.SubCategoryNames);
                Console.WriteLine($"New Subcategories: {string.Join(", ", newSubcategories)}");

                // Sync subcategories without deleting existing ones unnecessarily
                var existingSubCats = product.SubCategories.ToList();
                var newSubCatNames = newSubcategories.ToList();

                // Remove subcategories that are no longer in the list (only if not referenced)
                for (int i = existingSubCats.Count - 1; i >= 0; i--)
                {
                    var existingSubCat = existingSubCats[i];
                    if (!newSubCatNames.Contains(existingSubCat.Name))
                    {
                        // Check if this subcategory is referenced in Solutions
                        var isReferenced = await _unitOfWork.Solution.GetFirstOrDefaultAsync(s => s.SubCategoryId == existingSubCat.Id && !s.IsDeleted) != null;
                        if (!isReferenced)
                        {
                            product.SubCategories.Remove(existingSubCat);
                        }
                        else
                        {
                            // Optionally log or notify that it can't be removed
                            Console.WriteLine($"Cannot remove subcategory '{existingSubCat.Name}' (Code: {existingSubCat.Code}) as it is referenced in Solutions.");
                        }
                    }
                }

                // Add or update subcategories
                for (int i = 0; i < newSubCatNames.Count; i++)
                {
                    var subCatName = newSubCatNames[i];
                    var existingSubCat = product.SubCategories.FirstOrDefault(sc => sc.Name == subCatName);
                    if (existingSubCat == null)
                    {
                        // Add new subcategory
                        product.SubCategories.Add(new SubCategory
                        {
                            Name = subCatName,
                            Product = product,
                            Code = (i + 1).ToString("D2")
                        });
                    }
                    else
                    {
                        // Update code if order changed
                        existingSubCat.Code = (i + 1).ToString("D2");
                    }
                }

                _unitOfWork.Product.Update(product);
                await _unitOfWork.SaveAsync();

                var updatedProduct = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    p => p.Id == id, includeProperties: "SubCategories");
                Console.WriteLine($"After Save - Product: {updatedProduct.ProductName}, Code: {updatedProduct.Code}");
                Console.WriteLine($"After Save - Subcategories: {string.Join(", ", updatedProduct.SubCategories.Select(s => $"{s.Name} ({s.Code})"))}");
                Console.WriteLine($"After Save - Subcategories ProductIds: {string.Join(", ", updatedProduct.SubCategories.Select(s => s.ProductId))}");

                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == id, includeProperties: "SubCategories");
            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Code = product.Code,
                SubCategoryNames = product.SubCategories.Select(sc => sc.Name).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            _unitOfWork.Product.Remove(product);
            await _unitOfWork.SaveAsync();

            return RedirectToAction("Index");
        }

        private List<string> ParseSubCategoryNames(List<string> subCategoryNames)
        {
            if (subCategoryNames == null)
                return new List<string>();

            if (subCategoryNames.Count == 1)
            {
                string firstItem = subCategoryNames[0];
                if (firstItem.StartsWith("["))
                {
                    try
                    {
                        return JsonSerializer.Deserialize<List<string>>(firstItem) ?? new List<string>();
                    }
                    catch
                    {
                        return new List<string>();
                    }
                }
                else if (firstItem.Contains(","))
                {
                    return firstItem.Split(',')
                        .Select(name => name.Trim())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();
                }
            }

            return subCategoryNames.Select(name => name.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        private async Task<string> GetNextProductCode()
        {
            var products = await _unitOfWork.Product.GetAllAsync(p => !p.IsDeleted);
            var maxCode = products.Any() ? products.Max(p => int.Parse(p.Code ?? "00")) : 0;
            return (maxCode + 1).ToString("D2"); // e.g., "01" if empty, "03" if "02" exists
        }
    }
}