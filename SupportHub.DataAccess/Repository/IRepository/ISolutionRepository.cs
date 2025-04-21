using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface ISolutionRepository : IRepository<Solution>
    {

        void Update(Solution obj);
        Task<IEnumerable<Solution>> GetAllAsync(
            Expression<Func<Solution, bool>>? filter = null,
            string? includeProperties = null
        );
        List<Solution> GetApprovedSolutions();

    }
}
