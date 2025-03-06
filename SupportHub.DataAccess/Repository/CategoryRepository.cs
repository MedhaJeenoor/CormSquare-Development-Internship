using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private ApplicationDbContext _db;
        public CategoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public async Task<IEnumerable<Category>> GetAllAsync(
        Expression<Func<Category, bool>>? filter = null,
        string? includeProperties = null)
        {
            IQueryable<Category> query = _db.Categories.Where(c => !c.IsDeleted); // Always exclude soft-deleted categories

            // Apply filter if any
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Include related properties with nested support
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            // Include nested subcategories explicitly to ensure all levels are loaded
            query = query.Include(c => c.SubCategories)
                         .ThenInclude(sc => sc.SubCategories);

            return await query.ToListAsync();
        }



        public void Update(Category obj)
        {
            var existingCategory = _db.Categories.FirstOrDefault(c => c.Id == obj.Id);
            if (existingCategory != null)
            {
                _db.Entry(existingCategory).CurrentValues.SetValues(obj);
            }
        }

    }
}
