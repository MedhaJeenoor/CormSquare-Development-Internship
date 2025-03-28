using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface IReferenceRepository : IRepository<Reference>
    {
        void Update(Reference obj);
        Task<IEnumerable<Reference>> GetAllAsync(
            Expression<Func<Reference, bool>>? filter = null,
            string? includeProperties = null
        );
    }
}