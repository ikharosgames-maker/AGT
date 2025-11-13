// Agt.Domain/Models/CaseStageRuntime.cs
namespace Agt.Domain.Models;

/// <summary>
/// Runtime pohled na jednu stage v rámci konkrétního case.
/// </summary>
public sealed class CaseStageRuntime
{
    public Guid StageId { get; set; }
    public string StageTitle { get; set; } = "";
    public int Order { get; set; }

    /// <summary>Stage je pouze pro čtení (case už je v další stage).</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>Bloky patřící do této stage (CaseBlock instance).</summary>
    public IReadOnlyList<CaseBlock> Blocks { get; set; } = Array.Empty<CaseBlock>();
}
