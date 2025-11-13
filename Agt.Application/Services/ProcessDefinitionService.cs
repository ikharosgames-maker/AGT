// Agt.Application/Services/ProcessDefinitionService.cs
using System.Text.Json;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Application.Services;

/// <summary>
/// JSON-based implementation of process definition storage.
/// Short-term it uses FormVersion.StageGraphJson; later it can be replaced by SQL.
/// </summary>
public sealed class ProcessDefinitionService : IProcessDefinitionService
{
    private readonly IFormRepository _forms;

    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ProcessDefinitionService(IFormRepository forms)
    {
        _forms = forms;
    }

    public ProcessGraph? LoadGraph(Guid formVersionId)
    {
        var fv = _forms.GetVersion(formVersionId);
        if (fv == null) return null;

        if (string.IsNullOrWhiteSpace(fv.StageGraphJson) || fv.StageGraphJson == "{}")
            return new ProcessGraph
            {
                FormVersionId = fv.Id,
                Stages = Array.Empty<StageDefinition>(),
                StageBlocks = Array.Empty<StageBlock>(),
                Transitions = Array.Empty<StageTransition>()
            };

        try
        {
            var graph = JsonSerializer.Deserialize<ProcessGraph>(fv.StageGraphJson, Opt);
            if (graph == null)
                return null;

            // ensure FormVersionId consistency
            graph.FormVersionId = fv.Id;
            return graph;
        }
        catch
        {
            // corrupted / incompatible JSON – caller can decide how to handle
            return null;
        }
    }

    public void SaveGraph(ProcessGraph graph)
    {
        var fv = _forms.GetVersion(graph.FormVersionId);
        if (fv == null)
            throw new InvalidOperationException($"FormVersion {graph.FormVersionId:D} not found.");

        fv.StageGraphJson = JsonSerializer.Serialize(graph, Opt);
        _forms.UpsertVersion(fv);
    }
}
