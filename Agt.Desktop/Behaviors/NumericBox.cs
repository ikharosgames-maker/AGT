using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Agt.Desktop.Behaviors
{
    /// <summary>
    /// Attached behavior pro TextBox: povolí jen čísla (s ohledem na locale),
    /// volitelně desetinnou část a/nebo záporné znaménko. Filtruje psaní i paste.
    /// </summary>
    public static class NumericBox
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(NumericBox),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty AllowDecimalProperty =
            DependencyProperty.RegisterAttached(
                "AllowDecimal", typeof(bool), typeof(NumericBox),
                new PropertyMetadata(true));

        public static void SetAllowDecimal(DependencyObject obj, bool value) => obj.SetValue(AllowDecimalProperty, value);
        public static bool GetAllowDecimal(DependencyObject obj) => (bool)obj.GetValue(AllowDecimalProperty);

        public static readonly DependencyProperty AllowNegativeProperty =
            DependencyProperty.RegisterAttached(
                "AllowNegative", typeof(bool), typeof(NumericBox),
                new PropertyMetadata(true));

        public static void SetAllowNegative(DependencyObject obj, bool value) => obj.SetValue(AllowNegativeProperty, value);
        public static bool GetAllowNegative(DependencyObject obj) => (bool)obj.GetValue(AllowNegativeProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            if ((bool)e.NewValue)
            {
                tb.PreviewTextInput += OnPreviewTextInput;
                tb.PreviewKeyDown += OnPreviewKeyDown;
                DataObject.AddPastingHandler(tb, OnPaste);
            }
            else
            {
                tb.PreviewTextInput -= OnPreviewTextInput;
                tb.PreviewKeyDown -= OnPreviewKeyDown;
                DataObject.RemovePastingHandler(tb, OnPaste);
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // povol ovládací klávesy: Delete, Backspace, Tab, šipky, Home/End
            if (e.Key is Key.Back or Key.Delete or Key.Tab or Key.Left or Key.Right or Key.Home or Key.End)
                return;

            // Ctrl kombinace povolit (A/C/X/V)
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                return;

            // ostatní posuzujeme v PreviewTextInput
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var allowDec = GetAllowDecimal(tb);
            var allowNeg = GetAllowNegative(tb);

            var dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var neg = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;

            var newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                 .Insert(tb.SelectionStart, e.Text);

            e.Handled = !IsValid(newText, allowDec, allowNeg, dec, neg);
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var allowDec = GetAllowDecimal(tb);
            var allowNeg = GetAllowNegative(tb);
            var dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var neg = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;

            if (e.DataObject.GetDataPresent(DataFormats.UnicodeText))
            {
                string paste = (string)e.DataObject.GetData(DataFormats.UnicodeText) ?? string.Empty;
                var newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                     .Insert(tb.SelectionStart, paste);

                if (!IsValid(newText, allowDec, allowNeg, dec, neg))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsValid(string text, bool allowDec, bool allowNeg, string dec, string neg)
        {
            text = text.Trim();
            if (text.Length == 0) return true; // umožnit prázdno během psaní

            string decEsc = Regex.Escape(dec);
            string negEsc = Regex.Escape(neg);

            string pattern = allowDec
                ? (allowNeg ? $"^{negEsc}?\\d*({decEsc}\\d*)?$" : $"^\\d*({decEsc}\\d*)?$")
                : (allowNeg ? $"^{negEsc}?\\d*$" : "^\\d*$");

            return Regex.IsMatch(text, pattern);
        }
    }
}
