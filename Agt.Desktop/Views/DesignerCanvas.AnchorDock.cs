using System.Windows.Controls;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Doplňkový partial: vlastnost, kterou volá dokovací/anchor logika během drag.
    /// </summary>
    public partial class DesignerCanvas : UserControl
    {
        /// <summary>
        /// True = během drag/resize potlač chování kotvení/dokování.
        /// </summary>
        public bool ShouldSuspendAnchorDock { get; private set; } = false;
    }
}
