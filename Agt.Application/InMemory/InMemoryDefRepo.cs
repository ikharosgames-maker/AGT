using Agt.Application.Interfaces;
using Agt.Contracts;

namespace Agt.Application.InMemory;

public class InMemoryDefRepo : IDefRepo
{
    private readonly List<BlockDefinitionDto> _blocks = new();
    private readonly List<FormDefinitionDto> _forms = new();

    public InMemoryDefRepo()
    {
        // seed: 2 bloky a 1 formulář (přeneseno z dosavadního kódu)
        _blocks.Add(new("block.customerHeader", 1, "Hlavička zákazníka",
            new[] {
                new FieldDto("customerNumber","text","Číslo zákazníka", true),
                new FieldDto("name","text","Název", true),
                new FieldDto("address.city","text","Město")
            }, new UiHintsDto("grid-2")));
        _blocks.Add(new("block.noteDateAuthor", 1, "Poznámka + Datum + Autor",
            new[] {
                new FieldDto("note","text","Poznámka", true),
                new FieldDto("date","date","Datum"),
                new FieldDto("author","text","Autor", true)
            }, new UiHintsDto("grid-1")));

        _forms.Add(new("form.claim", 2, "Reklamace",
            new[] {
                new FormBlockRefDto("block.customerHeader", 1),
                new FormBlockRefDto("block.noteDateAuthor", 1, new() { ["noteLabel"] = "Popis závady" })
            }));
    }

    public Task<IReadOnlyList<BlockDefinitionDto>> GetBlocksAsync() => Task.FromResult<IReadOnlyList<BlockDefinitionDto>>(_blocks.ToList());
    public Task<BlockDefinitionDto?> GetBlockAsync(string id, int version) => Task.FromResult(_blocks.FirstOrDefault(b => b.Id == id && b.Version == version));
    public Task<BlockDefinitionDto> SaveBlockAsync(BlockDefinitionDto dto)
    {
        var newVersion = (_blocks.Where(b => b.Id == dto.Id).Select(b => b.Version).DefaultIfEmpty(0).Max()) + 1;
        var saved = dto with { Version = newVersion };
        _blocks.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<IReadOnlyList<FormDefinitionDto>> GetFormsAsync() => Task.FromResult<IReadOnlyList<FormDefinitionDto>>(_forms.ToList());
    public Task<FormDefinitionDto?> GetFormAsync(string id, int version) => Task.FromResult(_forms.FirstOrDefault(f => f.Id == id && f.Version == version));
    public Task<FormDefinitionDto> SaveFormAsync(FormDefinitionDto dto)
    {
        var newVersion = (_forms.Where(f => f.Id == dto.Id).Select(f => f.Version).DefaultIfEmpty(0).Max()) + 1;
        var saved = dto with { Version = newVersion };
        _forms.Add(saved);
        return Task.FromResult(saved);
    }
}
