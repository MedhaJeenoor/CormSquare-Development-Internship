using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository
{
    public class SolutionReferenceRepository : Repository<SolutionReference>, ISolutionReferenceRepository
    {
        private readonly ApplicationDbContext _db;
        public SolutionReferenceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<SolutionReference>> GetAllAsync(
            Expression<Func<SolutionReference, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<SolutionReference> query = _db.SolutionReferences.Where(c => !c.IsDeleted); // Always exclude soft-deleted categories

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

            return await query.ToListAsync();
        }

        public void Update(SolutionReference obj)
        {
            var existingReference = _db.SolutionReferences.FirstOrDefault(c => c.Id == obj.Id);
            if (existingReference != null)
            {
                // Preserve CreatedBy and CreatedDate
                obj.CreatedBy = existingReference.CreatedBy;
                obj.CreatedDate = existingReference.CreatedDate;

                _db.Entry(existingReference).CurrentValues.SetValues(obj);

                // Prevent overriding CreatedBy & CreatedDate in EF tracking
                _db.Entry(existingReference).Property(c => c.CreatedBy).IsModified = false;
                _db.Entry(existingReference).Property(c => c.CreatedDate).IsModified = false;
            }
        }
    }
}