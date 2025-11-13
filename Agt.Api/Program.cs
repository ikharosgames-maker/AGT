using Agt.Application.InMemory;
using Agt.Application.Interfaces;
using Agt.Contracts;
using Microsoft.AspNetCore.Mvc; // <-- DLEIT pro [FromServices]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JSON resolver (ponech, co ti funguje)
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.TypeInfoResolverChain.Add(new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
});

builder.Services.AddSingleton<IFormRepo, InMemoryFormRepo>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ---------- ENDPOINTY s [FromServices] ----------
app.MapGet("/forms/definitions/{id}/{version:int}/resolve",
    async (string id, int version, [FromServices] IFormRepo repo) =>
    {
        var form = await repo.GetFormDefinitionAsync(id, version);
        var blocks = await repo.GetBlocksAsync(form.Blocks.Select(b => (b.Ref, b.Version)));
        return Results.Ok(new { form, blocks });
    });

app.MapPost("/forms/responses",
    async ([FromBody] FormResponseCreateDto dto, [FromServices] IFormRepo repo) =>
    {
        var created = await repo.CreateResponseAsync(dto);
        return Results.Ok(created);
    });

app.MapGet("/forms/responses",
    async ([FromQuery] string formId, [FromQuery] int? formVersion, [FromQuery] string? customerNumber,
           [FromServices] IFormRepo repo) =>
    {
        var items = await repo.QueryResponsesAsync(formId, formVersion, customerNumber);
        return Results.Ok(items);
    });
// -----------------------------------------------
// Blocks
app.MapGet("/defs/blocks", ([FromServices] IDefRepo repo) => repo.GetBlocksAsync());
app.MapGet("/defs/blocks/{id}/{version:int}", ([FromServices] IDefRepo repo, string id, int version) => repo.GetBlockAsync(id, version));
app.MapPost("/defs/blocks", async ([FromServices] IDefRepo repo, [FromBody] BlockDefinitionDto dto) =>
{
    // jednoduch validace kl
    if (dto.Fields.GroupBy(f => f.Key).Any(g => g.Count() > 1))
        return Results.BadRequest("Duplicity field keys");
    var saved = await repo.SaveBlockAsync(dto);
    return Results.Ok(saved);
});

// Forms
app.MapGet("/defs/forms", ([FromServices] IDefRepo repo) => repo.GetFormsAsync());
app.MapGet("/defs/forms/{id}/{version:int}", ([FromServices] IDefRepo repo, string id, int version) => repo.GetFormAsync(id, version));
app.MapPost("/defs/forms", async ([FromServices] IDefRepo repo, [FromBody] FormDefinitionDto dto) =>
{
    // kontrola, e referenced bloky existuj
    // (MVP  bez verze validace detailn)
    var saved = await repo.SaveFormAsync(dto);
    return Results.Ok(saved);
});

app.Run();