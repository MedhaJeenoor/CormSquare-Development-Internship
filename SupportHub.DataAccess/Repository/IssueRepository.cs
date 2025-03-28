using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.DataAccess.Repository;
using SupportHub.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


namespace SupportHub.DataAccess.Repository
{
    public class IssueRepository : Repository<Issue>, IIssueRepository
    {
        private readonly ApplicationDbContext _context;

        public IssueRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddAsync(Issue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            await _context.Issues.AddAsync(issue); // Ensure _db is your DbContext
        }


        public void Update(Issue issue)
        {
            var existingIssue = _context.Issues.FirstOrDefault(i => i.Id == issue.Id);
            if (existingIssue != null)
            {
                existingIssue.Status = issue.Status;
                existingIssue.AdminResponse = issue.AdminResponse;
                _context.Issues.Update(existingIssue);
            }
        }
    }
}
