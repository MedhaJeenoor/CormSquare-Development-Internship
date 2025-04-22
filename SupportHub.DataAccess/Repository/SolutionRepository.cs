using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository
{
    public class SolutionRepository : Repository<Solution>, ISolutionRepository
    {
        private readonly ApplicationDbContext _db;

        public SolutionRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Solution>> GetAllAsync(
            Expression<Func<Solution, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<Solution> query = _db.Solutions.Where(c => !c.IsDeleted);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            return await query.ToListAsync();
        }

        public void Update(Solution obj)
        {
            var existingSolution = _db.Solutions.FirstOrDefault(c => c.Id == obj.Id);
            if (existingSolution != null)
            {
                obj.CreatedBy = existingSolution.CreatedBy;
                obj.CreatedDate = existingSolution.CreatedDate;
                _db.Entry(existingSolution).CurrentValues.SetValues(obj);
                _db.Entry(existingSolution).Property(c => c.CreatedBy).IsModified = false;
                _db.Entry(existingSolution).Property(c => c.CreatedDate).IsModified = false;
            }
        }

        public List<Solution> GetApprovedSolutions(string includeProperties)
        {
            IQueryable<Solution> query = _db.Solutions
                .Where(s => s.Status == "Approved" && !s.IsDeleted);

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            return query.ToList();
        }
    }
}