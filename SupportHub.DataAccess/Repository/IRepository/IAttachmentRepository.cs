using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface IAttachmentRepository : IRepository<Attachment>
    {
        void Update(Attachment attachment);
    }
}
