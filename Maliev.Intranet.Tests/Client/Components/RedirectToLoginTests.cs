using Bunit;
using Maliev.Intranet.Client.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class RedirectToLoginTests : BunitContext
{
    [Fact]
    public void RedirectToLogin_PreservesLocalPathQueryAndFragmentReturnUrl()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("http://localhost/customers/42?tab=quotes#activity");

        Render<RedirectToLogin>();

        Assert.Equal(
            "http://localhost/login?returnUrl=%2Fcustomers%2F42%3Ftab%3Dquotes%23activity",
            navigation.Uri);
    }
}
