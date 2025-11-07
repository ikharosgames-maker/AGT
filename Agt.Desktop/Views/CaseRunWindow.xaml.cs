using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Agt.Domain.Models;
using Agt.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Agt.Desktop.Views
{
    public partial class CaseRunWindow : Window
    {
        private readonly ICaseDataRepository _repo;
        public Guid CaseId { get; }
        public List<FieldVm> Fields { get; } = new();

        public CaseRunWindow(Guid caseId, IServiceProvider services)
        {
            InitializeComponent();
            DataContext = this;

            _repo = services.GetRequiredService<ICaseDataRepository>();
            CaseId = caseId;

            BuildDemoFields();
            _ = LoadExistingAsync();
        }

        private void BuildDemoFields()
        {
            Fields.Add(FieldVm.Text("firstname", "Jméno"));
            Fields.Add(FieldVm.Text("lastname", "Příjmení"));
            Fields.Add(FieldVm.Combo("role", "Role", new[] { "User", "Power User", "Admin" }));
            Fields.Add(FieldVm.Check("active", "Aktivní"));
            Fields.Add(FieldVm.Date("birth", "Datum narození"));
        }

        private async Task LoadExistingAsync()
        {
            var s = await _repo.LoadAsync(CaseId);
            if (s == null) return;

            foreach (var f in Fields)
            {
                if (s.Values.TryGetValue(f.Key, out var val))
                {
                    f.SetValueFromStorage(val);
                }
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var snapshot = new CaseDataSnapshot { CaseId = CaseId };
            foreach (var f in Fields)
            {
                var val = f.GetValueForStorage()?.ToString(); // uložit jako string
                snapshot.Values[f.Key] = val;
            }

            await _repo.SaveAsync(snapshot);
            MessageBox.Show(this, "Uloženo.", "Case", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }

    public abstract class FieldVm : ContentControl
    {
        public string Key { get; }
        public string Label { get; }

        protected FieldVm(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public abstract object? GetValueForStorage();
        public abstract void SetValueFromStorage(object? value);

        public static FieldVm Text(string key, string label)
        {
            var tb = new TextBox { Style = (Style)System.Windows.Application.Current.FindResource("TextBoxInput") };
            return new TextFieldVm(key, label, tb);
        }

        public static FieldVm Combo(string key, string label, IEnumerable<string> items)
        {
            var cb = new ComboBox { Style = (Style)System.Windows.Application.Current.FindResource("ComboInput") };
            foreach (var it in items) cb.Items.Add(it);
            return new ComboFieldVm(key, label, cb);
        }

        public static FieldVm Check(string key, string label)
        {
            var ch = new CheckBox { Style = (Style)System.Windows.Application.Current.FindResource("CheckInput") };
            return new CheckFieldVm(key, label, ch);
        }

        public static FieldVm Date(string key, string label)
        {
            var dp = new DatePicker { Style = (Style)System.Windows.Application.Current.FindResource("DateInput") };
            return new DateFieldVm(key, label, dp);
        }
    }

    public sealed class TextFieldVm : FieldVm
    {
        private readonly TextBox _tb;
        public TextFieldVm(string key, string label, TextBox tb) : base(key, label)
        {
            _tb = tb;
            Content = _tb;
        }
        public override object? GetValueForStorage() => _tb.Text;
        public override void SetValueFromStorage(object? value) => _tb.Text = value?.ToString() ?? string.Empty;
    }

    public sealed class ComboFieldVm : FieldVm
    {
        private readonly ComboBox _cb;
        public ComboFieldVm(string key, string label, ComboBox cb) : base(key, label)
        {
            _cb = cb;
            Content = _cb;
        }
        public override object? GetValueForStorage() => _cb.SelectedItem?.ToString();
        public override void SetValueFromStorage(object? value)
        {
            var s = value?.ToString();
            if (s == null) { _cb.SelectedIndex = -1; return; }
            var match = _cb.Items.Cast<object>().FirstOrDefault(i => string.Equals(i?.ToString(), s, StringComparison.Ordinal));
            _cb.SelectedItem = match;
        }
    }

    public sealed class CheckFieldVm : FieldVm
    {
        private readonly CheckBox _ch;
        public CheckFieldVm(string key, string label, CheckBox ch) : base(key, label)
        {
            _ch = ch;
            Content = _ch;
        }
        public override object? GetValueForStorage() => _ch.IsChecked == true;
        public override void SetValueFromStorage(object? value)
        {
            if (value is bool b) _ch.IsChecked = b;
            else if (bool.TryParse(value?.ToString(), out var parsed)) _ch.IsChecked = parsed;
            else _ch.IsChecked = false;
        }
    }

    public sealed class DateFieldVm : FieldVm
    {
        private readonly DatePicker _dp;
        public DateFieldVm(string key, string label, DatePicker dp) : base(key, label)
        {
            _dp = dp;
            Content = _dp;
        }
        public override object? GetValueForStorage() => _dp.SelectedDate;
        public override void SetValueFromStorage(object? value)
        {
            if (value is DateTime dt) _dp.SelectedDate = dt;
            else if (DateTime.TryParse(value?.ToString(), out var parsed)) _dp.SelectedDate = parsed;
            else _dp.SelectedDate = null;
        }
    }
}
