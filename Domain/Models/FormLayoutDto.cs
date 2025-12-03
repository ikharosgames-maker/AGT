using System;
using System.Collections.Generic;

namespace Agt.Domain.Models
{
    /// <summary>
    /// All-in-one popis layoutu formuláře:
    /// - základní identifikace verze formuláře
    /// - seznam stagí (s pozicí a velikostí)
    /// - seznam hran (přechodů mezi stagemi)
    /// - seznam instancí bloků ve stagích
    /// </summary>
    public sealed class FormLayoutDto
    {
        public string FormKey { get; set; } = string.Empty;
        public Guid FormVersionId { get; set; }

        public List<StageLayoutDto> Stages { get; set; } = new();
        public List<StageRouteDto> StageRoutes { get; set; } = new();
        public List<BlockInstanceDto> Blocks { get; set; } = new();
    }

    public sealed class StageLayoutDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public double X { get; set; }
        public double Y { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }
    }

    public sealed class StageRouteDto
    {
        public Guid Id { get; set; }

        public Guid FromStageId { get; set; }
        public Guid ToStageId { get; set; }

        /// <summary>
        /// Podmínka v JSON podobě – odpovídá StageEdgeVm.ConditionJson.
        /// </summary>
        public string ConditionJson { get; set; } = @"{ ""conditions"": [] }";
    }

    /// <summary>
    /// Jedna instance bloku v konkrétní stage (layout).
    /// </summary>
    public sealed class BlockInstanceDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Odkaz na definici bloku (BlockDefinition).
        /// </summary>
        public Guid BlockId { get; set; }

        public string Version { get; set; } = "1.0.0";
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Do které stage tahle instance patří.
        /// </summary>
        public Guid StageId { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }
    }
}
