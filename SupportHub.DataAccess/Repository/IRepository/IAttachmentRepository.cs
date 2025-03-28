using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface IAttachmentRepository : IRepository<Attachment>
    {
        void Update(Attachment obj);
        Task<IEnumerable<Attachment>> GetAllAsync(
            System.Linq.Expressions.Expression<System.Func<Attachment, bool>>? filter = null,
            string? includeProperties = null
        );
    }
}