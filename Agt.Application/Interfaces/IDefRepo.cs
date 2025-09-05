using Agt.Contracts;

namespace Agt.Application.Interfaces;

public interface IDefRepo
{
    Task<IReadOnlyList<BlockDefinitionDto>> GetBlocksAsync();
    Task<BlockDefinitionDto?> GetBlockAsync(string id, int version);
    Task<BlockDefinitionDto> SaveBlockAsync(BlockDefinitionDto dto); // uloží a vrátí dto (nová verze)

    Task<IReadOnlyList<FormDefinitionDto>> GetFormsAsync();
    Task<FormDefinitionDto?> GetFormAsync(string id, int version);
    Task<FormDefinitionDto> SaveFormAsync(FormDefinitionDto dto);
}
