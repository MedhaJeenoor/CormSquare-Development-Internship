using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;

public class AttachmentRepository : Repository<Attachment>, IAttachmentRepository
{
    private readonly ApplicationDbContext _db;
    public AttachmentRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(Attachment attachment)
    {
        _db.Attachments.Update(attachment);
    }
}