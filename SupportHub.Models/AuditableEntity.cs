using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportHub.Models
{
    public class AuditableEntity : BaseEntity
    {
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int? DeletedBy { get; set; } // Nullable - because it may not be deleted
        public DateTime? DeletedDate { get; set; } // Nullable - because it may not be deleted


        public void UpdateAudit(int userId)
        {
            if (Id > 0)  // Existing entity (Update)
            {
                if (CreatedBy == 0)  // Ensure CreatedBy is never overwritten
                {
                    CreatedBy = userId;
                    CreatedDate = DateTime.UtcNow;
                }

                UpdatedBy = userId;
                UpdatedDate = DateTime.UtcNow;
            }
            else  // New entity (Create)
            {
                CreatedBy = userId;
                CreatedDate = DateTime.UtcNow;
            }
        }

        public void SoftDelete(int userId)
        {
            if (!IsDeleted) // Prevent multiple deletions
            {
                IsDeleted = true;
                DeletedBy = userId;
                DeletedDate = DateTime.UtcNow;
            }
        }
    }
}
