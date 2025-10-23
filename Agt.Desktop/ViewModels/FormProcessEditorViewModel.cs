using Agt.Desktop.Models;
using Agt.Desktop.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Agt.Desktop.ViewModels
{
    public sealed class FormProcessEditorViewModel : ViewModelBase
    {
        private readonly IFormSaveService? _save;
        private readonly IFormCloneService? _clone;
        private readonly IFormCaseRegistryService _registry;

        private readonly string _formsRoot =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", "forms");
        private readonly string _draftsRoot =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", "form-drafts");

        private const double _portOffset = 12.0;

        public GraphVm Graph { get; } = new();
        public ObservableCollection<PaletteItem> Palette { get; } = new();
        public ObservableCollection<string> AvailableUsers { get; } = new();
        public ObservableCollection<string> AvailableGroups { get; } = new();

        private StageVm? _selectedStage;
        public StageVm? SelectedStage { get => _selectedStage; set { _selectedStage = value; Raise(); } }

        private BlockVm? _selectedBlock;
        public BlockVm? SelectedBlock { get => _selectedBlock; set { _selectedBlock = value; Raise(); } }

        private StageEdgeVm? _selectedStageEdge;
        public StageEdgeVm? SelectedStageEdge { get => _selectedStageEdge; set { _selectedStageEdge = value; Raise(); } }

        private string _formKey = "Process";
        public string FormKey { get => _formKey; set { _formKey = value; Raise(); } }

        private JsonNode? _originalBaselineJson;
        public string? OriginalFilePath { get => _originalFilePath; set { _originalFilePath = value; Raise(); } }
        private string? _originalFilePath;

        // Commands
        public ICommand OpenFromRepositoryCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand OpenDraftCommand { get; }
        public ICommand PublishCommand { get; }
        // kompatibilita se starým XAMLEM/handlery
        public ICommand PublishAutoCommand => PublishCommand;

        public FormProcessEditorViewModel()
            : this(
                  Agt.Desktop.App.Services?.GetService(typeof(IFormSaveService)) as IFormSaveService,
                  Agt.Desktop.App.Services?.GetService(typeof(IFormCloneService)) as IFormCloneService,
                  Agt.Desktop.App.Services?.GetService(typeof(IFormCaseRegistryService)) as IFormCaseRegistryService)
        {
            SeedDirectoryDemo();
        }

        public FormProcessEditorViewModel(IFormSaveService save, IFormCloneService clone, IFormCaseRegistryService registry)
        {
            _save = save;
            _clone = clone;
            _registry = registry;

            OpenFromRepositoryCommand = new RelayCommand(_ => OpenFromRepository());
            SaveDraftCommand = new RelayCommand(_ => SaveDraft());
            OpenDraftCommand = new RelayCommand(_ => OpenDraft());

            PublishCommand = new RelayCommand(_ => Publish(), _ => _save != null);
        }

        public void LoadPaletteFromLibrary(IBlockLibrary lib)
        {
            if (lib == null) return;
            var items = lib.Enumerate()
                           .GroupBy(it => it.BlockId.ToString("D") + "|" + it.Version, StringComparer.OrdinalIgnoreCase)
                           .Select(g => g.First())
                           .OrderBy(it => it.Name)
                           .ToList();

            Palette.Clear();
            foreach (var it in items)
                Palette.Add(new PaletteItem(it.BlockId, it.Name, it.Version));
        }


        /// <summary>Vrátí "Stage N" s nejmenším volným N (bere v potaz i "Nová Stage N").</summary>
        // ---- Auto název stage (Title) ----
        private string GetNextStageTitle(string baseTitle = "Nová Stage")
        {
            // Vezmi všechna existující jména
            var titles = Graph.Stages.Select(s => s.Title ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!titles.Contains(baseTitle))
                return baseTitle;

            // Hledej "Nová Stage (2)", "Nová Stage (3)", ...
            int n = 2;
            while (true)
            {
                var candidate = $"{baseTitle} ({n})";
                if (!titles.Contains(candidate))
                    return candidate;
                n++;
            }
        }

        // ---- Zjisti další volnou pozici pro stage (bez překryvu) ----
        private (double X, double Y, double W, double H) GetNextStageRect(double w = 600, double h = 400)
        {
            // Základ a krok (diagonální kaskáda)
            const double startX = 100, startY = 100;
            const double step = 48;   // posun mezi vrstvami

            bool Intersects(double x, double y)
            {
                foreach (var s in Graph.Stages)
                {
                    bool sep = (x + w <= s.X) || (s.X + s.W <= x) || (y + h <= s.Y) || (s.Y + s.H <= y);
                    if (!sep) return true;
                }
                return false;
            }

            // Zkusit navázat na poslední stage, a když to nejde, iterovat kaskádu
            double tryX, tryY;
            if (Graph.Stages.Any())
            {
                var last = Graph.Stages.Last();
                tryX = last.X + step;
                tryY = last.Y + step;
            }
            else
            {
                tryX = startX; tryY = startY;
            }

            // Pokud koliduje, hledej první volné místo v kaskádě
            int tries = 0;
            while (Intersects(tryX, tryY) && tries < 500)
            {
                tryX = startX + (tries * step) % (startX + 8 * step);
                tryY = startY + (tries * step);
                tries++;
            }

            return (tryX, tryY, w, h);
        }
        public StageVm AddStageAuto(double? w = null, double? h = null, string baseTitle = "Nová Stage")
        {
            double ww = Math.Max(200, w ?? 600);
            double hh = Math.Max(150, h ?? 400);

            var title = GetNextStageTitle(baseTitle);
            var (x, y, W, H) = GetNextStageRect(ww, hh);

            var st = new StageVm { Id = Guid.NewGuid(), Title = title, X = x, Y = y, W = W, H = H };
            Graph.Stages.Add(st);
            SelectedStage = st;   // rovnou vyber novou stage
            return st;
        }


        private static int? TryParseSuffixNumber(string prefix, string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;
            var t = title!.Trim();
            if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

            var tail = t.Substring(prefix.Length).Trim();
            if (int.TryParse(tail, out var num) && num > 0) return num;
            return null;
        }

        /// <summary>Najde nejbližší volné XY tak, aby se nová stage (w×h) nepřekrývala s existujícími.</summary>
        public (double X, double Y) FindNextStagePosition(double w, double h, double grid = 20, double margin = 24)
        {
            double startX = 100, startY = 100;

            // Začni „pod a vpravo“ od naposledy přidané
            if (Graph.Stages.Any())
            {
                var last = Graph.Stages.Last();
                startX = last.X + 48;
                startY = last.Y + 48;
            }

            double Snap(double v) => grid <= 0 ? v : Math.Round(v / grid) * grid;

            bool Overlaps(double x, double y)
            {
                foreach (var s in Graph.Stages)
                {
                    if (RectsOverlap(
                        x, y, w, h,
                        s.X - margin, s.Y - margin, s.W + 2 * margin, s.H + 2 * margin))
                        return true;
                }
                return false;
            }

            // 1) rychlá diagonální kaskáda (8 pokusů)
            for (int i = 0; i < 8; i++)
            {
                var x = Snap(startX + i * 48);
                var y = Snap(startY + i * 48);
                if (!Overlaps(x, y)) return (x, y);
            }

            // 2) spirála po gridu
            int radius = (int)grid;
            for (int step = 0; step < 200; step++, radius += (int)grid)
            {
                // horní/dolní řada
                for (int dx = -radius; dx <= radius; dx += (int)grid)
                {
                    var x1 = Snap(startX + dx);
                    var y1 = Snap(startY - radius);
                    if (!Overlaps(x1, y1)) return (x1, y1);

                    var x2 = Snap(startX + dx);
                    var y2 = Snap(startY + radius);
                    if (!Overlaps(x2, y2)) return (x2, y2);
                }
                // levý/pravý sloupec
                for (int dy = -radius + (int)grid; dy <= radius - (int)grid; dy += (int)grid)
                {
                    var x1 = Snap(startX - radius);
                    var y1 = Snap(startY + dy);
                    if (!Overlaps(x1, y1)) return (x1, y1);

                    var x2 = Snap(startX + radius);
                    var y2 = Snap(startY + dy);
                    if (!Overlaps(x2, y2)) return (x2, y2);
                }
            }

            // fallback
            return (Snap(startX), Snap(startY));
        }

        private static bool RectsOverlap(double x1, double y1, double w1, double h1,
                                         double x2, double y2, double w2, double h2)
        {
            return !(x1 + w1 <= x2 || x2 + w2 <= x1 || y1 + h1 <= y2 || y2 + h2 <= y1);
        }
        /// <summary>Vrátí "Stage N" s nejmenším volným N (bere v potaz i "Nová Stage N").</summary>
        public string GetNextStageName(string prefix)
        {
            var used = new HashSet<int>();

            // stejné prefixy
            foreach (var s in Graph.Stages)
            {
                var n = TryParseSuffixNumber(prefix, s.Title);
                if (n.HasValue) used.Add(n.Value);
            }

            // kompatibilita: pokud prefix == "Stage", zahrň i "Nová Stage"
            if (prefix.Equals("Stage", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var s in Graph.Stages)
                {
                    var n = TryParseSuffixNumber("Nová Stage", s.Title);
                    if (n.HasValue) used.Add(n.Value);
                }
            }

            int k = 1;
            while (used.Contains(k)) k++;
            return $"{prefix} {k}";
        }


        private void SeedDirectoryDemo()
        {
            foreach (var u in new[]
            {
                "jan.novak","petr.svoboda","karel.dvorak","eva.prochazkova",
                "lucie.kralova","martin.horak","hana.mala","ondrej.cerny",
                "tomas.pospisil","iva.kucerova"
            }) AvailableUsers.Add(u);

            foreach (var g in new[]
            {
                "QC-Operators","QC-Leads","Production-ShiftA","Production-ShiftB",
                "Engineering-Design","Engineering-Process","Logistics","Warehouse"
            }) AvailableGroups.Add(g);
        }

        // ================= Import / Export =================

        /// <summary>
        /// Načte buď (A) náš export se Stages/Routes, nebo (B) layout uložený zvlášť: { Form, Stages, StageRoutes, Blocks }.
        /// Schémata bloků se berou z BlockLibraryJson podle Key+Version (ne z form JSONu).
        /// </summary>
        public void ImportFromJsonNode(JsonNode json)
        {
            _originalBaselineJson = json?.DeepClone();
            OriginalFilePath = null;

            // form key – zkus více polí
            FormKey = FindFirstString(json, "Key", "FormKey", "Id", "FormId") ?? "Process";

            Graph.Stages.Clear();
            Graph.StageEdges.Clear();

            // (A) Editor export – Stages + Routes
            if (json?["Stages"] is JsonArray stagesA)
            {
                ImportShapeA(json, stagesA);
                return;
            }

            // (B) Layout uložený zvlášť – Form, Stages, StageRoutes, Blocks
            if (json?["Blocks"] is JsonArray blocksB)
            {
                ImportShapeB(json, blocksB);
                return;
            }

            // nic z toho -> přátelská hláška
            var keys = (json as JsonObject)?.Select(kv => kv.Key).ToArray() ?? Array.Empty<string>();
            MessageBox.Show(
                "Načítaný JSON neobsahuje očekávanou strukturu.\n" +
                "Hledám buď:  A) 'Stages' + 'Routes',  nebo  B) 'Blocks' (+ volitelně 'Stages','StageRoutes').\n" +
                $"Kořenová pole v souboru: {string.Join(", ", keys)}",
                "Načítání", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ImportShapeA(JsonNode json, JsonArray stagesNode)
        {
            var map = new Dictionary<string, StageVm>(StringComparer.OrdinalIgnoreCase);

            foreach (var sNode in stagesNode.OfType<JsonObject>())
            {
                var st = new StageVm
                {
                    Id = TryGuid(sNode.TryGet("Id")) ?? Guid.NewGuid(),
                    Title = sNode.TryGetString("Title") ?? "Stage",
                    X = sNode.TryGetDouble("X") ?? 100,
                    Y = sNode.TryGetDouble("Y") ?? 100,
                    W = Math.Max(200, sNode.TryGetDouble("Width") ?? 600),
                    H = Math.Max(150, sNode.TryGetDouble("Height") ?? 400)
                };
                Graph.Stages.Add(st);
                map[st.Id.ToString()] = st;

                if (sNode["Blocks"] is JsonArray blocks)
                    AddBlocksIntoStageFromNode(st, blocks);
            }

            // routes
            if (json?["Routes"] is JsonArray routesNode)
            {
                foreach (var r in routesNode.OfType<JsonObject>())
                {
                    var from = r.TryGetString("FromStageId");
                    var to = r.TryGetString("ToStageId");
                    if (from != null && to != null &&
                        map.TryGetValue(from, out var sFrom) &&
                        map.TryGetValue(to, out var sTo))
                    {
                        Graph.StageEdges.Add(new StageEdgeVm
                        {
                            Id = Guid.NewGuid(),
                            FromStageId = sFrom.Id,
                            ToStageId = sTo.Id,
                            ConditionJson = r.TryGetString("Condition") ?? @"{ ""conditions"": [] }"
                        });
                    }
                }
            }
        }

        private void ImportShapeB(JsonNode json, JsonArray blocksNode)
        {
            // Stages: buď jsou definované, nebo vytvoříme 1 defaulťák.
            var map = new Dictionary<string, StageVm>(StringComparer.OrdinalIgnoreCase);

            if (json?["Stages"] is JsonArray stagesB && stagesB.Count > 0)
            {
                foreach (var sNode in stagesB.OfType<JsonObject>())
                {
                    var st = new StageVm
                    {
                        Id = TryGuid(sNode.TryGet("Id")) ?? Guid.NewGuid(),
                        Title = sNode.TryGetString("Title") ?? "Stage",
                        X = sNode.TryGetDouble("X") ?? 100,
                        Y = sNode.TryGetDouble("Y") ?? 100,
                        W = Math.Max(200, sNode.TryGetDouble("Width") ?? 600),
                        H = Math.Max(150, sNode.TryGetDouble("Height") ?? 400)
                    };
                    Graph.Stages.Add(st);
                    map[st.Id.ToString()] = st;
                }
            }
            else
            {
                var st = new StageVm { Id = Guid.NewGuid(), Title = "Stage 1", X = 100, Y = 100, W = 800, H = 600 };
                Graph.Stages.Add(st);
                map[st.Id.ToString()] = st;
            }

            // Blocks: mají StageId; když StageId chybí, padnou do první stage s automatickým rozložením
            var firstStage = Graph.Stages.First();

            foreach (var bNode in blocksNode.OfType<JsonObject>())
            {
                // BlockId je kanon. Ber explicitní "BlockId"; pokud není, ale "Key" je GUID, bereme ho jako BlockId.
                // Pokud není ani jedno – blok přeskočíme (striktní režim).
                var blockIdStr = FirstNonEmpty(
                    bNode.TryGetString("BlockId"),
                    bNode.TryGetString("Key")); // legacy

                if (!Guid.TryParse(blockIdStr, out var blockId))
                    continue; // striktní: žádný fallback

                var ver = FirstNonEmpty(
                    bNode.TryGetString("Version"),
                    bNode.TryGetString("BlockVersion"),
                    bNode.TryGetString("SchemaVersion"))?.Trim();

                var title = FirstNonEmpty(bNode.TryGetString("Title"), bNode.TryGetString("Name")) ?? "";


                var stageIdStr = FirstNonEmpty(
                    bNode.TryGetString("StageId"), bNode.TryGetString("StageID"), bNode.TryGetString("Stage"));

                // pozice z layoutu, nebo autoumistění
                var hasXY = bNode.TryGetDouble("X").HasValue || bNode.TryGetDouble("Y").HasValue;
                var x = bNode.TryGetDouble("X") ?? 20;
                var y = bNode.TryGetDouble("Y") ?? 20;

                StageVm target = firstStage;
                if (!string.IsNullOrWhiteSpace(stageIdStr) && map.TryGetValue(stageIdStr!, out var sFound))
                    target = sFound;

                // vytvoř block
                var b = new BlockVm
                {
                    Id = TryGuid(bNode.TryGet("Id")) ?? Guid.NewGuid(),
                    BlockId = blockId,
                    Title = title,
                    Version = string.IsNullOrWhiteSpace(ver) ? "1.0.0" : ver!,
                };

                // náhled určím ze schématu (rozměry z Items)
                GeneratePreview(b);

                // fallback pozicování
                if (!hasXY)
                {
                    var next = GetNextBelowLast(target, left: 12, margin: 12);
                    x = next.X; y = next.Y;
                }

                b.X = x;
                b.Y = y;

                target.Blocks.Add(b);
            }

            // Stage routes
            var routesNode = json?["StageRoutes"] as JsonArray;
            if (routesNode != null)
            {
                foreach (var r in routesNode.OfType<JsonObject>())
                {
                    var from = r.TryGetString("FromStageId");
                    var to = r.TryGetString("ToStageId");
                    if (from != null && to != null &&
                        map.TryGetValue(from, out var sFrom) &&
                        map.TryGetValue(to, out var sTo))
                    {
                        Graph.StageEdges.Add(new StageEdgeVm
                        {
                            Id = Guid.NewGuid(),
                            FromStageId = sFrom.Id,
                            ToStageId = sTo.Id,
                            ConditionJson = r.TryGetString("Condition") ?? @"{ ""conditions"": [] }"
                        });
                    }
                }
            }
        }

        private static string? FirstNonEmpty(params string?[] vals)
            => vals.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        private static Guid? TryGuid(JsonNode? node)
            => Guid.TryParse(node?.ToString(), out var g) ? g : null;

        private static string? FindFirstString(JsonNode? node, params string[] names)
        {
            if (node is null) return null;
            var q = new Queue<JsonNode>();
            q.Enqueue(node);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur is JsonObject o)
                {
                    foreach (var kv in o)
                    {
                        if (names.Any(n => string.Equals(n, kv.Key, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (kv.Value is JsonValue v && v.TryGetValue<string>(out var s))
                                return s;
                        }
                        if (kv.Value is JsonObject or JsonArray) q.Enqueue(kv.Value!);
                    }
                }
                else if (cur is JsonArray arr)
                {
                    foreach (var el in arr) if (el is JsonObject or JsonArray) q.Enqueue(el!);
                }
            }
            return null;
        }

        private void AddBlocksIntoStageFromNode(StageVm st, JsonArray blocks)
        {
            foreach (var bNode in blocks.OfType<JsonObject>())
            {
                // BlockId je kanon. Ber explicitní "BlockId"; pokud není, ale "Key" je GUID, bereme ho jako BlockId.
                // Pokud není ani jedno – blok přeskočíme (striktní režim).
                var blockIdStr = FirstNonEmpty(
                    bNode.TryGetString("BlockId"),
                    bNode.TryGetString("Key")); // legacy

                if (!Guid.TryParse(blockIdStr, out var blockId))
                    continue; // striktní: žádný fallback

                var ver = FirstNonEmpty(
                    bNode.TryGetString("Version"),
                    bNode.TryGetString("BlockVersion"),
                    bNode.TryGetString("SchemaVersion"))?.Trim();

                var title = FirstNonEmpty(bNode.TryGetString("Title"), bNode.TryGetString("Name")) ?? "";

                var b = new BlockVm
                {
                    Id = TryGuid(bNode.TryGet("Id")) ?? Guid.NewGuid(),
                    BlockId = blockId,
                    Title = title,
                    Version = string.IsNullOrWhiteSpace(ver) ? "1.0.0" : ver!,
                    X = bNode.TryGetDouble("X") ?? 20,
                    Y = bNode.TryGetDouble("Y") ?? 20,
                };

                // náhled (vypočítá i PreviewWidth/Height dle schema Items)
                GeneratePreview(b);

                // pokud JSON měl i uložené rozměry, respektuj min. hranici
                b.PreviewWidth = Math.Max(b.PreviewWidth, bNode.TryGetDouble("Width") ?? b.PreviewWidth);
                b.PreviewHeight = Math.Max(b.PreviewHeight, bNode.TryGetDouble("Height") ?? b.PreviewHeight);

                st.Blocks.Add(b);
            }
        }

        private JsonNode ExportFormAsJsonNode()
        {
            var root = new JsonObject
            {
                ["Key"] = FormKey,
                ["Metadata"] = new JsonObject { ["ExportedUtc"] = DateTime.UtcNow }
            };

            var stages = new JsonArray();
            foreach (var s in Graph.Stages)
            {
                var arr = new JsonArray();
                foreach (var b in s.Blocks)
                {
                    arr.Add(new JsonObject
                    {
                        ["Id"] = b.Id.ToString(),
                        ["BlockId"] = b.BlockId.ToString(),
                        ["Title"] = b.Title,
                        ["Version"] = string.IsNullOrWhiteSpace(b.Version) ? "1.0.0" : b.Version,
                        ["X"] = b.X,
                        ["Y"] = b.Y,
                        ["Width"] = b.PreviewWidth,
                        ["Height"] = b.PreviewHeight
                    });

                }

                stages.Add(new JsonObject
                {
                    ["Id"] = s.Id.ToString(),
                    ["Title"] = s.Title,
                    ["X"] = s.X,
                    ["Y"] = s.Y,
                    ["Width"] = s.W,
                    ["Height"] = s.H,
                    ["Blocks"] = arr
                });
            }
            root["Stages"] = stages;

            var routes = new JsonArray();
            foreach (var e in Graph.StageEdges)
                routes.Add(new JsonObject { ["FromStageId"] = e.FromStageId.ToString(), ["ToStageId"] = e.ToStageId.ToString(), ["Condition"] = e.ConditionJson });
            root["Routes"] = routes;

            return root;
        }

        // =============== Otevření / Drafty / Publikace ===============

        private void OpenFromRepository()
        {
            var win = new Agt.Desktop.Views.FormRepositoryBrowserWindow();
            win.Owner = Agt.Desktop.App.Current?.MainWindow;
            if (win.ShowDialog() == true)
            {
                var json = win.SelectedFormJson;
                var key = win.SelectedFormKey ?? "Process";

                if (json != null)
                {
                    FormKey = key;
                    ImportFromJsonNode(json);
                    MessageBox.Show($"Načten formulář „{FormKey}“.", "Otevřít", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Soubor se nepodařilo načíst (prázdný / neplatný JSON).", "Otevřít", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveDraft()
        {
            try
            {
                Directory.CreateDirectory(_draftsRoot);
                var json = ExportFormAsJsonNode();

                var file = Path.Combine(_draftsRoot, $"{FormKey}__draft_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json");
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(file, json.ToJsonString(opts));

                MessageBox.Show($"Pracovní verze uložena:\n{file}", "Draft", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uložení pracovní verze selhalo: " + ex.Message, "Draft", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDraft()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Otevřít pracovní verzi",
                Filter = "Draft JSON (*.json)|*.json",
                InitialDirectory = Directory.Exists(_draftsRoot) ? _draftsRoot : null,
                Multiselect = false
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var text = File.ReadAllText(dlg.FileName);
                var node = JsonNode.Parse(text);
                if (node == null) throw new InvalidOperationException("Soubor je prázdný nebo neplatný JSON.");
                ImportFromJsonNode(node);
                MessageBox.Show("Pracovní verze načtena.", "Draft", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Načtení pracovní verze selhalo: " + ex.Message, "Draft", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Publish()
        {
            if (_save == null)
            {
                MessageBox.Show("Služba publikace není k dispozici.", "Publikovat",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (_originalBaselineJson == null)
                _originalBaselineJson = new JsonObject { ["Key"] = FormKey };

            var edited = ExportFormAsJsonNode();

            try
            {
                // každé použití dostane VLASTNÍ KOPII, jinak vzniká „node has already a parent“
                var editedForSave = edited.DeepClone();
                var editedForRegistry = edited.DeepClone();

                // 1) DESIGN uložení (mimo runtime)
                var designRoot = GetDir("design-forms");
                var newPath = _save.SaveNextVersionFromJson(
                    formsRoot: designRoot,
                    formKey: FormKey,
                    original: _originalBaselineJson,
                    edited: editedForSave,
                    out var newVer);

                _originalBaselineJson = edited.DeepClone();
                OriginalFilePath = newPath;

                // 2) REGISTRACE pro běh
                if (_registry == null)
                {
                    MessageBox.Show(
                        "Publikace proběhla, ale chybí registrace pro Case (IFormCaseRegistryService). " +
                        "Formulář nebude ve výběru pro spuštění.",
                        "Publikace – varování", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var paths = _registry.RegisterPublished(FormKey, newVer, editedForRegistry);

                MessageBox.Show(
                    $"Publikováno: {FormKey} v{newVer}\n\n" +
                    $"Design:        {newPath}\n" +
                    $"forms:         {paths.FormsPath}\n" +
                    $"form-versions: {paths.FormVersionPath}\n" +
                    $"layouts:       {paths.LayoutPath}",
                    "Publikace OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Publikace selhala: " + ex.Message, "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper – použij stejný jako dřív; pokud ho v souboru už máš, duplicitně NEPŘIDÁVEJ
        private static string GetDir(string name)
        {
            try
            {
                var t = Type.GetType("Agt.Infrastructure.JsonStore.JsonPaths, Agt.Infrastructure");
                var mi = t?.GetMethod("Dir", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var r = mi?.Invoke(null, new object?[] { name }) as string;
                if (!string.IsNullOrWhiteSpace(r))
                {
                    System.IO.Directory.CreateDirectory(r!);
                    return r!;
                }
            }
            catch { }
            var fallback = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", name);
            System.IO.Directory.CreateDirectory(fallback);
            return fallback;
        }

        // ================== Editor API ==================
        // ================== Editor API ==================
        public StageVm AddStage(string title, double x, double y, double w, double h)
        {
            var st = new StageVm { Id = Guid.NewGuid(), Title = title, X = x, Y = y, W = w, H = h };
            Graph.Stages.Add(st);
            return st;
        }


        public BlockVm AddBlock(StageVm stage, Guid blockId, string title, string version, double x, double y)
        {
            var b = new BlockVm { Id = Guid.NewGuid(), BlockId = blockId, Title = title, Version = version, X = x, Y = y };
            stage.Blocks.Add(b);
            return b;
        }


        public StageVm? FindStage(Guid id) => Graph.Stages.FirstOrDefault(s => s.Id == id);

        public (double X, double Y) GetStageOutPortAbs(StageVm s)
            => (s.X + s.W - _portOffset, s.Y + s.H / 2.0);

        public (double X, double Y) GetStageInPortAbs(StageVm s)
            => (s.X + _portOffset, s.Y + s.H / 2.0);

        public StageVm? HitTestStage(System.Windows.Point p, double tolerance)
        {
            foreach (var st in Graph.Stages)
            {
                var body = new System.Windows.Rect(st.X, st.Y, st.W, st.H);
                body.Inflate(tolerance, tolerance);
                if (body.Contains(p)) return st;
            }
            return null;
        }

        public double Snap(double v, double grid, bool noSnap) => (noSnap || grid <= 0) ? v : Math.Round(v / grid) * grid;

        public void SnapAll(double grid)
        {
            foreach (var st in Graph.Stages)
            {
                st.X = Snap(st.X, grid, false); st.Y = Snap(st.Y, grid, false);
                st.W = Math.Max(200, Snap(st.W, grid, false)); st.H = Math.Max(150, Snap(st.H, grid, false));
                foreach (var b in st.Blocks)
                {
                    b.X = Snap(b.X, grid, false); b.Y = Snap(b.Y, grid, false);
                }
            }
        }

        public (double X, double Y) GetNextBlockPosition(StageVm st, double baseX, double baseY)
        {
            var x = baseX; var y = baseY;
            while (st.Blocks.Any(b => Math.Abs(b.X - x) < 10 && Math.Abs(b.Y - y) < 10))
            {
                x += 30; y += 20;
                if (x + 260 > st.W) { x = baseX; y += 30; }
                if (y + 140 > st.H) { x = baseY + 40; y = baseY + 40; break; }
            }
            return (Snap(x, 10, false), Snap(y, 10, false));
        }

        public void ClearSelection()
        {
            foreach (var s in Graph.Stages) s.IsSelected = false;
            foreach (var s in Graph.Stages) foreach (var b in s.Blocks) b.IsSelected = false;
            SelectedBlock = null; SelectedStageEdge = null; SelectedStage = null;
        }

        public void SelectStage(StageVm? s)
        {
            foreach (var st in Graph.Stages) st.IsSelected = false;
            if (s != null) s.IsSelected = true;
            SelectedStage = s; SelectedBlock = null; SelectedStageEdge = null;
            Raise(nameof(Graph));
        }

        public void SelectBlock(BlockVm? b)
        {
            foreach (var st in Graph.Stages) foreach (var bb in st.Blocks) bb.IsSelected = false;
            if (b != null) b.IsSelected = true;
            SelectedBlock = b; SelectedStage = null; SelectedStageEdge = null;
            Raise(nameof(Graph));
        }

        public void SelectEdge(StageEdgeVm? e)
        {
            SelectedStageEdge = e; SelectedBlock = null; SelectedStage = null;
            foreach (var st in Graph.Stages) foreach (var bb in st.Blocks) bb.IsSelected = false;
            foreach (var st in Graph.Stages) st.IsSelected = false;
            Raise(nameof(SelectedStageEdge));
        }

        public void AddStageEdge(StageVm from, StageVm to)
            => Graph.StageEdges.Add(new StageEdgeVm { Id = Guid.NewGuid(), FromStageId = from.Id, ToStageId = to.Id, ConditionJson = @"{ ""conditions"": [] }" });

        public void ClampBlocksInside(StageVm st, double blockW, double blockH, double header)
        {
            foreach (var b in st.Blocks) ClampBlockInside(b, st, blockW, blockH, header);
        }

        public void ClampBlockInside(BlockVm b, StageVm st, double blockW, double blockH, double header)
        {
            if (b == null || st == null) return;
            const double innerPad = 4;
            b.X = Math.Max(innerPad, Math.Min(b.X, st.W - blockW - innerPad));
            b.Y = Math.Max(header + innerPad, Math.Min(b.Y, st.H - header - blockH - innerPad));
        }

        private static bool RectIntersects(double x, double y, double w, double h, BlockVm other)
        {
            return !(x + w <= other.X ||
                     other.X + other.PreviewWidth <= x ||
                     y + h <= other.Y ||
                     other.Y + other.PreviewHeight <= y);
        }

        private static double SnapTo(double v, double grid) => grid <= 0 ? v : Math.Round(v / grid) * grid;

        public (double X, double Y) FindNearestFreeSlot(StageVm st, double targetX, double targetY,
                                                        double blockW, double blockH,
                                                        double grid, double header,
                                                        int maxIterations = 200)
        {
            double baseX = SnapTo(targetX, grid);
            double baseY = SnapTo(targetY, grid);

            const double innerPad = 4;
            baseX = Math.Max(innerPad, Math.Min(baseX, st.W - blockW - innerPad));
            baseY = Math.Max(header + innerPad, Math.Min(baseY, st.H - header - blockH - innerPad));

            bool IsFree(double x, double y)
            {
                foreach (var other in st.Blocks)
                    if (RectIntersects(x, y, blockW, blockH, other)) return false;
                return true;
            }
            if (IsFree(baseX, baseY)) return (baseX, baseY);

            var step = Math.Max(1, (int)(grid > 0 ? grid : 8));
            int radius = step;
            int iter = 0;

            while (iter < maxIterations && radius < 4000)
            {
                for (int dx = -radius; dx <= radius; dx += step)
                {
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY - radius, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(header + innerPad, Math.Min(y, st.H - header - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY + radius, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(header + innerPad, Math.Min(y, st.H - header - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                }
                for (int dy = -radius + step; dy <= radius - step; dy += step)
                {
                    {
                        double x = SnapTo(baseX - radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(header + innerPad, Math.Min(y, st.H - header - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                    {
                        double x = SnapTo(baseX + radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(header + innerPad, Math.Min(y, st.H - header - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                }
                radius += step;
            }

            return (baseX, baseY);
        }

        private static bool Intersects(BlockVm a, BlockVm b) =>
            !(a.X + a.PreviewWidth <= b.X ||
              b.X + b.PreviewWidth <= a.X ||
              a.Y + a.PreviewHeight <= b.Y ||
              b.Y + b.PreviewHeight <= a.Y);

        private void ResolveBlockOverlaps(BlockVm moved, StageVm st, double grid, double headerHeight)
        {
            for (int iter = 0; iter < 20; iter++)
            {
                var hit = st.Blocks.FirstOrDefault(b => !ReferenceEquals(b, moved) && Intersects(moved, b));
                if (hit == null) break;

                double dxRight = (hit.X + hit.PreviewWidth) - moved.X;
                double dxLeft = (moved.X + moved.PreviewWidth) - hit.X;
                double dyDown = (hit.Y + hit.PreviewHeight) - moved.Y;
                double dyUp = (moved.Y + moved.PreviewHeight) - hit.Y;

                var best = new (double dx, double dy)[] {
                    ( +dxRight + 1, 0 ),
                    ( -(dxLeft)  - 1, 0 ),
                    ( 0, +dyDown + 1 ),
                    ( 0, -(dyUp)  - 1 )
                }.OrderBy(v => Math.Abs(v.dx) + Math.Abs(v.dy)).First();

                moved.X += best.dx;
                moved.Y += best.dy;

                moved.X = SnapTo(moved.X, grid);
                moved.Y = SnapTo(moved.Y, grid);

                ClampBlockInside(moved, st, moved.PreviewWidth, moved.PreviewHeight, headerHeight);
            }
        }

        private void AutoGrowStageIfNeeded(StageVm st, BlockVm b, double headerHeight)
        {
            double requiredW = b.X + b.PreviewWidth + 16;
            double requiredH = b.Y + b.PreviewHeight + 42 + headerHeight;
            if (requiredW > st.W) st.W = requiredW;
            if (requiredH > st.H) st.H = requiredH;
        }

        public void MoveBlockTo(BlockVm b, StageVm st, double targetX, double targetY, double grid, double headerHeight)
        {
            if (b == null || st == null) return;
            b.X = targetX; b.Y = targetY;
            ClampBlockInside(b, st, b.PreviewWidth, b.PreviewHeight, headerHeight);
            b.X = SnapTo(b.X, grid); b.Y = SnapTo(b.Y, grid);
            ResolveBlockOverlaps(b, st, grid, headerHeight);
            AutoGrowStageIfNeeded(st, b, headerHeight);
        }

        public (double X, double Y) GetNextBelowLast(StageVm st, double left = 8, double margin = 12)
        {
            if (st.Blocks.Count == 0) return (left, margin);
            var last = st.Blocks.OrderByDescending(b => b.Y + b.PreviewHeight).First();
            var y = last.Y + last.PreviewHeight + margin;
            return (left, y);
        }

        // ====== Preview z knihovny ======
        public void GeneratePreview(BlockVm b)
        {
            b.Components.Clear();

            if (!BlockLibraryJson.Default.TryLoadByIdVersion(b.BlockId, b.Version, out var doc, out _)
                || doc == null)
            {
                b.PreviewWidth = 200;
                b.PreviewHeight = 80;
                return;
            }

            try
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("BlockName", out var bn) && string.IsNullOrWhiteSpace(b.Title))
                    b.Title = bn.GetString() ?? b.Title;

                if (!root.TryGetProperty("Items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                {
                    b.PreviewWidth = Math.Max(b.PreviewWidth, 180);
                    b.PreviewHeight = Math.Max(b.PreviewHeight, 80);
                    return;
                }

                double maxRight = 0, maxBottom = 0;
                foreach (var it in itemsEl.EnumerateArray())
                {
                    double x = it.TryGetProperty("X", out var _x) && _x.TryGetDouble(out var ddx) ? ddx : 0;
                    double y = it.TryGetProperty("Y", out var _y) && _y.TryGetDouble(out var ddy) ? ddy : 0;
                    double w = it.TryGetProperty("Width", out var _w) && _w.TryGetDouble(out var ddw) ? ddw : 120;
                    double h = it.TryGetProperty("Height", out var _h) && _h.TryGetDouble(out var ddh) ? ddh : 28;
                    int z = it.TryGetProperty("ZIndex", out var _z) && _z.TryGetInt32(out var dzi) ? dzi : 0;

                    var typeKey = (it.TryGetProperty("TypeKey", out var _tk) ? _tk.GetString() : null) ?? "";
                    var label = (it.TryGetProperty("Label", out var _lb) ? _lb.GetString() : null) ?? "";

                    FieldComponentBase component = typeKey.ToLowerInvariant() switch
                    {
                        "label" => new LabelField { Label = label, Width = w, Height = h },
                        "textbox" => new TextBoxField { Label = label, Width = w, Height = h },
                        "textarea" => new TextAreaField { Label = label, Width = w, Height = h },
                        "number" => new NumberField { Label = label, Width = w, Height = h },
                        "date" => new DateField { Label = label, Width = w, Height = h },
                        "combobox" => new ComboBoxField { Label = label, Width = w, Height = h },
                        "checkbox" => new CheckBoxField { Label = label, Width = w, Height = h },
                        _ => new LabelField { Label = string.IsNullOrWhiteSpace(label) ? $"[{typeKey}]" : label, Width = w, Height = h }
                    };

                    component.X = x; component.Y = y; component.ZIndex = z;
                    component.TotalWidth = w; component.TotalHeight = h;

                    b.Components.Add(component);

                    maxRight = Math.Max(maxRight, x + component.TotalWidth);
                    maxBottom = Math.Max(maxBottom, y + component.TotalHeight);
                }

                b.PreviewWidth = Math.Max(1, Math.Ceiling(maxRight) + 1);
                b.PreviewHeight = Math.Max(1, Math.Ceiling(maxBottom) + 1);
            }
            finally { doc.Dispose(); }
        }

        // ===== helpers: schema loading & JSON =====

        private static string SanitizeKey(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var trimmed = s.Trim();
            var chars = trimmed.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var res = new string(chars);
            while (res.Contains("__")) res = res.Replace("__", "_");
            return res.Trim('_');
        }

        private static int CompareSemVer(string a, string b)
        {
            static int[] Parse(string v)
            {
                var parts = v.TrimStart('v', 'V').Split('.', StringSplitOptions.RemoveEmptyEntries);
                var arr = new int[3];
                for (int i = 0; i < Math.Min(3, parts.Length); i++)
                    arr[i] = int.TryParse(parts[i], out var n) ? n : 0;
                return arr;
            }
            var aa = Parse(a); var bb = Parse(b);
            if (aa[0] != bb[0]) return aa[0].CompareTo(bb[0]);
            if (aa[1] != bb[1]) return aa[1].CompareTo(bb[1]);
            return aa[2].CompareTo(bb[2]);
        }

        private static bool TryProp(JsonElement el, string propName, out JsonElement val)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (p.NameEquals(propName) || string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    val = p.Value; return true;
                }
            }
            val = default; return false;
        }

        private static string? TryGetString(JsonElement el, string prop)
        {
            return TryProp(el, prop, out var v)
                ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString())
                : null;
        }

        private static double? TryGetDouble(JsonElement el, string prop)
        {
            if (!TryProp(el, prop, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.Number => v.TryGetDouble(out var d) ? d : (double?)null,
                JsonValueKind.String => double.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                                                        System.Globalization.CultureInfo.InvariantCulture, out var d2) ? d2 : (double?)null,
                _ => null
            };
        }

        private static Brush? ToBrushSafe(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            try
            {
                var obj = new BrushConverter().ConvertFromString(hex);
                if (obj is Brush br) return br;
            }
            catch { }
            return null;
        }

        private static double MeasureTextWidth(string text, string fontFamily, double fontSize)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var typeface = new Typeface(new FontFamily(fontFamily),
                FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

#pragma warning disable CS0618
            var ft = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Transparent,
                1.0);
#pragma warning restore CS0618

            return Math.Ceiling(ft.Width);
        }

        // ===== helpers on JsonObject =====
    }

    internal static class JsonObjectExt
    {
        public static JsonNode? TryGet(this JsonObject o, string name)
        {
            foreach (var kv in o)
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            return null;
        }
        public static string? TryGetString(this JsonObject o, string name)
        {
            var n = o.TryGet(name);
            if (n is JsonValue v && v.TryGetValue<string>(out var s)) return s;
            return n?.ToString();
        }
        public static double? TryGetDouble(this JsonObject o, string name)
        {
            var n = o.TryGet(name);
            if (n is JsonValue v && v.TryGetValue<double>(out var d)) return d;
            if (double.TryParse(n?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dd)) return dd;
            return null;
        }
    }

    // ====== VMs ======
    public sealed class GraphVm : ViewModelBase
    {
        public ObservableCollection<StageVm> Stages { get; } = new();
        public ObservableCollection<StageEdgeVm> StageEdges { get; } = new();
    }
    public sealed class StageVm : ViewModelBase
    {
        public Guid Id { get; set; }
        private string _title = "Stage";
        public string Title { get => _title; set { _title = value; Raise(); } }
        private double _x; public double X { get => _x; set { _x = value; Raise(); } }
        private double _y; public double Y { get => _y; set { _y = value; Raise(); } }
        private double _w = 600; public double W { get => _w; set { _w = Math.Max(200, value); Raise(); } }
        private double _h = 400; public double H { get => _h; set { _h = Math.Max(150, value); Raise(); } }
        private bool _sel; public bool IsSelected { get => _sel; set { _sel = value; Raise(); } }
        public ObservableCollection<string> AssignedGroups { get; } = new();
        public ObservableCollection<string> AssignedUsers { get; } = new();
        private string _startMode = "Manual"; public string StartMode { get => _startMode; set { _startMode = value; Raise(); } }
        private int _slaHours = 0; public int SLAHours { get => _slaHours; set { _slaHours = value; Raise(); } }
        private bool _allowReopen = true; public bool AllowReopen { get => _allowReopen; set { _allowReopen = value; Raise(); } }
        private bool _autoCompleteOnAllBlocks = false; public bool AutoCompleteOnAllBlocks { get => _autoCompleteOnAllBlocks; set { _autoCompleteOnAllBlocks = value; Raise(); } }
        public ObservableCollection<BlockVm> Blocks { get; } = new();
    }
    public sealed class BlockVm : ViewModelBase
    {
        public Guid Id { get; set; }
        public Guid BlockId { get; set; }      // ← KANON
        public string Title { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        private double _x; public double X { get => _x; set { _x = value; Raise(); } }
        private double _y; public double Y { get => _y; set { _y = value; Raise(); } }
        private bool _sel; public bool IsSelected { get => _sel; set { _sel = value; Raise(); } }
        public ObservableCollection<FieldComponentBase> Components { get; } = new();
        private double _previewWidth = 320; public double PreviewWidth { get => _previewWidth; set { _previewWidth = value; Raise(); } }
        private double _previewHeight = 200; public double PreviewHeight { get => _previewHeight; set { _previewHeight = value; Raise(); } }
    }



    public sealed class StageEdgeVm : ViewModelBase
    {
        public Guid Id { get; set; }
        public Guid FromStageId { get; set; }
        public Guid ToStageId { get; set; }
        public string ConditionJson { get; set; } = @"{ ""conditions"": [] }";
    }
    public sealed class PaletteItem
    {
        public Guid BlockId { get; }
        public string Name { get; }
        public string Version { get; }
        public string Display => $"{Name}   [{BlockId}]   v{Version}";
        public PaletteItem(Guid blockId, string name, string version) { BlockId = blockId; Name = name; Version = version; }
    }
}
