// Agt.Application/Services/CaseWorkflowService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Application.Services;

/// <summary>
/// Orchestruje běh case po stage – používá ProcessGraph (StageDefinition,
/// StageBlock, StageTransition) a TaskService.
/// Stage příslušnost CaseBlocků se odvozuje z grafu (přes BlockKey),
/// není uložená přímo v CaseBlock.
/// </summary>
public sealed class CaseWorkflowService : ICaseWorkflowService
{
    private readonly ICaseRepository _cases;
    private readonly IBlockRepository _blocks;
    private readonly ITaskService _tasks;
    private readonly IProcessDefinitionService _processDefs;

    public CaseWorkflowService(
        ICaseRepository cases,
        IBlockRepository blocks,
        ITaskService tasks,
        IProcessDefinitionService processDefs)
    {
        _cases = cases;
        _blocks = blocks;
        _tasks = tasks;
        _processDefs = processDefs;
    }

    /// <summary>
    /// Inicializuje case do startovní stage (první StageDefinition podle Order).
    /// Pokud už bloky této stage pro case existují, neudělá nic (idempotentní).
    /// </summary>
    public void InitializeCase(Guid caseId, Guid actorUserId)
    {
        var c = _cases.Get(caseId)
            ?? throw new InvalidOperationException($"Case {caseId} nebyl nalezen.");

        var graph = _processDefs.LoadGraph(c.FormVersionId)
            ?? throw new InvalidOperationException($"Pro FormVersion {c.FormVersionId} není definovaný ProcessGraph.");

        var startStage = graph.Stages.OrderBy(s => s.Order).FirstOrDefault()
            ?? throw new InvalidOperationException("ProcessGraph nemá žádnou StageDefinition.");

        var stageBlockKeys = GetStageBlockKeys(c.FormVersionId, graph, startStage.Id);

        // pokud už existuje aspoň jeden blok patřící do téhle stage, stage je pro case inicializovaná
        var existingBlocks = _cases.ListBlocks(caseId)
                                   .Where(b => stageBlockKeys.Contains(b.BlockKey, StringComparer.OrdinalIgnoreCase))
                                   .ToList();
        if (existingBlocks.Count > 0)
            return;

        CreateBlocksForStage(c, startStage, graph, actorUserId);
    }

    /// <summary>
    /// Dokončí stage (pokud jsou všechny její bloky hotové) a posune case do
    /// další stage podle StageTransition grafu. Vytvoří nové CaseBlocky/Tasky.
    /// </summary>
    public void CompleteStageAndAdvance(Guid caseId, Guid stageId, Guid actorUserId)
    {
        var c = _cases.Get(caseId)
            ?? throw new InvalidOperationException($"Case {caseId} nebyl nalezen.");

        var graph = _processDefs.LoadGraph(c.FormVersionId)
            ?? throw new InvalidOperationException($"Pro FormVersion {c.FormVersionId} není definovaný ProcessGraph.");

        var stage = graph.Stages.FirstOrDefault(s => s.Id == stageId)
            ?? throw new InvalidOperationException($"StageDefinition {stageId} nenalezena.");

        var allBlocks = _cases.ListBlocks(caseId).ToList();
        var stageBlockKeys = GetStageBlockKeys(c.FormVersionId, graph, stageId);

        var stageBlocks = allBlocks
            .Where(b => stageBlockKeys.Contains(b.BlockKey, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (stageBlocks.Count == 0)
            throw new InvalidOperationException("Stage nemá žádné bloky pro tento case.");

        // 1) ověř, že všechny bloky stage jsou dokončené
        var notDone = stageBlocks.Where(b =>
            b.State != CaseBlockState.Done &&
            b.State != CaseBlockState.Rejected).ToList();

        if (notDone.Any())
        {
            var ids = string.Join(", ", notDone.Select(b => b.Id));
            throw new InvalidOperationException($"Stage nelze dokončit – bloky nejsou hotové: {ids}");
        }

        // 2) zajisti, aby všechny bloky stage byly v TaskService označené jako Done
        foreach (var b in stageBlocks)
        {
            _tasks.SetStatus(b.Id, "Done", actorUserId);
        }

        // 3) najdi cílové stage podle StageTransition
        var outgoing = graph.Transitions
            .Where(t => t.FromStageId == stageId)
            .ToList();

        if (outgoing.Count == 0)
        {
            // žádný další krok – case může být ukončen (logiku na Case.State
            // můžeš přidat sem)
            return;
        }

        // TODO: vyhodnocení Condition pro StageTransition.
        // Pro první integraci bereme všechny přechody jako splněné.
        foreach (var tr in outgoing)
        {
            var targetStage = graph.Stages.FirstOrDefault(s => s.Id == tr.ToStageId);
            if (targetStage == null)
                continue;

            // pokud už bloky pro cílovou stage existují, nepřidávej další
            var targetKeys = GetStageBlockKeys(c.FormVersionId, graph, targetStage.Id);
            var existing = allBlocks
                .Where(b => targetKeys.Contains(b.BlockKey, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (existing.Any())
                continue;

            CreateBlocksForStage(c, targetStage, graph, actorUserId);
        }
    }

    /// <summary>
    /// Spočítá přiřazení pro CaseBlock na základě AssignmentRule stage
    /// (do budoucna i StageTransition) a případných fallbacků.
    /// </summary>
    private (Guid? userId, Guid? groupId, DateTime? dueAt) ResolveAssignment(
        StageDefinition stage,
        Case c,
        CaseBlock cb)
    {
        var rule = stage.AssignmentRule;

        if (rule == null)
        {
            // zatím žádný automatický fallback – můžeš tady později doplnit např.:
            // - owner case
            // - skupinu z form metadat
            return (null, null, null);
        }

        Guid? userId = rule.UserId;
        Guid? groupId = rule.GroupId;

        DateTime? dueAt = null;
        if (rule.DueInHours.HasValue && rule.DueInHours.Value > 0)
        {
            dueAt = DateTime.UtcNow.AddHours(rule.DueInHours.Value);
        }

        // Expression zatím nevyhodnocujeme – připravené pro budoucí mini rules engine.
        // (tam se může vyhodnotit predikát nad Case/CaseBlock/UserSettings a přepsat userId/groupId/dueAt.)

        return (userId, groupId, dueAt);
    }
    /// <summary>
    /// Runtime pohled pro UI – jednotlivé stage a jejich bloky pro konkrétní case.
    /// Stage je ReadOnly, pokud jsou všechny její bloky Done/Rejected.
    /// </summary>
    public IReadOnlyList<CaseStageRuntime> GetRuntimeStages(Guid caseId)
    {
        var c = _cases.Get(caseId)
            ?? throw new InvalidOperationException($"Case {caseId} nebyl nalezen.");

        var graph = _processDefs.LoadGraph(c.FormVersionId)
            ?? throw new InvalidOperationException($"Pro FormVersion {c.FormVersionId} není definovaný ProcessGraph.");

        var blocks = _cases.ListBlocks(caseId).ToList();
        var result = new List<CaseStageRuntime>();

        foreach (var stage in graph.Stages.OrderBy(s => s.Order))
        {
            var keys = GetStageBlockKeys(c.FormVersionId, graph, stage.Id);
            var stageBlocks = blocks
                .Where(b => keys.Contains(b.BlockKey, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (stageBlocks.Count == 0)
                continue;

            var isCompleted = stageBlocks.All(b =>
                b.State == CaseBlockState.Done ||
                b.State == CaseBlockState.Rejected);

            result.Add(new CaseStageRuntime
            {
                StageId = stage.Id,
                StageTitle = stage.Title,
                Order = stage.Order,
                IsReadOnly = isCompleted,
                Blocks = stageBlocks
            });
        }

        return result;
    }

    /// <summary>
    /// Vytvoří CaseBlocky a Tasky pro všechny bloky definované ve stage.
    /// Stage příslušnost se odvozuje z ProcessGraph, neukládá se přímo do CaseBlock.
    /// </summary>
    private void CreateBlocksForStage(Case c, StageDefinition stage, ProcessGraph graph, Guid actorUserId)
    {
        var sb = graph.StageBlocks
                      .Where(x => x.StageId == stage.Id)
                      .OrderBy(x => x.Order)
                      .ToList();

        if (sb.Count == 0)
            return;

        // mapování BlockDefinitionId -> Key/Title
        var blockDefs = _blocks.ListByFormVersion(c.FormVersionId).ToList();
        var byId = blockDefs.ToDictionary(d => d.Id, d => d, EqualityComparer<Guid>.Default);

        foreach (var link in sb)
        {
            if (!byId.TryGetValue(link.BlockDefinitionId, out var blockDef))
                continue;

            var cb = new CaseBlock
            {
                Id = Guid.NewGuid(),
                CaseId = c.Id,
                BlockKey = blockDef.Key,
                Title = string.IsNullOrWhiteSpace(blockDef.Name) ? blockDef.Key : blockDef.Name,
                State = CaseBlockState.Open,
                DataJson = "{}"
            };

            _cases.UpsertBlock(cb);

            // AssignmentRule → userId, groupId, dueAt
            var (userId, groupId, dueAt) = ResolveAssignment(stage, c, cb);

            _tasks.Assign(cb.Id, userId, groupId, dueAt, actorUserId);
        }
    }


    /// <summary>
    /// Vrátí množinu BlockKey, které patří do dané stage (podle StageBlocks).
    /// </summary>
    private HashSet<string> GetStageBlockKeys(Guid formVersionId, ProcessGraph graph, Guid stageId)
    {
        var stageBlockIds = graph.StageBlocks
            .Where(sb => sb.StageId == stageId)
            .Select(sb => sb.BlockDefinitionId)
            .ToHashSet();

        if (stageBlockIds.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var blockDefs = _blocks.ListByFormVersion(formVersionId).ToList();

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var bd in blockDefs)
        {
            if (stageBlockIds.Contains(bd.Id))
                keys.Add(bd.Key);
        }

        return keys;
    }
}
