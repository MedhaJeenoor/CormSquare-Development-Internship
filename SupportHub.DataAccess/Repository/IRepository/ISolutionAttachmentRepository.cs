using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface ISolutionAttachmentRepository : IRepository<SolutionAttachment>
    {
        void Update(SolutionAttachment obj);
        Task<IEnumerable<SolutionAttachment>> GetAllAsync(
            System.Linq.Expressions.Expression<System.Func<SolutionAttachment, bool>>? filter = null,
            string? includeProperties = null
        );
    }
}