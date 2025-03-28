using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface ISubCategoryRepository : IRepository<SubCategory>
    {
        void Update(SubCategory subCategory);
        
    }
}
