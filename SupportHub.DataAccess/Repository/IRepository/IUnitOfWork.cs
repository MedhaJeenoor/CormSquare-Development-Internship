using SupportHub.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportHub.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICategoryRepository Category { get; }
        IAttachmentRepository Attachment { get; }
        IReferenceRepository Reference { get; }
        ISubCategoryRepository SubCategory { get; }
        IProductRepository Product { get; }
        IIssueRepository Issue { get; }
        Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        void Save();
        Task SaveAsync();
    }
}
