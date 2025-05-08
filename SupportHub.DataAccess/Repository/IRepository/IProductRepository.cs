using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        Task AddAsync(Product product);
        void Update(Product product);
        Task<IEnumerable<Product>> GetAllAsync(
            Expression<Func<Product, bool>>? filter = null,
            string? includeProperties = null
        );
    }
}
