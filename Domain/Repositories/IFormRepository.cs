using Agt.Domain.Models;

namespace Agt.Domain.Repositories;

public interface IFormRepository
{
    Form? Get(Guid id);
    FormVersion? GetVersion(Guid id);
    IEnumerable<FormVersion> ListVersions(Guid formId);
    void Upsert(Form form);
    void UpsertVersion(FormVersion version);
}
