using System.Text.Json;
using Agt.Application.Interfaces;
using Agt.Contracts;

namespace Agt.Application.InMemory;

public class InMemoryFormRepo : IFormRepo
{
    // Ukládáme data JSON bez „obalu“ (rovnou obsah dto.Data)
    private readonly List<(Guid Id, string FormId, int FormVersion, JsonDocument Data, DateTime CreatedAt, string CreatedBy)> _responses = new();

    // --- seed bloků a formuláře (beze změny) ---
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
        // ✅ ŽÁDNÁ serializace: jen zkopíruj přijatý JsonElement do JsonDocument
        using var tmp = JsonDocument.Parse(dto.Data.GetRawText());
        var json = JsonDocument.Parse(tmp.RootElement.GetRawText()); // vytvoř vlastní instance pro uložení

        _responses.Add((id, dto.FormId, dto.FormVersion, json, DateTime.UtcNow, dto.CreatedBy));
        return Task.FromResult(new FormResponseCreatedDto(id));
    }

    public Task<IReadOnlyList<(Guid Id, string FormId, int FormVersion)>> QueryResponsesAsync(
        string formId, int? formVersion = null, string? customerNumber = null)
    {
        var q = _responses.Where(r => r.FormId == formId && (!formVersion.HasValue || r.FormVersion == formVersion));
        if (!string.IsNullOrWhiteSpace(customerNumber))
        {
            q = q.Where(r => JsonDocumentContainsCustomerNumber(r.Data, customerNumber!));
        }
        return Task.FromResult<IReadOnlyList<(Guid, string, int)>>(q.Select(r => (r.Id, r.FormId, r.FormVersion)).ToList());
    }

    // Protože teď ukládáme přímo obsah 'data', cesta je na kořeni: customerHeader.customerNumber
    private static bool JsonDocumentContainsCustomerNumber(JsonDocument doc, string expected)
    {
        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("customerHeader", out var ch) &&
            ch.TryGetProperty("customerNumber", out var cn) &&
            cn.GetString() == expected)
        {
            return true;
        }
        return false;
    }
}
