using Agt.Contracts;


namespace Agt.Desktop.ViewModels;


public record FieldInstance(string BlockKey, FieldDto Field)
{
    public object? Value { get; set; }
}