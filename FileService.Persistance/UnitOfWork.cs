using FileService.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Persistance
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public IStoredFileRepository StoredFiles { get; }

        public UnitOfWork(AppDbContext db, IStoredFileRepository storedFiles)
        {
            _db = db;
            StoredFiles = storedFiles;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }
}
