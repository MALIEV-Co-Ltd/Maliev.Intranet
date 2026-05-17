using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Configures MudBlazor services with MALIEV-specific interaction defaults.
/// </summary>
public static class MalievMudServices
{
    /// <summary>
    /// Adds MudBlazor with a restrained snackbar policy for operational screens.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddMalievMudServices(this IServiceCollection services)
    {
        return services.AddMudServices(options =>
        {
            options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
            options.SnackbarConfiguration.MaxDisplayedSnackbars = 2;
            options.SnackbarConfiguration.PreventDuplicates = true;
            options.SnackbarConfiguration.NewestOnTop = false;
            options.SnackbarConfiguration.ShowCloseIcon = true;
            options.SnackbarConfiguration.VisibleStateDuration = 2600;
            options.SnackbarConfiguration.ShowTransitionDuration = 120;
            options.SnackbarConfiguration.HideTransitionDuration = 120;
            options.SnackbarConfiguration.ClearAfterNavigation = true;
            options.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });
    }
}
