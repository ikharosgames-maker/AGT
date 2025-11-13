// Agt.Domain/Models/ProcessGraph.cs
using System.Collections.Generic;

namespace Agt.Domain.Models;

/// <summary>
/// Aggregate representation of a process definition for a single form version.
/// </summary>
public sealed class ProcessGraph
{
    public Guid FormVersionId { get; set; }
    public IReadOnlyList<StageDefinition> Stages { get; set; } = Array.Empty<StageDefinition>();
    public IReadOnlyList<StageBlock> StageBlocks { get; set; } = Array.Empty<StageBlock>();
    public IReadOnlyList<StageTransition> Transitions { get; set; } = Array.Empty<StageTransition>();
}
