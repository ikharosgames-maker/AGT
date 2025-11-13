// Agt.Domain/Models/AssignmentRule.cs
namespace Agt.Domain.Models;

/// <summary>
/// Jednoduché přiřazovací pravidlo k určení primárního příjemce úkolu.
/// V první fázi držíme pouze UserId/GroupId, další strategie se dají
/// doplnit přes MetadataJson nebo rozšíření modelu.
/// </summary>
public sealed class AssignmentRule
{
    /// <summary>Konkrétní uživatel, kterému se má stage/úkol přiřadit.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Skupina, která má stage/úkol zpracovat.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Volitelné doplňkové informace (např. strategie, user-settings klíče…),
    /// v JSON formátu kvůli snadné serializaci.
    /// </summary>
    public string? MetadataJson { get; set; }
}
