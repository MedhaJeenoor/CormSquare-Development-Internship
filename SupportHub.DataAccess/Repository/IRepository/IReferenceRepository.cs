using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface IReferenceRepository : IRepository<Reference>
    {
        void Update(Reference reference);
    }
}
