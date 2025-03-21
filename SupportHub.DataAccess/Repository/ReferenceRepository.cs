using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;

public class ReferenceRepository : Repository<Reference>, IReferenceRepository
{
    private readonly ApplicationDbContext _db;
    public ReferenceRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(Reference reference)
    {
        _db.References.Update(reference);
    }
}