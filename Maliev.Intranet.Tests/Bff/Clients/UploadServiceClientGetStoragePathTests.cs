using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class UploadServiceClientGetStoragePathTests
{
    private static UploadServiceClient MakeClient(HttpStatusCode status, object? body = null)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            var msg = new HttpResponseMessage(status);
            if (body != null)
                msg.Content = JsonContent.Create(body);
            return Task.FromResult(msg);
        });
        return new UploadServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task GetStoragePathAsync_ReturnsStoragePath_OnSuccess()
    {
        var client = MakeClient(HttpStatusCode.OK, new { storagePath = "customers/c1/projects/p1/part.stl" });

        var result = await client.GetStoragePathAsync("upload-123");

        Assert.Equal("customers/c1/projects/p1/part.stl", result);
    }

    [Fact]
    public async Task GetStoragePathAsync_ReturnsNull_OnNotFound()
    {
        var client = MakeClient(HttpStatusCode.NotFound);

        var result = await client.GetStoragePathAsync("upload-missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoragePathAsync_ReturnsNull_OnServerError()
    {
        var client = MakeClient(HttpStatusCode.InternalServerError);

        var result = await client.GetStoragePathAsync("upload-123");

        Assert.Null(result);
    }
}
