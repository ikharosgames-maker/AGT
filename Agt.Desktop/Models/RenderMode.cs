using System;

namespace Agt.Desktop.Models
{
    /// <summary>
    /// Režim vykreslení formulářových prvků:
    /// - Edit: návrhář (drag, resize, úpravy)
    /// - ReadOnly: jen zobrazení hodnot (bez interakce)
    /// - Run: běh v rámci Case (editace hodnot + validace)
    /// </summary>
    public enum RenderMode
    {
        Edit = 0,
        ReadOnly = 1,
        Run = 2
    }
}
