// JsonTaskRepository.cs
using System.Text.Json;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore;

public sealed class JsonTaskRepository : BaseJsonRepo, ITaskRepository
{
    public JsonTaskRepository() : base("tasks") { }

    public TaskItem? GetByCaseBlock(Guid caseBlockId)
        => AllFiles()
           .Select(f => JsonSerializer.Deserialize<TaskItem>(File.ReadAllText(f)))
           .FirstOrDefault(t => t is not null && t!.CaseBlockId == caseBlockId);

    public void Upsert(TaskItem task)
    {
        if (task.Id == Guid.Empty) task.Id = Guid.NewGuid();
        Save(task.Id, task).GetAwaiter().GetResult();
    }
}
