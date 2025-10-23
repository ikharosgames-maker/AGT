using System;
using System.Threading.Tasks;
using Agt.Domain.Models;

namespace Agt.Domain.Repositories
{
    /// <summary>Úložiště vyplněných hodnot pro case (per CaseId).</summary>
    public interface ICaseDataRepository
    {
        Task<CaseDataSnapshot?> LoadAsync(Guid caseId);
        Task SaveAsync(CaseDataSnapshot snapshot);
    }
}
