using Agt.Domain.Models;
using Agt.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class MainShell : Window
    {
        public MainShell()
        {
            InitializeComponent();
            DataContext = new ViewModels.CasesDashboardViewModel();
        }
        private void CasesList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (CasesList?.SelectedItem == null) return;

                var item = CasesList.SelectedItem;

                var caseId = GetGuidProp(item, "Id");
                var fvId = GetGuidProp(item, "FormVersionId");
                if (fvId == Guid.Empty) fvId = GetGuidProp(item, "FormVersionID");

                if (caseId == Guid.Empty)
                {
                    MessageBox.Show("Nepodařilo se určit Id případu.", "Otevřít případ",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (fvId == Guid.Empty)
                {
                    // zkusíme dočíst z repo, pokud existuje
                    var sp = Agt.Desktop.App.Services;
                    var casesRepo = sp.GetService<ICaseRepository>();
                    if (casesRepo != null)
                    {
                        var getMi = casesRepo.GetType().GetMethod("Get", new[] { typeof(Guid) });
                        if (getMi != null)
                        {
                            var c = getMi.Invoke(casesRepo, new object[] { caseId }) as Case;
                            if (c != null) fvId = c.FormVersionId;
                        }
                    }
                }
                if (fvId == Guid.Empty)
                {
                    MessageBox.Show("Nepodařilo se určit verzi formuláře pro případ.", "Otevřít případ",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 1) pokus: vzít aktivní bloky z case repa (pokud máme metodu)
                var activeKeys = TryGetActiveBlocks(caseId);

                // 2) fallback: bloky první stage z layoutu
                if (activeKeys.Length == 0)
                    activeKeys = ResolveInitialBlocksFromLayout(fvId);

                var win = new StageRunWindow(fvId, activeKeys, caseId)
                {
                    Owner = Agt.Desktop.App.Current?.MainWindow
                };
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Otevření případu selhalo: " + ex.Message,
                    "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== helpery =====

        private static Guid GetGuidProp(object obj, string name)
        {
            if (obj == null) return Guid.Empty;
            var pi = obj.GetType().GetProperty(name);
            if (pi == null) return Guid.Empty;
            try
            {
                var v = pi.GetValue(obj, null);
                if (v is Guid g) return g;
                Guid parsed;
                return v != null && Guid.TryParse(v.ToString(), out parsed) ? parsed : Guid.Empty;
            }
            catch { return Guid.Empty; }
        }

        /// <summary>
        /// Bezpečný pokus získat aktivní bloky existujícího případu z ICaseRepository.
        /// Žádné Type.GetType ani GetService(null). Když není metoda k dispozici, vrací prázdno.
        /// </summary>
        private static string[] TryGetActiveBlocks(Guid caseId)
        {
            try
            {
                var sp = Agt.Desktop.App.Services;
                var casesRepo = sp.GetService<ICaseRepository>();
                if (casesRepo == null) return Array.Empty<string>();

                // Preferovaná metoda, pokud ji JSON/MSSQL implementace nabídne:
                var mi = casesRepo.GetType().GetMethod("ListActiveBlockKeys", new[] { typeof(Guid) });
                if (mi != null)
                {
                    var res = mi.Invoke(casesRepo, new object[] { caseId }) as System.Collections.IEnumerable;
                    if (res != null)
                    {
                        return res.Cast<object>()
                                  .Select(o => o?.ToString())
                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .ToArray();
                    }
                }

                // Záchranná varianta: načti case a z něj vyčti aktivní, pokud to model umožňuje
                var getMi = casesRepo.GetType().GetMethod("Get", new[] { typeof(Guid) });
                if (getMi != null)
                {
                    var c = getMi.Invoke(casesRepo, new object[] { caseId });
                    if (c != null)
                    {
                        // zkus najít vlastnost ActiveBlockKeys / Active / Tasks apod.
                        foreach (var propName in new[] { "ActiveBlockKeys", "Active", "Tasks" })
                        {
                            var pi = c.GetType().GetProperty(propName);
                            if (pi == null) continue;

                            var val = pi.GetValue(c, null) as System.Collections.IEnumerable;
                            if (val == null) continue;

                            var keys = val.Cast<object>()
                                          .Select(o => o?.ToString())
                                          .Where(s => !string.IsNullOrWhiteSpace(s))
                                          .Distinct(StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
                            if (keys.Length > 0) return keys;
                        }
                    }
                }
            }
            catch
            {
                // ignoruj – vrátíme prázdno a použijeme layout fallback
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Vezme bloky první stage z uloženého layout snapshotu (layouts/{FormVersionId}.json).
        /// </summary>
        private static string[] ResolveInitialBlocksFromLayout(Guid formVersionId)
        {
            try
            {
                var dir = Agt.Infrastructure.JsonStore.JsonPaths.Dir("layouts");
                Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, formVersionId + ".json");
                if (!File.Exists(path)) return Array.Empty<string>();

                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);

                var stages = doc.RootElement.GetProperty("Stages").EnumerateArray().ToList();
                if (stages.Count == 0) return Array.Empty<string>();

                var firstStage = stages
                    .Select(e => new
                    {
                        Id = e.GetProperty("Id").GetGuid(),
                        X = e.TryGetProperty("X", out var x) ? x.GetDouble() : 0.0
                    })
                    .OrderBy(e => e.X)
                    .First().Id;

                // POZOR: bereme "InstanceKey", ne "Key"
                var blocks = doc.RootElement.GetProperty("Blocks").EnumerateArray()
                    .Where(b => b.TryGetProperty("StageId", out var sid) && sid.GetGuid() == firstStage)
                    .Select(b => b.TryGetProperty("InstanceKey", out var ik) ? ik.GetString() : null)
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return blocks;
            }
            catch { return Array.Empty<string>(); }
        }
    }
}
