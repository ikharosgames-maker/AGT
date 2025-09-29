// Agt.Application/Services/AuthZ.cs
using Agt.Domain.Abstractions;

namespace Agt.Application.Services;

public sealed class AuthZ : IAuthZ
{
    public bool CanCreateBlocks(Guid userId) => true;
    public bool CanCreateForms(Guid userId) => true;
    public bool CanPublishForms(Guid userId) => true;
    public bool CanReopenLockedBlocks(Guid userId, Guid formVersionId) => true;
}
