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
    public class SolutionRepository : Repository<Solution>, ISolutionRepository
    {
        private ApplicationDbContext _db;
        public SolutionRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public async Task<IEnumerable<Solution>> GetAllAsync(
        Expression<Func<Solution, bool>>? filter = null,
        string? includeProperties = null)
        {
            IQueryable<Solution> query = _db.Solutions.Where(c => !c.IsDeleted); // Always exclude soft-deleted categories

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
            query = query.Include(c => c.SubCategory);
                         //.ThenInclude(sc => sc.SubCategories);

            return await query.ToListAsync();
        }

        //public async Task<Category> GetFirstOrDefaultAsync(Expression<Func<Category, bool>> filter, string includeProperties = "")
        //{
        //    IQueryable<Category> query = _db.Where(filter);

        //    foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        //    {
        //        query = query.Include(includeProperty);
        //    }

        //    return await query.FirstOrDefaultAsync();
        //}

        public void Update(Solution obj)
        {
            var existingSolution = _db.Solutions.FirstOrDefault(c => c.Id == obj.Id);
            if (existingSolution != null)
            {
                // Preserve CreatedBy and CreatedDate
                obj.CreatedBy = existingSolution.CreatedBy;
                obj.CreatedDate = existingSolution.CreatedDate;

                _db.Entry(existingSolution).CurrentValues.SetValues(obj);

                // Prevent overriding CreatedBy & CreatedDate in EF tracking
                _db.Entry(existingSolution).Property(c => c.CreatedBy).IsModified = false;
                _db.Entry(existingSolution).Property(c => c.CreatedDate).IsModified = false;
            }
        }

    }
}
