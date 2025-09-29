using Agt.Domain.Models;

namespace Agt.Domain.Repositories
{
    public interface ICaseRepository
    {
        Case? Get(Guid id);
        void Upsert(Case entity);

        CaseBlock? GetBlock(Guid id);
        IEnumerable<CaseBlock> ListBlocks(Guid caseId);
        void UpsertBlock(CaseBlock entity);

        // NOVÉ:
        IEnumerable<Case> ListAll();
        IEnumerable<Case> ListRecent(int take = 200); // volitelné
    }
}
