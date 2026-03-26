using Maliev.Intranet.Client;
using Maliev.Intranet.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Maliev.Intranet.Tests.Client.Services;

public class ThemeServiceTests
{
    [Fact]
    public void CreateMalievTheme_ReturnsValidTheme()
    {
        var theme = ThemeConfiguration.MalievTheme;

        Assert.NotNull(theme);
        Assert.NotNull(theme.PaletteDark);
        Assert.NotNull(theme.PaletteLight);

        // MudColor.ToString() returns rgba(...)
        // PaletteDark.Primary is not explicitly set, so it uses MudBlazor default
        Assert.Equal("rgba(119,107,231,1)", theme.PaletteDark.Primary.ToString());
        // PaletteLight.Primary = "#1b6ec2"
        Assert.Equal("rgba(27,110,194,1)", theme.PaletteLight.Primary.ToString());
    }

    [Fact]
    public async Task LayoutService_ToggleCyclesCorrectly()
    {
        // Arrange
        var jsMock = new Mock<IJSRuntime>();
        var loggerMock = new Mock<ILogger<LayoutService>>();
        var service = new LayoutService(jsMock.Object, loggerMock.Object);

        // Act & Assert cycle: System (Initial) -> Light -> Dark -> System
        Assert.Equal(ThemeMode.System, service.CurrentMode);

        await service.ToggleModeAsync();
        Assert.Equal(ThemeMode.Light, service.CurrentMode);

        await service.ToggleModeAsync();
        Assert.Equal(ThemeMode.Dark, service.CurrentMode);

        await service.ToggleModeAsync();
        Assert.Equal(ThemeMode.System, service.CurrentMode);
    }

    [Fact]
    public async Task LayoutService_CalculateEffectiveTheme_WorksWithSystemPreference()
    {
        // Arrange
        var jsMock = new Mock<IJSRuntime>();
        var loggerMock = new Mock<ILogger<LayoutService>>();
        var service = new LayoutService(jsMock.Object, loggerMock.Object);

        // Act: System mode + System Dark = true
        service.UpdateSystemPreference(true);
        Assert.True(service.IsDarkMode);

        // Act: System mode + System Light = false
        service.UpdateSystemPreference(false);
        Assert.False(service.IsDarkMode);

        // Act: Explicit Dark mode
        await service.SetModeAsync(ThemeMode.Dark);
        service.UpdateSystemPreference(false); // System is light, but user wants dark
        Assert.True(service.IsDarkMode);
    }
}
