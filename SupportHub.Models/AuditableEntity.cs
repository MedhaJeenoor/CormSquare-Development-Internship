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
    }
}
