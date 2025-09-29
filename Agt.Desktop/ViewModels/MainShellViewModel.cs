using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Agt.Desktop.ViewModels;

public sealed class MainShellViewModel : ViewModelBase
{
    public ObservableCollection<FormItemVm> Forms { get; } = new();
    public string? SearchText { get; set; }
    public object? SelectedVersion { get; set; }
    public string CurrentContextTitle => SelectedVersion is FormVersionVm v ? $"{v.FormName} v{v.Version}" : "Žádný výběr";
    public int SelectedTabIndex { get; set; } = 0;

    public ICommand NewFormCommand { get; }
    public ICommand NewFormVersionCommand { get; }
    public ICommand PublishFormVersionCommand { get; }

    public MainShellViewModel()
    {
        NewFormCommand = new RelayCommand(_ => NewForm());
        NewFormVersionCommand = new RelayCommand(_ => NewFormVersion(), _ => SelectedVersion is FormVersionVm);
        PublishFormVersionCommand = new RelayCommand(_ => Publish(), _ => SelectedVersion is FormVersionVm v && v.Status == "Draft");

        // TODO: načtení dat z IFormRepository → Forms
    }

    void NewForm() { /* otevři dialog, založ Form + verzi Draft, refresh */ }
    void NewFormVersion() { /* klon aktuální Published do nové Draft verze */ }
    void Publish() { /* nastav Status=Published, uzamkni */ }
}

public sealed class FormItemVm
{
    public string Name { get; set; } = "";
    public ObservableCollection<FormVersionVm> Versions { get; } = new();
}

public sealed class FormVersionVm
{
    public Guid Id { get; set; }
    public string FormName { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Status { get; set; } = "Draft";
}
