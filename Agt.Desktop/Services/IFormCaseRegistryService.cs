using System.Text.Json.Nodes;

namespace Agt.Desktop.Services
{
    public sealed record FormPublishPaths(string FormsPath, string FormVersionPath, string LayoutPath);

    public interface IFormCaseRegistryService
    {
        /// Zaregistruje publikovanou verzi do běhového repozitáře (forms, form-versions, layouts)
        /// a vrátí přesné cesty vytvořených souborů. Vyhazuje výjimku při selhání.
        FormPublishPaths RegisterPublished(string formKey, string version, JsonNode editedFormJson);
    }
}
