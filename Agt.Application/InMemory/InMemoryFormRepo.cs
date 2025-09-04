using System.Text.Json;
using Agt.Application.Interfaces;
using Agt.Contracts;


namespace Agt.Application.InMemory;


public class InMemoryFormRepo : IFormRepo
{
    private readonly List<(Guid Id, string FormId, int FormVersion, JsonDocument Data, DateTime CreatedAt, string CreatedBy)> _responses = new();


    // Seed bloků a formuláře
    private static readonly BlockDefinitionDto BlockCustomerHeader = new(
    Id: "block.customerHeader", Version: 1, Name: "Hlavička zákazníka",
    Fields: new[]
    {
new FieldDto("customerNumber", "text", "Číslo zákazníka", true),
new FieldDto("name", "text", "Název", true),
new FieldDto("address.city", "text", "Město")
    },
    UiHints: new UiHintsDto("grid-2"));


    private static readonly BlockDefinitionDto BlockNoteDateAuthor = new(
    Id: "block.noteDateAuthor", Version: 1, Name: "Poznámka + Datum + Autor",
    Fields: new[]
    {
new FieldDto("note", "text", "Poznámka", true),
new FieldDto("date", "date", "Datum"),
new FieldDto("author", "text", "Autor", true)
    },
    UiHints: new UiHintsDto("grid-1"));


    private static readonly FormDefinitionDto FormClaimV2 = new(
    Id: "form.claim", Version: 2, Name: "Reklamace",
    Blocks: new[]
    {
new FormBlockRefDto("block.customerHeader", 1),
new FormBlockRefDto("block.noteDateAuthor", 1, new() { ["noteLabel"] = "Popis závady" })
    });


    public Task<FormDefinitionDto> GetFormDefinitionAsync(string id, int version)
    => Task.FromResult(FormClaimV2);


    public Task<IReadOnlyList<BlockDefinitionDto>> GetBlocksAsync(IEnumerable<(string Ref, int Version)> refs)
    => Task.FromResult<IReadOnlyList<BlockDefinitionDto>>(new[] { BlockCustomerHeader, BlockNoteDateAuthor });


    public Task<FormResponseCreatedDto> CreateResponseAsync(FormResponseCreateDto dto)
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.SerializeToDocument(new
        {
            formId = dto.FormId,
            formVersion = dto.FormVersion,
            data = dto.Data
        });
        _responses.Add((id, dto.FormId, dto.FormVersion, json, DateTime.UtcNow, dto.CreatedBy));
        return Task.FromResult(new FormResponseCreatedDto(id));
    }


    public Task<IReadOnlyList<(Guid Id, string FormId, int FormVersion)>> QueryResponsesAsync(
    string formId, int? formVersion = null, string? customerNumber = null)
    {
        var q = _responses.Where(r => r.FormId == formId && (!formVersion.HasValue || r.FormVersion == formVersion));
        if (!string.IsNullOrWhiteSpace(customerNumber))
        {
            q = q.Where(r => JsonDocumentContains(r.Data, "$.data.customerHeader.customerNumber", customerNumber!));
        }
        return Task.FromResult<IReadOnlyList<(Guid, string, int)>>(q.Select(r => (r.Id, r.FormId, r.FormVersion)).ToList());
    }


    private static bool JsonDocumentContains(JsonDocument doc, string path, string expected)
    {
        // Jednoduché vyhledání klíče (bez plného JSONPath – MVP)
        // Najde hodnotu pro zákaznické číslo v naší struktuře
        if (doc.RootElement.TryGetProperty("data", out var data) &&
        data.TryGetProperty("customerHeader", out var ch) &&
        ch.TryGetProperty("customerNumber", out var cn) &&
        cn.GetString() == expected)
        {
            return true;
        }
        return false;
    }
}