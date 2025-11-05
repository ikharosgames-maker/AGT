using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Agt.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel pro přehled case na hlavním okně (dashboard).
    /// Sloupce: Formulář, Založeno, Založil, Krok (i/N), Aktuální stage.
    /// </summary>
    public sealed class CasesDashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<CaseRow> Items { get; } = new ObservableCollection<CaseRow>();

        private CaseRow? _selected;
        public CaseRow? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        public CasesDashboardViewModel()
        {
            // TODO: Napojte na skutečné zdroje dat.
            Items.Clear();
            Items.Add(new CaseRow
            {
                FormName = "Ukázkový formulář",
                CreatedAt = DateTime.Now.AddDays(-1),
                CreatedBy = Environment.UserName,
                Step = ToStepLabel(0, 3),
                CurrentStageName = "Stage 1"
            });
        }

        /// <summary>
        /// Bezpečné vytvoření labelu kroku "i/N" (index je 0-based).
        /// </summary>
        public static string ToStepLabel(int currentIndexZeroBased, int totalStages)
        {
            if (totalStages <= 0) return "0/0";
            var i = Math.Clamp(currentIndexZeroBased + 1, 1, totalStages);
            return $"{i}/{totalStages}";
        }
    }

    /// <summary>
    /// Řádek tabulky na dashboardu.
    /// </summary>
    public sealed class CaseRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _formName = string.Empty;
        public string FormName
        {
            get => _formName;
            set { _formName = value; OnPropertyChanged(); }
        }

        private DateTime _createdAt = DateTime.Now;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        private string _createdBy = string.Empty;
        public string CreatedBy
        {
            get => _createdBy;
            set { _createdBy = value; OnPropertyChanged(); }
        }

        private string _step = "0/0";
        public string Step
        {
            get => _step;
            set { _step = value; OnPropertyChanged(); }
        }

        private string _currentStageName = string.Empty;
        public string CurrentStageName
        {
            get => _currentStageName;
            set { _currentStageName = value; OnPropertyChanged(); }
        }
    }
}