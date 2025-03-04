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
            if (Id > 0)
            {
                UpdatedBy = userId;
                UpdatedDate = DateTime.UtcNow;
            }
            else
            {
                CreatedBy = userId;
                CreatedDate = DateTime.UtcNow;
            }
        }
        public void setCreatedBy(int userId)
        {
            CreatedBy = userId;

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
