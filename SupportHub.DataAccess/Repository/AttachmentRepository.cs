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
    public class AttachmentRepository : Repository<Attachment>, IAttachmentRepository
    {
        private readonly ApplicationDbContext _db;
        public AttachmentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Attachment>> GetAllAsync(
            Expression<Func<Attachment, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<Attachment> query = _db.Attachments.Where(c => !c.IsDeleted); // Always exclude soft-deleted categories

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

        public void Update(Attachment obj)
        {
            var existingAttachment = _db.Attachments.FirstOrDefault(c => c.Id == obj.Id);
            if (existingAttachment != null)
            {
                // Preserve CreatedBy and CreatedDate
                obj.CreatedBy = existingAttachment.CreatedBy;
                obj.CreatedDate = existingAttachment.CreatedDate;

                _db.Entry(existingAttachment).CurrentValues.SetValues(obj);

                // Prevent overriding CreatedBy & CreatedDate in EF tracking
                _db.Entry(existingAttachment).Property(c => c.CreatedBy).IsModified = false;
                _db.Entry(existingAttachment).Property(c => c.CreatedDate).IsModified = false;
            }
        }
    }
}