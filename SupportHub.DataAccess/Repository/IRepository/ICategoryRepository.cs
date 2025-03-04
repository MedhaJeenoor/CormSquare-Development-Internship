using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category obj);
        Task<IEnumerable<Category>> GetAllAsync(
            Expression<Func<Category, bool>>? filter = null,
            string? includeProperties = null
        );
    }
}
