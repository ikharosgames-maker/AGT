namespace Agt.Contracts
{ 



public record FieldDto(
string Key, string Type, string Label,
bool Required = false, string? Regex = null);


public record UiHintsDto(string? Layout = null);


public record BlockDefinitionDto(
string Id, int Version, string Name,
IReadOnlyList<FieldDto> Fields,
UiHintsDto UiHints);


public record FormBlockRefDto(string Ref, int Version, Dictionary<string, object?>? @Params = null);


public record FormDefinitionDto(
string Id, int Version, string Name,
IReadOnlyList<FormBlockRefDto> Blocks);


// Create request (klient posílá data jako libovolný JSON)
public record FormResponseCreateDto(
string FormId, int FormVersion, System.Text.Json.JsonElement Data, string CreatedBy);


// Odpověď po uložení
public record FormResponseCreatedDto(Guid Id);
}