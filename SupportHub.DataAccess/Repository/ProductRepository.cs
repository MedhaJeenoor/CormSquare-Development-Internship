using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SupportHub.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Product product)
        {
            await _dbContext.Products.AddAsync(product);
            // EF Core will automatically handle the SubCategories due to the relationship
        }

        public void Update(Product entity)
        {
            // Retrieve the existing product with its subcategories
            var existingProduct = _dbContext.Products
                .Include(p => p.SubCategories)
                .FirstOrDefault(p => p.Id == entity.Id);

            if (existingProduct == null)
            {
                throw new Exception($"Product with ID {entity.Id} not found.");
            }

            // Update scalar properties
            _dbContext.Entry(existingProduct).CurrentValues.SetValues(entity);

            // Handle subcategories
            var existingSubCategories = existingProduct.SubCategories.ToList();
            var newSubCategories = entity.SubCategories.ToList();

            // Remove subcategories that are not in the new list
            var subCategoriesToRemove = existingSubCategories
                .Where(s => !newSubCategories.Any(ns => ns.Id == s.Id && s.Id != 0))
                .ToList();

            foreach (var sub in subCategoriesToRemove)
            {
                _dbContext.SubCategories.Remove(sub);
            }

            // Add or update subcategories
            foreach (var sub in newSubCategories)
            {
                if (sub.Id == 0) // New subcategory
                {
                    _dbContext.SubCategories.Add(sub);
                }
                else
                {
                    // Update existing subcategory
                    var existingSub = existingSubCategories.FirstOrDefault(s => s.Id == s.Id);
                    if (existingSub != null)
                    {
                        _dbContext.Entry(existingSub).CurrentValues.SetValues(sub);
                    }
                }
            }
        }

        public async Task<IEnumerable<Product>> GetAllAsync(
            Expression<Func<Product, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<Product> query = _dbContext.Products;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return await query.ToListAsync();
        }
    }
}