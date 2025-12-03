using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Agt.Domain.Models;
using Agt.Domain.Repositories;
using Agt.Domain.Abstractions;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Zobrazí publikovaný layout (stage + bloky) a umožní průchod dle rout.
    /// Layout se čte ze souboru layouts/{FormVersionId}.json.
    /// Routy se načtou z repozitáře (pokud je k dispozici) nebo z JSON fallbacku.
    /// </summary>
    public partial class CaseRunWindow : Window
    {
        private readonly Guid _formVersionId;

        // bloky ve stavech
        private readonly HashSet<string> _active = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _done = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);

        // definované routy
        private List<Route> _routes = new();

        // layout stage + bloků
        private LayoutSnapshot _layout = new();

        // mapování blockKey -> UI border
        private readonly Dictionary<string, Border> _blockVisuals =
            new(StringComparer.OrdinalIgnoreCase);

        public CaseRunWindow(Guid formVersionId, IEnumerable<string> startBlockKeys)
        {
            InitializeComponent();

            if (formVersionId == Guid.Empty)
                throw new ArgumentException("formVersionId nesmí být Guid.Empty", nameof(formVersionId));

            _formVersionId = formVersionId;

            // Header: jméno / verze formuláře – aktuálně zobrazujeme pouze verzi
            var sp = App.Services;
            var forms = sp.GetRequiredService<IFormRepository>();
            var fv = forms.GetVersion(formVersionId);

            var header = "Běh případu";
            if (fv != null)
            {
                header = "Form v" + fv.Version;
            }
            HeaderTitle.Text = header;

            // načti routy a layout
            _routes = GetRoutesForVersion(sp, formVersionId);
            _layout = LoadLayout(formVersionId);

            // počáteční aktivní bloky (z MainShellu / jiného volajícího)
            if (startBlockKeys != null)
            {
                foreach (var k in startBlockKeys)
                {
                    if (!string.IsNullOrWhiteSpace(k))
                        _active.Add(k);
                }
            }

            RenderLayout();
            UpdateAllBlockStyles();
        }

        // =================== vykreslení layoutu ===================

        private void RenderLayout()
        {
            StageCanvas.Children.Clear();
            _blockVisuals.Clear();

            // Stage jako rámečky
            foreach (var st in _layout.Stages)
            {
                var gb = new GroupBox
                {
                    Header = st.Title ?? ("Stage " + st.Id.ToString("N")[..8]),
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

            // Bloky (vykreslené nad stagemi)
            foreach (var b in _layout.Blocks)
            {
                if (string.IsNullOrWhiteSpace(b.Key))
                    continue;

                var border = new Border
                {
                    Width = b.Width > 0 ? b.Width : 140,
                    Height = b.Height > 0 ? b.Height : 64,
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4),
                    Background = (Brush)FindResource("AppPanelAltBrush"),
                    BorderBrush = (Brush)FindResource("AppBorderBrush"),
                    BorderThickness = new Thickness(1),
                    ToolTip = b.Title ?? b.Key
                };

                var tb = new TextBlock
                {
                    Text = b.Title ?? b.Key,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(8),
                    Foreground = (Brush)FindResource("AppTextBrush")
                };
                border.Child = tb;

                border.MouseLeftButtonUp += (_, __) => ToggleSelect(b.Key);

                Canvas.SetLeft(border, b.X);
                Canvas.SetTop(border, b.Y);

                StageCanvas.Children.Add(border);
                _blockVisuals[b.Key] = border;
            }
        }

        // =================== ovládání ===================

        private void ToggleSelect(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (_selected.Contains(key))
                _selected.Remove(key);
            else
                _selected.Add(key);

            UpdateBlockStyle(key);
        }

        private void CompleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selected.Count == 0)
            {
                MessageBox.Show(
                    "Vyber alespoň jeden blok k dokončení (klikem na blok).",
                    "Běh případu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // dokonči vybrané
            foreach (var k in _selected.ToArray())
            {
                _active.Remove(k);
                _done.Add(k);
            }

            // rozvinout následníky dle rout (MVP – bez joinů / podmínek)
            foreach (var k in _selected.ToArray())
            {
                var outs = _routes
                    .Where(r => string.Equals(r.FromBlockKey, k, StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.ToBlockKey);

                foreach (var to in outs)
                {
                    if (string.IsNullOrWhiteSpace(to))
                        continue;
                    if (_done.Contains(to))
                        continue;

                    _active.Add(to);
                }
            }

            _selected.Clear();
            UpdateAllBlockStyles();
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            _selected.Clear();
            UpdateAllBlockStyles();
        }

        // =================== styling bloků ===================

        private void UpdateAllBlockStyles()
        {
            foreach (var key in _blockVisuals.Keys.ToList())
            {
                UpdateBlockStyle(key);
            }
        }

        private void UpdateBlockStyle(string key)
        {
            if (!_blockVisuals.TryGetValue(key, out var border))
                return;

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
            // 1) pokus o repo (připravené pro MSSQL/EF)
            try
            {
                var repo = sp.GetService<IRouteRepository>();
                if (repo != null)
                {
                    // preferuj ListByFormVersion(Guid)
                    var mi = repo.GetType().GetMethod("ListByFormVersion", new[] { typeof(Guid) });
                    if (mi != null)
                    {
                        var res = mi.Invoke(repo, new object[] { formVersionId }) as IEnumerable<Route>;
                        if (res != null)
                            return res.ToList();
                    }

                    // fallback: případné ListAll()
                    var miAll = repo.GetType().GetMethod("ListAll", Type.EmptyTypes);
                    if (miAll != null)
                    {
                        var all = miAll.Invoke(repo, Array.Empty<object>()) as IEnumerable<Route>;
                        if (all != null)
                            return all.Where(r => r.FormVersionId == formVersionId).ToList();
                    }
                }
            }
            catch
            {
                // ignoruj – spadneme na JSON fallback
            }

            // 2) JSON fallback
            var dir = Agt.Infrastructure.JsonStore.JsonPaths.Dir("routes");
            Directory.CreateDirectory(dir);

            var list = new List<Route>();

            foreach (var f in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(f);
                    var r = JsonSerializer.Deserialize<Route>(json);
                    if (r != null && r.FormVersionId == formVersionId)
                        list.Add(r);
                }
                catch
                {
                    // ignoruj poškozené záznamy
                }
            }

            return list;
        }

        private static LayoutSnapshot LoadLayout(Guid formVersionId)
        {
            var dir = Agt.Infrastructure.JsonStore.JsonPaths.Dir("layouts");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, formVersionId + ".json");

            var result = new LayoutSnapshot();

            if (!File.Exists(path))
                return result;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;

                // Stages
                if (root.TryGetProperty("Stages", out var sEl) &&
                    sEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var st in sEl.EnumerateArray())
                    {
                        result.Stages.Add(new StageLayout
                        {
                            Id = st.GetProperty("Id").GetGuid(),
                            Title = TryGetString(st, "Title"),
                            X = TryGetDouble(st, "X", 0),
                            Y = TryGetDouble(st, "Y", 0),
                            Width = TryGetDouble(st, "Width", 600),
                            Height = TryGetDouble(st, "Height", 400)
                        });
                    }
                }

                // Blocks
                if (root.TryGetProperty("Blocks", out var bEl) &&
                    bEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var bl in bEl.EnumerateArray())
                    {
                        var key =
                            TryGetString(bl, "InstanceKey")
                            ?? TryGetString(bl, "Key")
                            ?? TryGetString(bl, "DefKey")
                            ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(key))
                            continue;

                        var title = TryGetString(bl, "Title") ?? key;

                        result.Blocks.Add(new BlockLayout
                        {
                            Key = key,
                            Title = title,
                            StageId = bl.GetProperty("StageId").GetGuid(),
                            X = TryGetDouble(bl, "X", 0),
                            Y = TryGetDouble(bl, "Y", 0),
                            Width = TryGetDouble(bl, "Width", 160),
                            Height = TryGetDouble(bl, "Height", 80)
                        });
                    }
                }

                return result;
            }
            catch
            {
                return result;
            }
        }

        private static string? TryGetString(JsonElement el, string propName)
        {
            if (!el.TryGetProperty(propName, out var p))
                return null;

            return p.ValueKind switch
            {
                JsonValueKind.String => p.GetString(),
                JsonValueKind.Number => p.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static double TryGetDouble(JsonElement el, string propName, double defaultValue)
        {
            if (!el.TryGetProperty(propName, out var p))
                return defaultValue;

            if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var d))
                return d;

            if (p.ValueKind == JsonValueKind.String &&
                double.TryParse(p.GetString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out d))
                return d;

            return defaultValue;
        }

        // =================== DTO pro layout snapshot ===================

        private sealed class LayoutSnapshot
        {
            public List<StageLayout> Stages { get; } = new();
            public List<BlockLayout> Blocks { get; } = new();
        }

        private sealed class StageLayout
        {
            public Guid Id { get; set; }
            public string? Title { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }

        private sealed class BlockLayout
        {
            public string Key { get; set; } = string.Empty;
            public string? Title { get; set; }
            public Guid StageId { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }
    }
}
