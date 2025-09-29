namespace Agt.Domain.Abstractions;

public interface ICaseService
{
    Guid StartCase(Guid formVersionId, Guid actor, StartSelection selection);
    void CompleteBlock(Guid caseBlockId, Guid actor);
    void ReopenBlock(Guid caseBlockId, Guid actor, string reason);
}
public record StartSelection(string? Preset, IReadOnlyList<string> Blocks);
