using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.Models;

namespace SupportHub.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;
        private IDbContextTransaction _transaction;
        public ICategoryRepository Category { get; private set; }
        public IAttachmentRepository Attachment { get; private set; }
        public IReferenceRepository Reference { get; private set; }
        public IProductRepository Product { get; private set; }
        public ISubCategoryRepository SubCategory { get; private set; }
        public IIssueRepository Issue { get; private set; }


        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Category = new CategoryRepository(_db);
            Attachment = new AttachmentRepository(_db);
            Reference = new ReferenceRepository(_db);
            Product = new ProductRepository(_db);
            SubCategory = new SubCategoryRepository(_db);
            Issue = new IssueRepository(_db);
        }

        // Begin Transaction
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _transaction = await _db.Database.BeginTransactionAsync(isolationLevel);
        }

        // Commit Transaction
        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Rollback Transaction
        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Save()
        {
            _db.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
