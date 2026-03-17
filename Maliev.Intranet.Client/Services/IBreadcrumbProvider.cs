namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Interface that page components can implement to provide custom breadcrumb labels.
/// <para>
/// <b>Implementation note:</b> Rather than implementing this interface directly,
/// pages should inject <see cref="BreadcrumbService"/> and call
/// <see cref="BreadcrumbService.SetPageLabel"/> in <c>OnInitializedAsync</c>.
/// </para>
/// <para>
/// Example usage in a detail page:
/// <code>
/// @inject BreadcrumbService BreadcrumbService
///
/// protected override async Task OnInitializedAsync()
/// {
///     await LoadData();
///     BreadcrumbService.SetPageLabel(_entity?.Name);
/// }
/// </code>
/// </para>
/// </summary>
public interface IBreadcrumbProvider
{
    /// <summary>
    /// Returns the human-readable label to display in the breadcrumb for this page.
    /// For example, a CustomerDetail page would return the customer's name.
    /// Return <c>null</c> to fall back to the auto-generated label.
    /// </summary>
    string? GetBreadcrumbLabel();
}
