// Agt.Domain/Abstractions/IProcessDefinitionService.cs
using Agt.Domain.Models;

namespace Agt.Domain.Abstractions;

/// <summary>
/// Service for working with process definitions (stages, blocks, transitions)
/// for a single form version.
/// </summary>
public interface IProcessDefinitionService
{
    /// <summary>
    /// Loads process graph for the specified form version, or null when not present.
    /// </summary>
    ProcessGraph? LoadGraph(Guid formVersionId);

    /// <summary>
    /// Persists process graph for the specified form version.
    /// Existing definition (if any) is overwritten.
    /// </summary>
    void SaveGraph(ProcessGraph graph);
}
