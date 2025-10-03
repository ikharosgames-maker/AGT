using System.Collections.ObjectModel;

namespace Agt.Desktop.ViewModels.Flow;

public sealed class FlowGraphViewModel : ViewModelBase
{
    public ObservableCollection<NodeVm> Nodes { get; } = new();
    public ObservableCollection<EdgeVm> Edges { get; } = new();

    private NodeVm? _pendingFrom;
    public NodeVm? SelectedNode { get; set; }
    public EdgeVm? SelectedEdge { get; set; }

    public void AddNode(string key, string title, string version, double x, double y)
        => Nodes.Add(new NodeVm { Id = Guid.NewGuid(), Key = key, Title = title, Version = version, X = x, Y = y });

    public void StartConnection(NodeVm from) => _pendingFrom = from;

    public void CommitConnection(NodeVm to)
    {
        if (_pendingFrom is null || to is null || ReferenceEquals(_pendingFrom, to)) { _pendingFrom = null; return; }
        Edges.Add(new EdgeVm { Id = Guid.NewGuid(), FromId = _pendingFrom.Id, ToId = to.Id, ConditionJson = @"{ ""conditions"": [] }" });
        _pendingFrom = null;
    }
}

public sealed class NodeVm : ViewModelBase
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    private double _x; public double X { get => _x; set { _x = value; Raise(); } }
    private double _y; public double Y { get => _y; set { _y = value; Raise(); } }
}

public sealed class EdgeVm : ViewModelBase
{
    public Guid Id { get; set; }
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public string ConditionJson { get; set; } = @"{ ""conditions"": [] }";
}
