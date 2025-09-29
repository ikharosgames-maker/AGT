namespace Agt.Domain.Abstractions;

public interface IAuthZ
{
    bool CanCreateBlocks(Guid userId);
    bool CanCreateForms(Guid userId);
    bool CanPublishForms(Guid userId);
    bool CanReopenLockedBlocks(Guid userId, Guid formVersionId);
}
