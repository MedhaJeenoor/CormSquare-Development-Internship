using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface IIssueRepository : IRepository<Issue>
    {
        void Update(Issue issue);
        Task AddAsync(Issue issue);
    }
}
