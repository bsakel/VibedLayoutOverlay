using LayoutOverlay.Windows.Forms;

namespace LayoutOverlay.Windows.Services;

public class OverlayWindowService : IDisposable
{
    private readonly LayoutService _layoutService;
    private OverlayForm? _overlayForm;

    public OverlayWindowService(LayoutService layoutService)
    {
        _layoutService = layoutService;
        _layoutService.LayoutChanged += OnLayoutChanged;
        _layoutService.OverlayVisibilityChanged += OnOverlayVisibilityChanged;
        _layoutService.TransparencyChanged += OnTransparencyChanged;
    }

    public void ShowOverlay()
    {
        if (_overlayForm == null || _overlayForm.IsDisposed)
        {
            _overlayForm = new OverlayForm(_layoutService);
        }

        if (!_overlayForm.Visible)
        {
            _overlayForm.Show();
        }
    }

    public void HideOverlay()
    {
        _overlayForm?.Hide();
    }

    private void OnLayoutChanged(object? sender, LayoutChangedEventArgs e)
    {
        _overlayForm?.RefreshLayout();
    }

    private void OnOverlayVisibilityChanged(object? sender, OverlayVisibilityChangedEventArgs e)
    {
        if (e.IsVisible)
        {
            ShowOverlay();
        }
        else
        {
            HideOverlay();
        }
    }

    private void OnTransparencyChanged(object? sender, TransparencyChangedEventArgs e)
    {
        _overlayForm?.RefreshLayout();
    }

    public void Dispose()
    {
        _layoutService.LayoutChanged -= OnLayoutChanged;
        _layoutService.OverlayVisibilityChanged -= OnOverlayVisibilityChanged;
        _layoutService.TransparencyChanged -= OnTransparencyChanged;
        
        _overlayForm?.Close();
        _overlayForm?.Dispose();
    }
}