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
}
