using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop.Views
{
    public partial class BlocksEditorWindow : Window
    {
        private DesignerViewModel VM => (DesignerViewModel)DataContext;

        // Drag state
        private UIElement? _dragElement;
        private Point _dragStart;
        private Point _origPos;
        private int _gridSize = 8;

        public BlocksEditorWindow()
        {
            InitializeComponent();
            DataContext = new DesignerViewModel();

            LoadPalette();
            RefreshGridSizeFromVm();
        }

        // --- GridSize helpers (int/double tolerant) ---
        private PropertyInfo? GridSizeProp()
            => typeof(DesignerViewModel).GetProperty("GridSize");

        private void RefreshGridSizeFromVm()
        {
            try
            {
                var p = GridSizeProp();
                if (p == null) return;
                var v = p.GetValue(VM);
                if (v is int i) _gridSize = i;
                else if (v is double d) _gridSize = (int)Math.Round(d);
            }
            catch { }
        }

        private void SetVmGridSize(int newVal)
        {
            try
            {
                var p = GridSizeProp();
                if (p == null) return;
                if (p.PropertyType == typeof(int)) p.SetValue(VM, newVal);
                else if (p.PropertyType == typeof(double)) p.SetValue(VM, (double)newVal);
                _gridSize = newVal;
            }
            catch { }
        }
        // ----------------------------------------------

        private void LoadPalette()
        {
            var all = BlockLibraryJson.Default.Enumerate().OrderBy(e => e.Title).ToList();
            if (PaletteList != null) PaletteList.ItemsSource = all;
        }

        private void PaletteFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (PaletteFilter?.Text ?? "").Trim();
            var all = BlockLibraryJson.Default.Enumerate().ToList();
            if (!string.IsNullOrWhiteSpace(q))
                all = all.Where(x => (x.Title ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                  || (x.Key ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (PaletteList != null) PaletteList.ItemsSource = all.OrderBy(x => x.Title).ToList();
        }

        private void PaletteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PaletteList?.SelectedItem is BlockLibEntry entry)
            {
                var pos = new Point(40, 40);
                AddBlockVisual(entry, pos);
            }
        }

        private void AddBlockVisual(BlockLibEntry entry, Point pos)
        {
            var b = new Border
            {
                Background = Brushes.AliceBlue,
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Tag = entry
            };
            var tb = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(entry.Title) ? $"{entry.Key} ({entry.Version})" : entry.Title,
                FontWeight = FontWeights.SemiBold
            };
            b.Child = tb;

            if (DesignCanvas != null)
            {
                DesignCanvas.Children.Add(b);
                Canvas.SetLeft(b, Snap(pos.X));
                Canvas.SetTop(b, Snap(pos.Y));
            }

            b.MouseLeftButtonDown += OnBlockMouseDown;
            b.MouseMove += OnBlockMouseMove;
            b.MouseLeftButtonUp += OnBlockMouseUp;
        }

        private double Snap(double v)
        {
            if (_gridSize <= 1) return v;
            var g = _gridSize;
            return Math.Round(v / g) * g;
        }

        private void OnBlockMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragElement = (UIElement)sender;
            _dragStart = e.GetPosition(DesignCanvas ?? (IInputElement)sender);
            _origPos = new Point(Canvas.GetLeft(_dragElement), Canvas.GetTop(_dragElement));
            _dragElement.CaptureMouse();
        }

        private void OnBlockMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragElement == null || e.LeftButton != MouseButtonState.Pressed) return;
            var host = DesignCanvas ?? (sender as UIElement);
            if (host == null) return;
            var p = e.GetPosition(host);
            var dx = p.X - _dragStart.X;
            var dy = p.Y - _dragStart.Y;
            var nx = Snap(_origPos.X + dx);
            var ny = Snap(_origPos.Y + dy);
            Canvas.SetLeft(_dragElement, nx);
            Canvas.SetTop(_dragElement, ny);
        }

        private void OnBlockMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragElement == null) return;
            _dragElement.ReleaseMouseCapture();
            _dragElement = null;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }
        private void Canvas_MouseMove(object sender, MouseEventArgs e) { }

        private void NewBlock_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new NewBlockDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dlg.ShowDialog() == true && dlg.ResultBlock != null)
            {
                VM.NewBlock(dlg.ResultBlock);
            }
        }

        private void SaveJson_OnClick(object sender, RoutedEventArgs e)
        {
            if (VM.CurrentBlock == null)
            {
                MessageBox.Show("Nejprve založte nebo načtěte blok.", "Upozornění",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dto = VM.ExportToDto();
                if (dto == null)
                    throw new InvalidOperationException("Export vrací prázdná data.");

                var jsonIndented = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                using var doc = JsonDocument.Parse(jsonIndented);
                var root = doc.RootElement;

                if (!root.TryGetProperty("BlockId", out var blockIdEl) ||
                    !Guid.TryParse(blockIdEl.GetString(), out var blockId) ||
                    blockId == Guid.Empty)
                    throw new InvalidOperationException("Kořen JSON neobsahuje platný 'BlockId' (GUID).");

                string version = root.TryGetProperty("Version", out var verEl) ? (verEl.GetString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(version)) version = "1.0.0";
                string? key = root.TryGetProperty("Key", out var keyEl) ? keyEl.GetString() : null;
                string? blockName = root.TryGetProperty("BlockName", out var nameEl) ? nameEl.GetString() : null;

                var sfd = new SaveFileDialog
                {
                    Filter = "AGT JSON (*.json)|*.json",
                    FileName = $"{blockId:D}__{version}.json"
                };
                if (sfd.ShowDialog(this) != true) return;

                File.WriteAllText(sfd.FileName, jsonIndented);

                var ok = BlockLibraryJson.Default.SaveToLibrary(blockId, version, root, key, blockName: blockName);
                if (!ok)
                    throw new InvalidOperationException("Zápis do knihovny bloků selhal.");

                VM.StatusText = $"Uloženo: {System.IO.Path.GetFileName(sfd.FileName)}";
                MessageBox.Show("Blok byl úspěšně uložen a zapsán do knihovny.",
                    "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ukládání selhalo:\n{ex.Message}",
                    "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadJson_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "AGT JSON (*.json)|*.json" };
            if (ofd.ShowDialog(this) != true) return;

            try
            {
                var json = File.ReadAllText(ofd.FileName);
                var dto = JsonSerializer.Deserialize<DesignerViewModel.Dto>(json);
                if (dto == null)
                    throw new InvalidOperationException("Soubor neobsahuje platný blok.");

                VM.ImportFromDto(dto);
                VM.StatusText = $"Načteno: {System.IO.Path.GetFileName(ofd.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Soubor nelze načíst:\n{ex.Message}",
                    "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GridPlus_OnClick(object sender, RoutedEventArgs e)
        {
            var newVal = Math.Max(2, _gridSize + 2);
            SetVmGridSize(newVal);
        }

        private void GridMinus_OnClick(object sender, RoutedEventArgs e)
        {
            var newVal = Math.Max(2, _gridSize - 2);
            SetVmGridSize(newVal);
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e) => Close();

        private void AutoLayout_OnClick(object sender, RoutedEventArgs e) => VM.AutoLayout();
    }
}
