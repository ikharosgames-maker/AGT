using System;
using System.Threading.Tasks;
using Agt.Domain.Models;

namespace Agt.Domain.Repositories
{
    public interface ICaseDataRepository
    {
        Task<CaseDataSnapshot?> LoadAsync(Guid caseId);
        Task SaveAsync(CaseDataSnapshot snapshot);
    }
}
