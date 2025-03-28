using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface ISolutionReferenceRepository : IRepository<SolutionReference>
    {
        void Update(SolutionReference obj);
        Task<IEnumerable<SolutionReference>> GetAllAsync(
            Expression<Func<SolutionReference, bool>>? filter = null,
            string? includeProperties = null
        );
    }
}