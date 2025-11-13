// Agt.Domain/Abstractions/ICaseWorkflowService.cs
using Agt.Domain.Models;

namespace Agt.Domain.Abstractions;

/// <summary>
/// Orchestruje běh case po stage – vytváří bloky pro stage,
/// vyhodnocuje přechody do dalších stagí a vytváří úkoly.
/// </summary>
public interface ICaseWorkflowService
{
    /// <summary>
    /// Inicializuje case do startovní stage (např. první StageDefinition podle Order).
    /// Vytvoří CaseBlocky a Tasky pro startovní stage.
    /// </summary>
    void InitializeCase(Guid caseId, Guid actorUserId);

    /// <summary>
    /// Dokončí stage (pokud jsou všechny její bloky hotové) a posune case do
    /// další stage podle StageTransition grafu. Vytvoří nové CaseBlocky/Tasky.
    /// </summary>
    void CompleteStageAndAdvance(Guid caseId, Guid stageId, Guid actorUserId);

    /// <summary>
    /// Vrátí runtime pohled na všechna „navštívená“ stage case:
    /// jednotlivé stage + jejich CaseBlocky, včetně info pro UI (RO vs. edit).
    /// </summary>
    IReadOnlyList<CaseStageRuntime> GetRuntimeStages(Guid caseId);
}
