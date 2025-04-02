using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportHub.Models
{
    public class AuditableEntity : BaseEntity
    {
        public string CreatedBy { get; set; } // String GUID for user ID
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; } // String GUID, nullable
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; } // String GUID, nullable
        public DateTime? DeletedDate { get; set; }

        public void UpdateAudit(string userId)
        {
            if (Id > 0)  // Existing entity (Update)
            {
                if (string.IsNullOrEmpty(CreatedBy))  // Ensure CreatedBy is set only once
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

        public void SoftDelete(string userId)
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
