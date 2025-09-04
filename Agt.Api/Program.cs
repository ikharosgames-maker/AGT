using Agt.Application.InMemory;
using Agt.Application.Interfaces;
using Agt.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IFormRepo, InMemoryFormRepo>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/forms/definitions/{id}/{version:int}/resolve", async (string id, int version, IFormRepo repo) =>
{
    var form = await repo.GetFormDefinitionAsync(id, version);
    var blocks = await repo.GetBlocksAsync(form.Blocks.Select(b => (b.Ref, b.Version)));
    return Results.Ok(new { form, blocks });
});

app.MapPost("/forms/responses", async (FormResponseCreateDto dto, IFormRepo repo) =>
{
    var created = await repo.CreateResponseAsync(dto);
    return Results.Ok(created);
});

app.MapGet("/forms/responses", async (string formId, int? formVersion, string? customerNumber, IFormRepo repo) =>
{
    var items = await repo.QueryResponsesAsync(formId, formVersion, customerNumber);
    return Results.Ok(items);
});

app.Run();
