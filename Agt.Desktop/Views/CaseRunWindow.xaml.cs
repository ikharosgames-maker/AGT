using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Zobrazí publikovaný layout (stage + bloky) a umožní průchod dle rout.
    /// Layout se čte ze souboru layouts/{FormVersionId}.json (viz patch publikace).
    /// Routy se načtou z repozitáře (pokud máte metodu) nebo z JSON fallbacku.
    /// </summary>
    public partial class CaseRunWindow : Window
    {
        private readonly Guid _formVersionId;
        private readonly HashSet<string> _active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<Route> _routes = new List<Route>();
        private readonly LayoutSnapshot _layout = new LayoutSnapshot();

        // mapování: blockKey -> UI prvek
        private readonly Dictionary<string, Border> _blockVisuals = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);

        public CaseRunWindow(Guid formVersionId, IEnumerable<string> startBlockKeys)
        {
            InitializeComponent();

            if (formVersionId == Guid.Empty) throw new ArgumentException("formVersionId nesmí být Guid.Empty", nameof(formVersionId));
            _formVersionId = formVersionId;

            // Header: jméno formuláře + verze
            var sp = Agt.Desktop.App.Services;
            var forms = sp.GetRequiredService<IFormRepository>();
            var fv = forms.GetVersion(formVersionId);
            string header = "Běh případu";
            if (fv != null)
            {
                var f = forms.Get(fv.FormId);
                header = f != null ? (f.Name + "  v" + fv.Version) : ("Form v" + fv.Version);
            }
            HeaderTitle.Text = header;

            // Načti routes
            _routes = GetRoutesForVersion(sp, formVersionId);

            // Načti layout snapshot
            _layout = LoadLayout(formVersionId);

            // Nastav počáteční aktivní
            if (startBlockKeys != null)
            {
                foreach (var k in startBlockKeys) if (!string.IsNullOrWhiteSpace(k)) _active.Add(k);
            }

            // Vykresli
            RenderLayout();
            UpdateAllBlockStyles();
        }

        // =================== vykreslení layoutu ===================

        private void RenderLayout()
        {
            StageCanvas.Children.Clear();
            _blockVisuals.Clear();

            // Stage (jako rámečky)
            foreach (var st in _layout.Stages)
            {
                var gb = new GroupBox
                {
                    Header = st.Title ?? ("Stage " + st.Id.ToString().Substring(0, 8)),
                    BorderBrush = (Brush)FindResource("AppBorderBrush"),
                    BorderThickness = new Thickness(1),
                    Background = (Brush)FindResource("AppPanelAltBrush"),
                    Padding = new Thickness(8),
                    Width = st.Width > 0 ? st.Width : 600,
                    Height = st.Height > 0 ? st.Height : 400
                };
                Canvas.SetLeft(gb, st.X);
                Canvas.SetTop(gb, st.Y);
                StageCanvas.Children.Add(gb);
            }

            // Bloky (uvnitř canvasu, vizuálně nad stagemi)
            foreach (var b in _layout.Blocks)
            {
                var border = new Border
                {
                    Width = b.Width > 0 ? b.Width : 140,
                    Height = b.Height > 0 ? b.Height : 64,
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1),
                    Background = (Brush)FindResource("AppPanelAltBrush"),
                    BorderBrush = (Brush)FindResource("AppBorderBrush"),
                    ToolTip = (b.Title ?? b.Key)
                };

                var tb = new TextBlock
                {
                    Text = (b.Title ?? b.Key),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(8),
                    Foreground = (Brush)FindResource("AppTextBrush")
                };
                border.Child = tb;

                border.MouseLeftButtonUp += (_, __) => ToggleSelect(b.Key);

                Canvas.SetLeft(border, b.X);
                Canvas.SetTop(border, b.Y);
                Panel.SetZIndex(border, 10);
                StageCanvas.Children.Add(border);

                _blockVisuals[b.Key] = border;
            }
        }

        // =================== ovládání ===================

        private void ToggleSelect(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            if (_selected.Contains(key)) _selected.Remove(key);
            else _selected.Add(key);

            UpdateBlockStyle(key);
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            _selected.Clear();
            UpdateAllBlockStyles();
        }

        private void CompleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selected.Count == 0)
            {
                MessageBox.Show("Vyber alespoň jeden blok k dokončení (klikem na blok).", "Běh případu",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // dokonči vybrané
            foreach (var k in _selected.ToArray())
            {
                _active.Remove(k);
                _done.Add(k);
            }

            // rozvinout následníky dle rout (MVP: bez joinů/podmínek)
            foreach (var k in _selected.ToArray())
            {
                var outs = _routes
                    .Where(r => string.Equals(r.FromBlockKey, k, StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.ToBlockKey);

                foreach (var to in outs)
                {
                    if (string.IsNullOrWhiteSpace(to)) continue;
                    if (_done.Contains(to)) continue;
                    _active.Add(to);
                }
            }

            _selected.Clear();
            UpdateAllBlockStyles();
        }

        // =================== styling bloků ===================

        private void UpdateAllBlockStyles()
        {
            foreach (var key in _blockVisuals.Keys.ToList())
                UpdateBlockStyle(key);
        }

        private void UpdateBlockStyle(string key)
        {
            Border border;
            if (!_blockVisuals.TryGetValue(key, out border)) return;

            // základ
            border.Background = (Brush)FindResource("AppPanelAltBrush");
            border.BorderBrush = (Brush)FindResource("AppBorderBrush");
            border.BorderThickness = new Thickness(1);
            border.Opacity = 1.0;

            // stav
            if (_done.Contains(key))
            {
                border.Background = (Brush)FindResource("AppPanelDarkBrush");
                border.Opacity = 0.7;
            }
            else if (_active.Contains(key))
            {
                border.Background = (Brush)FindResource("AppPanelBrush");
                border.BorderBrush = (Brush)FindResource("AppAccentBrush");
                border.BorderThickness = new Thickness(2);
            }

            // výběr
            if (_selected.Contains(key))
            {
                border.BorderBrush = (Brush)FindResource("AppAccentBrush");
                border.BorderThickness = new Thickness(3);
            }
        }

        // =================== načtení rout a layoutu ===================

        private static List<Route> GetRoutesForVersion(IServiceProvider sp, Guid formVersionId)
        {
            // pokus o repo (připravené pro MSSQL/EF)
            try
            {
                var repo = sp.GetService<IRouteRepository>();
                if (repo != null)
                {
                    var mi = repo.GetType().GetMethod("ListByFormVersion", new[] { typeof(Guid) });
                    if (mi != null)
                    {
                        var res = mi.Invoke(repo, new object[] { formVersionId }) as IEnumerable<Route>;
                        if (res != null) return res.ToList();
                    }

                    // fallback přes případné ListAll():
                    var miAll = repo.GetType().GetMethod("ListAll", Type.EmptyTypes);
                    if (miAll != null)
                    {
                        var all = miAll.Invoke(repo, new object[0]) as IEnumerable<Route>;
                        if (all != null) return all.Where(r => r.FormVersionId == formVersionId).ToList();
                    }
                }
            }
            catch { /* ignoruj, spadneme na JSON */ }

            // JSON fallback
            var dir = Agt.Infrastructure.JsonStore.JsonPaths.Dir("routes");
            Directory.CreateDirectory(dir);
            var list = new List<Route>();
            foreach (var f in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(f);
                    var r = JsonSerializer.Deserialize<Route>(json);
                    if (r != null && r.FormVersionId == formVersionId) list.Add(r);
                }
                catch { }
            }
            return list;
        }

        private static LayoutSnapshot LoadLayout(Guid formVersionId)
        {
            var dir = Agt.Infrastructure.JsonStore.JsonPaths.Dir("layouts");
            Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, formVersionId + ".json");
            if (!File.Exists(path)) return new LayoutSnapshot();

            try
            {
                var json = File.ReadAllText(path);
                var snap = JsonSerializer.Deserialize<LayoutSnapshot>(json);
                return snap ?? new LayoutSnapshot();
            }
            catch
            {
                return new LayoutSnapshot();
            }
        }

        // =================== DTO pro layout snapshot ===================

        private sealed class LayoutSnapshot
        {
            public List<StageLayout> Stages { get; set; }
            public List<BlockLayout> Blocks { get; set; }
            public LayoutSnapshot()
            {
                Stages = new List<StageLayout>();
                Blocks = new List<BlockLayout>();
            }
        }

        private sealed class StageLayout
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }

        private sealed class BlockLayout
        {
            public string Key { get; set; }
            public string Title { get; set; }
            public Guid StageId { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }
    }
}
