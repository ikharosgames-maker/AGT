using Agt.Contracts;
using System.Net.Http;
using System.Net.Http.Json;


namespace Agt.Desktop.Services;


public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }


    public async Task<(FormDefinitionDto form, IReadOnlyList<BlockDefinitionDto> blocks)> ResolveFormAsync(string id, int version)
    {
        var res = await _http.GetFromJsonAsync<ResolveResponse>($"/forms/definitions/{id}/{version}/resolve");
        if (res is null) throw new InvalidOperationException("Resolve response is null");
        return (res.form, res.blocks);
    }


    public Task<FormResponseCreatedDto?> CreateResponseAsync(FormResponseCreateDto dto)
    => _http.PostAsJsonAsync("/forms/responses", dto)
    .Result.Content.ReadFromJsonAsync<FormResponseCreatedDto>();


    private record ResolveResponse(FormDefinitionDto form, List<BlockDefinitionDto> blocks);
}