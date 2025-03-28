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
                Console.WriteLine($"Product: {product.ProductName}, Subcategories: {string.Join(", ", product.SubCategories.Select(s => s.Name))}");
            }
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var subCategoryNames = ParseSubCategoryNames(model.SubCategoryNames);

                var product = new Product
                {
                    ProductName = model.ProductName,
                    Description = model.Description,
                    SubCategories = new List<SubCategory>()
                };

                foreach (var name in subCategoryNames)
                {
                    var subCategory = new SubCategory { Name = name, Product = product };
                    product.SubCategories.Add(subCategory);
                }

                await _unitOfWork.Product.AddAsync(product);
                await _unitOfWork.SaveAsync();

                var savedProduct = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    p => p.Id == product.Id, includeProperties: "SubCategories");
                Console.WriteLine($"After Create - Subcategories: {string.Join(", ", savedProduct.SubCategories.Select(s => s.Name))}");
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

                product.ProductName = model.ProductName;
                product.Description = model.Description;

                var newSubcategories = ParseSubCategoryNames(model.SubCategoryNames);
                Console.WriteLine($"New Subcategories: {string.Join(", ", newSubcategories)}");

                product.SubCategories.Clear();

                foreach (var name in newSubcategories)
                {
                    var subCategory = new SubCategory { Name = name, Product = product };
                    product.SubCategories.Add(subCategory);
                }

                _unitOfWork.Product.Update(product);
                await _unitOfWork.SaveAsync();

                var updatedProduct = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    p => p.Id == id, includeProperties: "SubCategories");
                Console.WriteLine($"After Save - Subcategories: {string.Join(", ", updatedProduct.SubCategories.Select(s => s.Name))}");
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
    }
}