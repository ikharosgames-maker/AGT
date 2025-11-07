using System;
using System.Collections.Generic;

namespace Agt.Domain.Models
{
    public sealed class CaseDataSnapshot
    {
        public Guid CaseId { get; set; }
        public Dictionary<string, object?> Values { get; set; } = new();
        public DateTime SavedUtc { get; set; } = DateTime.UtcNow;
    }
}
