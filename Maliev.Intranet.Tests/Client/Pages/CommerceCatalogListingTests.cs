using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bunit;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class CommerceCatalogListingTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();
    private readonly List<HttpRequestMessage> _sentRequests = [];

    public CommerceCatalogListingTests()
    {
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddSingleton(new HttpClient(_httpHandler) { BaseAddress = new Uri("http://test/") });
        Services.AddScoped<CurrencyService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();

        _httpHandler.HandlerFunc = DefaultHandler;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task UploadMediaFilesAsync_AddsUploadedImagesInDisplayOrder()
    {
        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests)
            {
                _sentRequests.Add(request);
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/commerce/products/media")
            {
                var uploads = new List<BffUploadResponse>
                {
                    new()
                    {
                        UploadId = Guid.NewGuid().ToString(),
                        FileName = "front.png",
                        FileSize = 1024,
                        FileReference = "api/v1/commerce/products/media/front-upload",
                        SignedUrl = "https://signed.example/front.png"
                    },
                    new()
                    {
                        UploadId = Guid.NewGuid().ToString(),
                        FileName = "side.png",
                        FileSize = 2048,
                        FileReference = "api/v1/commerce/products/media/side-upload",
                        SignedUrl = "https://signed.example/side.png"
                    }
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(uploads), Encoding.UTF8, "application/json")
                };
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing>();
        SetPrivateField(cut.Instance, "_productForm", new CommerceProductMutationRequest
        {
            Handle = "pneumatic-injection-molding-machine-30g",
            Title = "Pneumatic Injection Molding Machine 30g"
        });

        IReadOnlyList<IBrowserFile> files =
        [
            new TestBrowserFile("front.png"),
            new TestBrowserFile("side.png")
        ];

        await InvokePrivateTaskWithArgsAsync(cut, "UploadMediaFilesAsync", files);

        var form = GetPrivateField<CommerceProductMutationRequest>(cut.Instance, "_productForm");
        Assert.Collection(
            form.Media.OrderBy(media => media.SortOrder),
            media =>
            {
                Assert.Equal("api/v1/commerce/products/media/front-upload", media.Url);
                Assert.Equal("front image", media.AltText);
                Assert.Equal(0, media.SortOrder);
            },
            media =>
            {
                Assert.Equal("api/v1/commerce/products/media/side-upload", media.Url);
                Assert.Equal("side image", media.AltText);
                Assert.Equal(1, media.SortOrder);
            });

        Assert.Contains(_sentRequests, request =>
            request.Method == HttpMethod.Post
            && request.RequestUri?.AbsolutePath == "/api/v1/commerce/products/media"
            && request.RequestUri.Query.Contains("handle=pneumatic-injection-molding-machine-30g", StringComparison.Ordinal));
    }

    [Fact]
    public void MediaOrderingActions_KeepPrimaryImageAtSortOrderZero()
    {
        var page = new global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing();
        var first = new CommerceProductMediaMutationRequest { Url = "https://cdn.example/front.png", SortOrder = 0 };
        var second = new CommerceProductMediaMutationRequest { Url = "https://cdn.example/side.png", SortOrder = 1 };
        var third = new CommerceProductMediaMutationRequest { Url = "https://cdn.example/detail.png", SortOrder = 2 };
        var form = new CommerceProductMutationRequest { Media = [first, second, third] };
        SetPrivateField(page, "_productForm", form);

        InvokePrivateVoidWithArgs(page, "SetPrimaryMedia", third);

        Assert.Collection(
            form.Media.OrderBy(media => media.SortOrder),
            media => Assert.Same(third, media),
            media => Assert.Same(first, media),
            media => Assert.Same(second, media));

        InvokePrivateVoidWithArgs(page, "MoveMedia", second, -1);

        Assert.Collection(
            form.Media.OrderBy(media => media.SortOrder),
            media =>
            {
                Assert.Same(third, media);
                Assert.Equal(0, media.SortOrder);
            },
            media =>
            {
                Assert.Same(second, media);
                Assert.Equal(1, media.SortOrder);
            },
            media =>
            {
                Assert.Same(first, media);
                Assert.Equal(2, media.SortOrder);
            });
    }

    [Fact]
    public async Task SaveListingAsync_RemovesBlankMediaBeforeCallingBff()
    {
        var productId = Guid.NewGuid();
        string? savedRequestBody = null;
        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests)
            {
                _sentRequests.Add(request);
            }

            if (request.Method == HttpMethod.Patch &&
                request.RequestUri?.AbsolutePath == $"/api/v1/commerce/products/{productId}")
            {
                savedRequestBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(ct);

                var saved = new CommerceProductDto
                {
                    Id = productId,
                    Handle = "pneumatic-injection-molding-machine-30g",
                    Title = "Pneumatic Injection Molding Machine 30g",
                    Summary = "Compact pneumatic injection molding machine.",
                    Description = "Draft listing.",
                    ProductType = "Injection molding machine",
                    Status = "Draft",
                    Variants =
                    [
                        new CommerceProductVariantDto
                        {
                            Id = Guid.NewGuid(),
                            Sku = "PIMM-30-125-200",
                            Title = "30g starter package",
                            PriceAmount = 99000m,
                            Currency = "THB"
                        }
                    ],
                    Media =
                    [
                        new CommerceProductMediaDto
                        {
                            Id = Guid.NewGuid(),
                            Url = "api/v1/commerce/products/media/front-upload",
                            AltText = "front image",
                            SortOrder = 0
                        }
                    ]
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(saved), Encoding.UTF8, "application/json")
                };
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing>();
        SetPrivateField<Guid?>(cut.Instance, "_editingProductId", productId);
        SetPrivateField(cut.Instance, "_productForm", new CommerceProductMutationRequest
        {
            Handle = "pneumatic-injection-molding-machine-30g",
            Title = "Pneumatic Injection Molding Machine 30g",
            Summary = "Compact pneumatic injection molding machine.",
            Description = "Draft listing.",
            ProductType = "Injection molding machine",
            Status = "Draft",
            Variants =
            [
                new CommerceProductVariantMutationRequest
                {
                    Sku = "PIMM-30-125-200",
                    Title = "30g starter package",
                    PriceAmount = 99000m,
                    Currency = "THB"
                }
            ],
            Media =
            [
                new CommerceProductMediaMutationRequest { Url = "   ", SortOrder = 0 },
                new CommerceProductMediaMutationRequest
                {
                    Url = "api/v1/commerce/products/media/front-upload",
                    AltText = "front image",
                    SortOrder = 1
                }
            ]
        });

        await InvokePrivateTaskWithArgsAsync(cut, "SaveListingAsync");

        Assert.NotNull(savedRequestBody);
        var payload = JsonSerializer.Deserialize<CommerceProductMutationRequest>(
            savedRequestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        var media = Assert.Single(payload.Media);
        Assert.Equal("api/v1/commerce/products/media/front-upload", media.Url);
        Assert.Equal(0, media.SortOrder);
    }

    private Task<HttpResponseMessage> DefaultHandler(HttpRequestMessage request, CancellationToken ct)
    {
        lock (_sentRequests)
        {
            _sentRequests.Add(request);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });
    }

    private static async Task InvokePrivateTaskWithArgsAsync(
        RenderedComponent<global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing> cut,
        string methodName,
        params object[] args)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        await cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, args)!);
    }

    private static void InvokePrivateVoidWithArgs(
        global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing page,
        string methodName,
        params object[] args)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(page, args);
    }

    private static void SetPrivateField<T>(
        global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing instance,
        string fieldName,
        T value)
    {
        var field = typeof(global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(
        global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing instance,
        string fieldName)
    {
        var field = typeof(global::Maliev.Intranet.Client.Pages.Commerce.CatalogListing)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(instance));
    }

    private sealed class TestBrowserFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public long Size { get; } = 1024;

        public string ContentType { get; } = "image/png";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            new MemoryStream(Encoding.UTF8.GetBytes("image"));
    }
}
