using System;
using System.Text.Json;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Rozšiřující metody dovolující volat SaveToLibrary s pojmenovanými argumenty 'key' a 'version',
    /// které mapují na instanční metody očekávající 'blockName' a 'blockVersion'.
    /// </summary>
    public static class BlockLibraryJsonExtensions
    {
        public static BlockLibEntry SaveToLibrary(this BlockLibraryJson lib, string key, string version, string json, string? title = null)
            => lib.SaveToLibrary(blockName: key, blockVersion: version, json: json, title: title);

        public static BlockLibEntry SaveToLibrary(this BlockLibraryJson lib, string key, string version, object value, string? title = null, JsonSerializerOptions? options = null)
            => lib.SaveToLibrary(blockName: key, blockVersion: version, value: value, title: title, options: options);
    }
}
