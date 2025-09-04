using Agt.Contracts;


namespace Agt.Application.Interfaces;


public interface IFormRepo
{
    Task<FormDefinitionDto> GetFormDefinitionAsync(string id, int version);
    Task<IReadOnlyList<BlockDefinitionDto>> GetBlocksAsync(IEnumerable<(string Ref, int Version)> refs);


    Task<FormResponseCreatedDto> CreateResponseAsync(FormResponseCreateDto dto);
    Task<IReadOnlyList<(Guid Id, string FormId, int FormVersion)>> QueryResponsesAsync(
    string formId, int? formVersion = null, string? customerNumber = null);
}