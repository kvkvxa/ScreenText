namespace ScreenText.Capture;

public sealed class SelectionOverlayManager
{
    private readonly MonitorService _monitorService;
    private readonly List<SelectionOverlayWindow> _overlays = [];
    private TaskCompletionSource<PhysicalRect?>? _selectionCompletion;
    private SelectionSession? _session;
    private IReadOnlyList<PhysicalRect>? _topologyAtStart;

    public SelectionOverlayManager(MonitorService monitorService) => _monitorService = monitorService;

    public Task<PhysicalRect?> SelectRegionAsync()
    {
        if (_selectionCompletion is not null) throw new InvalidOperationException("A selection is already active.");
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            throw new InvalidOperationException("SelectRegionAsync must be called on the UI thread.");
        }
        _selectionCompletion = new TaskCompletionSource<PhysicalRect?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = _selectionCompletion.Task;
        try
        {
            var monitors = _monitorService.GetMonitors();
            _topologyAtStart = monitors.Select(monitor => monitor.Bounds)
                .OrderBy(bounds => bounds.Left)
                .ThenBy(bounds => bounds.Top)
                .ToArray();
            foreach (var monitor in monitors)
            {
                var overlay = new SelectionOverlayWindow(monitor, BeginSelection, UpdateSelection, EndSelection, CancelSelection);
                _overlays.Add(overlay);
                overlay.Show();
            }
            if (_overlays.Count == 0) Complete(null);
            else _overlays[0].Focus();
        }
        catch
        {
            Complete(null);
            throw;
        }
        return task;
    }

    private void BeginSelection(SelectionOverlayWindow source, PhysicalPoint point)
    {
        _session = new SelectionSession(point);
        Render();
    }

    private void UpdateSelection(PhysicalPoint point)
    {
        _session?.Update(point);
        Render();
    }

    private void EndSelection(SelectionOverlayWindow source)
    {
        if (_session is null) return;
        var selection = _session.CurrentRect;
        Complete(selection.IsEmpty ? null : selection);
    }

    private void CancelSelection() => Complete(null);

    private void Render()
    {
        foreach (var overlay in _overlays) overlay.RenderSelection(_session?.CurrentRect);
    }

    private void Complete(PhysicalRect? selection)
    {
        var completion = _selectionCompletion;
        if (completion is null) return;
        _selectionCompletion = null;
        _session = null;
        _topologyAtStart = null;
        foreach (var overlay in _overlays.ToArray())
        {
            overlay.ReleaseMouseCapture();
            overlay.Close();
        }
        _overlays.Clear();
        completion.TrySetResult(selection);
    }

    public void Cancel() => CancelSelection();

    public bool IsSelectionOnCurrentTopology(PhysicalRect selection)
    {
        var expectedTopology = _topologyAtStart;
        if (expectedTopology is null) return false;

        var currentTopology = _monitorService.GetMonitors()
            .Select(monitor => monitor.Bounds)
            .OrderBy(bounds => bounds.Left)
            .ThenBy(bounds => bounds.Top)
            .ToArray();
        return expectedTopology.SequenceEqual(currentTopology)
            && currentTopology.Any(bounds => !bounds.Intersect(selection).IsEmpty);
    }
}
