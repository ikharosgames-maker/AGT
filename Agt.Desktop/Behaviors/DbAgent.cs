using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Data.SqlClient; // NuGet: Microsoft.Data.SqlClient

namespace Agt.Desktop.Behaviors
{
    /// <summary>
    /// Uživ. databinding agent:
    /// - beh:DbAgent.ConnectionKey="MainDb" (dědí se stromem)
    /// - beh:DbAgent.Binding="{Binding DataBinding}" (JSON skript na komponentě)
    /// </summary>
    public static class DbAgent
    {
        public static readonly DependencyProperty ConnectionKeyProperty =
            DependencyProperty.RegisterAttached(
                "ConnectionKey",
                typeof(string),
                typeof(DbAgent),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetConnectionKey(DependencyObject d, string v) => d.SetValue(ConnectionKeyProperty, v);
        public static string GetConnectionKey(DependencyObject d) => (string)d.GetValue(ConnectionKeyProperty);

        public static readonly DependencyProperty BindingProperty =
            DependencyProperty.RegisterAttached(
                "Binding",
                typeof(string),
                typeof(DbAgent),
                new PropertyMetadata(null, OnBindingChanged));

        public static void SetBinding(DependencyObject d, string v) => d.SetValue(BindingProperty, v);
        public static string GetBinding(DependencyObject d) => (string)d.GetValue(BindingProperty);

        private static readonly DependencyProperty HostStateProperty =
            DependencyProperty.RegisterAttached("HostState", typeof(HostState), typeof(DbAgent));

        private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            (fe.GetValue(HostStateProperty) as HostState)?.Dispose();
            fe.ClearValue(HostStateProperty);

            var json = e.NewValue as string;
            if (string.IsNullOrWhiteSpace(json)) return;

            var connKey = GetConnectionKey(fe);
            var allFields = ResolveFieldsFromDataContext(fe.DataContext);
            if (allFields is null || allFields.Count == 0) return;

            DbScript script;
            try
            {
                script = JsonSerializer.Deserialize<DbScript>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
            }
            catch
            {
                return;
            }
            if (script == null) return;

            var state = new HostState(fe, allFields, connKey, script);
            fe.SetValue(HostStateProperty, state);
            state.Hook();
        }

        private sealed class HostState : IDisposable
        {
            private readonly FrameworkElement _host;
            private readonly IList<object> _fields;
            private readonly string _inheritedConnKey;
            private readonly DbScript _script;

            private readonly Dictionary<string, object> _byKey;
            private INotifyPropertyChanged _triggerSource;
            private CancellationTokenSource _cts;

            public HostState(FrameworkElement host, IList<object> fields, string connKey, DbScript script)
            {
                _host = host;
                _fields = fields;
                _inheritedConnKey = connKey;
                _script = script ?? new DbScript();

                _byKey = fields
                    .Select(f => (f, Key: GetProp<string>(f, "FieldKey")))
                    .Where(t => !string.IsNullOrWhiteSpace(t.Key))
                    .ToDictionary(t => t.Key, t => t.f, StringComparer.OrdinalIgnoreCase);
            }

            public void Hook()
            {
                var on = _script.On?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(on) || on == "load" || on == "startup")
                {
                    _host.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => _ = RunAsyncSafe()));
                    return;
                }

                if (on.StartsWith("change"))
                {
                    string fk = null;
                    var idx = on.IndexOf(':');
                    if (idx > 0 && idx < on.Length - 1) fk = on[(idx + 1)..].Trim();

                    object source = null;
                    if (!string.IsNullOrWhiteSpace(fk)) _byKey.TryGetValue(fk, out source);
                    else source = _host.DataContext;

                    if (source is INotifyPropertyChanged npc)
                    {
                        _triggerSource = npc;
                        npc.PropertyChanged += OnSourceChanged;
                    }
                }
            }

            private void OnSourceChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName != "Value") return;
                DebounceAndRun();
            }

            private void DebounceAndRun(int ms = 200)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(ms, _cts.Token);
                        await RunAsyncSafe();
                    }
                    catch (TaskCanceledException) { }
                });
            }

            private async Task RunAsyncSafe()
            {
                try { await RunAsync(); } catch { /* log případně sem */ }
            }

            private async Task RunAsync()
            {
                if (string.IsNullOrWhiteSpace(_script.Query)) return;

                string connString = ResolveConnection(_inheritedConnKey, _script.ConnectionKey);
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(_script.Query, conn)
                {
                    CommandType = System.Data.CommandType.Text,
                    CommandTimeout = 30
                };

                var sourceVal = GetProp<object>(_host.DataContext, "Value");
                cmd.Parameters.Add(new SqlParameter("@Value", sourceVal ?? DBNull.Value));

                if (_script.Parameters != null)
                {
                    foreach (var p in _script.Parameters)
                    {
                        object val = p.Literal;
                        if (!string.IsNullOrWhiteSpace(p.FromFieldKey) && _byKey.TryGetValue(p.FromFieldKey, out var src))
                            val = GetProp<object>(src, "Value");

                        cmd.Parameters.Add(new SqlParameter("@" + p.Name, val ?? DBNull.Value));
                    }
                }

                var resultSets = new List<List<Dictionary<string, object>>>();
                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    do
                    {
                        var rows = new List<Dictionary<string, object>>();
                        var names = Enumerable.Range(0, rdr.FieldCount).Select(rdr.GetName).ToArray();
                        while (await rdr.ReadAsync())
                        {
                            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < names.Length; i++)
                                row[names[i]] = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
                            rows.Add(row);
                        }
                        resultSets.Add(rows);
                    } while (await rdr.NextResultAsync());
                }

                foreach (var map in _script.Maps ?? Enumerable.Empty<DbMap>())
                {
                    var idx = Math.Max(0, map.ResultIndex);
                    var rows = idx < resultSets.Count ? resultSets[idx] : new List<Dictionary<string, object>>();

                    switch (map.Kind?.ToLowerInvariant())
                    {
                        case "options": ApplyOptions(map, rows); break;
                        case "fill": ApplyFill(map, rows); break;
                        case "validation": ApplyValidation(map, rows); break;
                    }
                }
            }

            private void ApplyOptions(DbMap map, List<Dictionary<string, object>> rows)
            {
                if (!string.IsNullOrWhiteSpace(map.FieldKeyColumn))
                {
                    var groups = rows.GroupBy(r => r.TryGetValue(map.FieldKeyColumn, out var k) ? k?.ToString() : null);
                    foreach (var grp in groups)
                    {
                        if (grp.Key == null) continue;
                        if (!_byKey.TryGetValue(grp.Key, out var field)) continue;

                        var (list, itemType) = GetOrCreateOptions(field);
                        list.Clear();

                        foreach (var r in grp)
                            list.Add(CreateOptionItem(itemType,
                                r.TryGetValue(map.ValueColumn ?? "Id", out var v) ? v : null,
                                r.TryGetValue(map.DisplayColumn ?? "Name", out var d) ? d?.ToString() : null));

                        var cur = GetProp<object>(field, "Value");
                        if (cur != null && !ListAnyValueEquals(list, itemType, cur))
                            SetProp(field, "Value", null);
                    }
                }
                else
                {
                    var cache = rows.Select(r => new
                    {
                        Val = r.TryGetValue(map.ValueColumn ?? "Id", out var v) ? v : null,
                        Disp = r.TryGetValue(map.DisplayColumn ?? "Name", out var d) ? d?.ToString() : null
                    }).ToList();

                    foreach (var key in map.TargetFieldKeys ?? Enumerable.Empty<string>())
                    {
                        if (!_byKey.TryGetValue(key, out var field)) continue;
                        var (list, itemType) = GetOrCreateOptions(field);
                        list.Clear();
                        foreach (var it in cache)
                            list.Add(CreateOptionItem(itemType, it.Val, it.Disp));

                        var cur = GetProp<object>(field, "Value");
                        if (cur != null && !ListAnyValueEquals(list, itemType, cur))
                            SetProp(field, "Value", null);
                    }
                }
            }

            private void ApplyFill(DbMap map, List<Dictionary<string, object>> rows)
            {
                if (rows.Count == 0) return;
                var first = rows[0];

                foreach (var kv in map.Fill ?? new Dictionary<string, string>())
                {
                    if (!_byKey.TryGetValue(kv.Value, out var field)) continue;
                    if (!first.TryGetValue(kv.Key, out var val)) continue;

                    if (!SetProp(field, "Value", val))
                        SetProp(field, "Label", val?.ToString());
                }
            }

            private void ApplyValidation(DbMap map, List<Dictionary<string, object>> rows)
            {
                if (string.IsNullOrWhiteSpace(map.TargetFieldKey)) return;
                if (!_byKey.TryGetValue(map.TargetFieldKey, out var field)) return;

                bool any = rows.Count > 0;
                bool ok = map.AnyRow ? any : !any;
                SetErrors(field, "Value", ok ? null : new[] { map.Error ?? "Neplatná hodnota." });
            }

            private static string ResolveConnection(string inheritedKey, string overrideKey)
            {
                var key = string.IsNullOrWhiteSpace(overrideKey) ? inheritedKey : overrideKey;
                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidOperationException("DbAgent: Nenastaven ConnectionKey (ani na kořeni, ani ve skriptu).");
                return ConfigurationManager.ConnectionStrings[key].ConnectionString;
            }

            private static (IList list, Type itemType) GetOrCreateOptions(object field)
            {
                var p = field.GetType().GetProperty("Options", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p == null) throw new InvalidOperationException("Model nemá property 'Options'");

                if (p.GetValue(field) is IList existing && existing.GetType().IsGenericType)
                    return (existing, existing.GetType().GetGenericArguments()[0]);

                Type itemType = typeof(object);
                var propType = p.PropertyType;
                if (propType.IsGenericType)
                    itemType = propType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
                else
                    itemType = field.GetType().Assembly.GetTypes().FirstOrDefault(t => t.Name == "OptionItem") ?? typeof(object);

                var ocType = typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(itemType);
                var oc = (IList)Activator.CreateInstance(ocType);
                p.SetValue(field, oc);
                return (oc, itemType);
            }

            private static object CreateOptionItem(Type itemType, object value, string display)
            {
                var it = Activator.CreateInstance(itemType);
                var pVal = itemType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var pDisp = itemType.GetProperty("Display", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                pVal?.SetValue(it, value);
                pDisp?.SetValue(it, display);
                return it;
            }

            private static bool ListAnyValueEquals(IList list, Type itemType, object value)
            {
                var pVal = itemType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var obj in list)
                {
                    var v = pVal?.GetValue(obj);
                    if (Equals(v, value)) return true;
                }
                return false;
            }

            private static T GetProp<T>(object obj, string name)
            {
                if (obj == null) return default;
                var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p == null) return default;
                var v = p.GetValue(obj);
                if (v is T t) return t;
                try { return (T)Convert.ChangeType(v, typeof(T)); } catch { return default; }
            }

            private static bool SetProp(object obj, string name, object value)
            {
                if (obj == null) return false;
                var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p == null || !p.CanWrite) return false;
                p.SetValue(obj, value);
                return true;
            }

            private static void SetErrors(object field, string propName, IEnumerable<string> messages)
            {
                var m = field.GetType().GetMethod("SetErrors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null)
                    m.Invoke(field, new object[] { propName, messages });
            }

            public void Dispose()
            {
                _cts?.Cancel();
                _cts?.Dispose();
                if (_triggerSource != null)
                    _triggerSource.PropertyChanged -= OnSourceChanged;
            }
        }

        private static IList<object> ResolveFieldsFromDataContext(object dc)
        {
            if (dc == null) return null;
            foreach (var name in new[] { "Items", "Fields", "Components" })
            {
                var p = dc.GetType().GetProperty(name);
                if (p == null) continue;
                if (p.GetValue(dc) is IEnumerable seq)
                {
                    var list = new List<object>();
                    foreach (var it in seq) list.Add(it);
                    if (list.Count > 0) return list;
                }
            }
            return null;
        }

        private sealed class DbScript
        {
            [JsonPropertyName("on")] public string On { get; set; } = "load"; // load/startup | change | change:FieldKey
            [JsonPropertyName("connection")] public string ConnectionKey { get; set; }
            [JsonPropertyName("query")] public string Query { get; set; }
            [JsonPropertyName("params")] public List<DbParam> Parameters { get; set; }
            [JsonPropertyName("maps")] public List<DbMap> Maps { get; set; }
        }
        private sealed class DbParam
        {
            [JsonPropertyName("name")] public string Name { get; set; }
            [JsonPropertyName("fromField")] public string FromFieldKey { get; set; }
            [JsonPropertyName("value")] public object Literal { get; set; }
        }
        private sealed class DbMap
        {
            [JsonPropertyName("kind")] public string Kind { get; set; } // options|fill|validation
            [JsonPropertyName("result")] public int ResultIndex { get; set; } = 0;
            [JsonPropertyName("fieldKeyColumn")] public string FieldKeyColumn { get; set; }
            [JsonPropertyName("targets")] public List<string> TargetFieldKeys { get; set; }
            [JsonPropertyName("value")] public string ValueColumn { get; set; } = "Id";
            [JsonPropertyName("display")] public string DisplayColumn { get; set; } = "Name";
            [JsonPropertyName("fill")] public Dictionary<string, string> Fill { get; set; }
            [JsonPropertyName("target")] public string TargetFieldKey { get; set; }
            [JsonPropertyName("anyRow")] public bool AnyRow { get; set; } = true;
            [JsonPropertyName("error")] public string Error { get; set; } = "Neplatná hodnota.";
        }
    }
}
