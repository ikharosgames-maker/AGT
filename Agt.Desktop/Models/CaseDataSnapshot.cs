using System;
using System.Collections.Generic;

namespace Agt.Domain.Models
{
    /// <summary>Snapshot hodnot vyplněných uživatelem v rámci jednoho case.</summary>
    public class CaseDataSnapshot
    {
        public Guid CaseId { get; set; }
        public Guid FormVersionId { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }
}
