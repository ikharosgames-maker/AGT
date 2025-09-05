using Agt.Contracts;
using System.Net.Http;
using System.Net.Http.Json;

namespace Agt.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(string baseUrl) => _http = new HttpClient { BaseAddress = new Uri(baseUrl) };

    public async Task<(FormDefinitionDto form, IReadOnlyList<BlockDefinitionDto> blocks)> ResolveFormAsync(string id, int version)
    {
        var res = await _http.GetFromJsonAsync<ResolveResponse>($"/forms/definitions/{id}/{version}/resolve");
        if (res is null) throw new InvalidOperationException("Resolve response is null");
        return (res.form, res.blocks);
    }

    // ❗ OPRAVA: plně async (žádné .Result)
    public async Task<FormResponseCreatedDto?> CreateResponseAsync(FormResponseCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("/forms/responses", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FormResponseCreatedDto>();
    }

    private record ResolveResponse(FormDefinitionDto form, List<BlockDefinitionDto> blocks);
    public async Task<List<BlockDefinitionDto>> GetBlocksAsync()
    => await _http.GetFromJsonAsync<List<BlockDefinitionDto>>("/defs/blocks") ?? new();

    public async Task<BlockDefinitionDto> SaveBlockAsync(BlockDefinitionDto dto)
    {
        var resp = await _http.PostAsJsonAsync("/defs/blocks", dto);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<BlockDefinitionDto>())!;
    }
}
