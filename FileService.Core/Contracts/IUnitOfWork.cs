using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.Contracts
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IStoredFileRepository StoredFiles { get; }
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
