using System.Net;
using Maliev.Intranet.Bff.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class LoginPageControllerTests
{
    [Fact]
    public void Login_ReturnsServerOwnedHtmlWithoutWasmBootScript()
    {
        var controller = new LoginPageController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Login("/customers", null);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html; charset=utf-8", content.ContentType);
        Assert.Contains("MALIEV", content.Content);
        Assert.Contains("/api/v1/auth/login-form", content.Content);
        Assert.DoesNotContain("_framework/blazor.web.js", content.Content);
        Assert.DoesNotContain("wasm-loading", content.Content);
    }

    [Fact]
    public void Login_UsesMalievLogoGoogleButtonActiveThemeIconsAndCompanyFooter()
    {
        var controller = new LoginPageController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Login("/customers", null);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Contains("src=\"/images/logo.svg\"", content.Content);
        Assert.Contains("src=\"/images/logo-white.svg\"", content.Content);
        Assert.Contains("alt=\"MALIEV Logo\"", content.Content);
        Assert.Contains("class=\"btn-google\"", content.Content);
        Assert.Contains("class=\"google-logo\"", content.Content);
        Assert.Contains("Sign in with Google", content.Content);
        Assert.Contains("data-theme-icon=\"dark\"", content.Content);
        Assert.Contains("data-theme-icon=\"light\"", content.Content);
        Assert.Contains("data-theme-icon=\"system\"", content.Content);
        Assert.Contains("MALIEV CO., LTD.", content.Content);
        Assert.DoesNotContain("MALIEV INC. Employee systems", content.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(">A</button>", content.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Login_WhenAuthCorrelationFails_ShowsNaturalRetryMessage()
    {
        var controller = new LoginPageController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Login("/accounting", "Correlation failed.");

        var content = Assert.IsType<ContentResult>(result);
        var decodedContent = WebUtility.HtmlDecode(content.Content);
        Assert.Contains("We couldn't complete sign-in right now. Please try again in a moment.", decodedContent);
        Assert.DoesNotContain("Correlation failed", content.Content, StringComparison.Ordinal);
    }
}
