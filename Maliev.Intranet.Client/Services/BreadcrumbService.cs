namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Scoped service that allows page components to register a human-readable label
/// for the current breadcrumb segment. Pages call <see cref="SetPageLabel"/> to
/// override the auto-generated segment label (e.g., replace a GUID with an entity name).
/// </summary>
public sealed class BreadcrumbService
{
    private string? _currentLabel;

    /// <summary>Fires when the current page label changes.</summary>
    public event Action? OnChanged;

    /// <summary>Gets the label registered by the current page, or <c>null</c> if none.</summary>
    public string? CurrentLabel => _currentLabel;

    /// <summary>
    /// Called by page components to supply a human-readable breadcrumb label for the current route.
    /// Triggers <see cref="OnChanged"/> so that MainLayout can re-render breadcrumbs.
    /// </summary>
    /// <param name="label">The entity name or page title to display (e.g., "Acme Corp").</param>
    public void SetPageLabel(string? label)
    {
        _currentLabel = label;
        OnChanged?.Invoke();
    }

    /// <summary>Clears the stored label. Called by MainLayout on navigation.</summary>
    public void Clear()
    {
        _currentLabel = null;
    }
}
