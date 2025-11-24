using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Models;

namespace Agt.Desktop.Controls
{
    public partial class FormCanvas : UserControl
    {
        public FormCanvas()
        {
            InitializeComponent();

            // rozumné defaulty
            CanvasWidth = 320;
            CanvasHeight = 200;
            Mode = RenderMode.Edit;
            RuntimeData = new Dictionary<string, object?>();
        }

        #region DP: Items (kolekce polí)

        public IEnumerable<FieldComponentBase>? Items
        {
            get => (IEnumerable<FieldComponentBase>?)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(
                nameof(Items),
                typeof(IEnumerable<FieldComponentBase>),
                typeof(FormCanvas),
                new PropertyMetadata(null));

        #endregion

        #region DP: CanvasWidth / CanvasHeight

        public double CanvasWidth
        {
            get => (double)GetValue(CanvasWidthProperty);
            set => SetValue(CanvasWidthProperty, value);
        }

        public static readonly DependencyProperty CanvasWidthProperty =
            DependencyProperty.Register(
                nameof(CanvasWidth),
                typeof(double),
                typeof(FormCanvas),
                new PropertyMetadata(320d));

        public double CanvasHeight
        {
            get => (double)GetValue(CanvasHeightProperty);
            set => SetValue(CanvasHeightProperty, value);
        }

        public static readonly DependencyProperty CanvasHeightProperty =
            DependencyProperty.Register(
                nameof(CanvasHeight),
                typeof(double),
                typeof(FormCanvas),
                new PropertyMetadata(200d));

        #endregion

        #region DP: Mode

        public RenderMode Mode
        {
            get => (RenderMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(RenderMode),
                typeof(FormCanvas),
                new PropertyMetadata(RenderMode.Edit));

        #endregion

        #region DP: RuntimeData (pro RUN)

        public IDictionary<string, object?> RuntimeData
        {
            get => (IDictionary<string, object?>)GetValue(RuntimeDataProperty);
            set => SetValue(RuntimeDataProperty, value);
        }

        public static readonly DependencyProperty RuntimeDataProperty =
            DependencyProperty.Register(
                nameof(RuntimeData),
                typeof(IDictionary<string, object?>),
                typeof(FormCanvas),
                new PropertyMetadata(new Dictionary<string, object?>()));

        #endregion
    }
}
