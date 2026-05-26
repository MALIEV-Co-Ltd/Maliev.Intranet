using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Unit tests for <see cref="ProjectsController"/> using a mock HTTP client.
/// </summary>
public class ProjectsControllerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly ILogger<ProjectsController> Logger =
        NullLogger<ProjectsController>.Instance;

    private static ProjectServiceClient CreateClient<T>(T responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(responseBody) }));
        return new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static ProjectServiceClient CreateRawClient(HttpStatusCode status)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        return new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static JobServiceClient StubJobClient()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        return new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static FacilityServiceClient StubFacilityClient()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        return new FacilityServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    // ── GET (list) ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ShouldReturnOk_WithPagedResponse()
    {
        var paged = new PagedResponse<ProjectSummaryDto> { Data = new List<ProjectSummaryDto>() };
        var controller = new ProjectsController(CreateClient(paged), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Get(ct: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PagedResponse<ProjectSummaryDto>>(ok.Value);
    }

    [Fact]
    public async Task Get_WithStatusFilter_ShouldPassQueryToClient()
    {
        // Verify the query string is forwarded (handler logs requests)
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedUrl = req.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PagedResponse<ProjectSummaryDto>())
            });
        });
        var client = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new ProjectsController(client, StubJobClient(), StubFacilityClient(), Logger);

        await controller.Get(status: "Configuring", ct: CancellationToken.None);

        Assert.NotNull(capturedUrl);
        Assert.Contains("status=Configuring", capturedUrl);
    }

    // ── GET by ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        var project = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-001" };
        var controller = new ProjectsController(CreateClient(project), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetById(project.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_WhenCustomerServiceAvailable_ShouldEnrichCustomerProfileImage()
    {
        var customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        const string ProfileImageUrl = "https://lh3.googleusercontent.com/a/axion";
        var project = new ProjectDetailDto
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "PRJ-001",
            CustomerId = customerId,
            CustomerName = "Axion Robotics"
        };
        var customerHandler = new MockHttpMessageHandler((request, _) =>
        {
            var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
            if (pathAndQuery.Equals($"/customer/v1/customers/{customerId}", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CustomerDetailDto
                    {
                        Id = customerId,
                        Name = "Axion Robotics",
                        ProfileImageUrl = ProfileImageUrl
                    })
                });
            }

            if (pathAndQuery.StartsWith("/customer/v1/addresses", StringComparison.Ordinal)
                || pathAndQuery.StartsWith("/customer/v1/internal-notes", StringComparison.Ordinal)
                || pathAndQuery.StartsWith("/customer/v1/ndas", StringComparison.Ordinal)
                || pathAndQuery.StartsWith("/customer/v1/documents", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(Array.Empty<object>())
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var controller = new ProjectsController(
            CreateClient(project),
            StubJobClient(),
            StubFacilityClient(),
            Logger,
            customerClient: new CustomerServiceClient(
                new HttpClient(customerHandler) { BaseAddress = new Uri("http://test") },
                NullLogger<CustomerServiceClient>.Instance));

        var result = await controller.GetById(project.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectDetailDto>(ok.Value);
        Assert.Equal(ProfileImageUrl, dto.CustomerProfileImageUrl);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NotFound), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPartLargeThumbnailUrl_WhenPartHasLargeThumbnail_ShouldReturnSignedUrl()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        const string StoragePath = "customers/axion/projects/prj/bracket-left_thumb_1200.webp";
        const string SignedUrl = "https://signed.example/bracket-left_thumb_1200.webp";
        HttpRequestMessage? downstreamRequest = null;
        string? downstreamBody = null;
        var project = new ProjectDetailDto
        {
            Id = projectId,
            Parts =
            [
                new ProjectPartDto
                {
                    Id = partId,
                    ThumbnailLargeGcsPath = StoragePath
                }
            ]
        };
        var uploadHandler = new MockHttpMessageHandler(async (request, _) =>
        {
            downstreamRequest = request;
            downstreamBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = SignedUrl })
            };
        });
        var controller = new ProjectsController(
            CreateClient(project),
            StubJobClient(),
            StubFacilityClient(),
            Logger,
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://test") }));

        var result = await controller.GetPartLargeThumbnailUrl(projectId, partId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var urlProperty = ok.Value?.GetType().GetProperty("Url");
        Assert.NotNull(urlProperty);
        Assert.Equal(SignedUrl, urlProperty.GetValue(ok.Value));
        Assert.NotNull(downstreamRequest);
        Assert.Equal(HttpMethod.Post, downstreamRequest.Method);
        Assert.Equal("/upload/v1/files/by-path/signed-url", downstreamRequest.RequestUri?.AbsolutePath);
        Assert.NotNull(downstreamBody);
        var payload = JsonDocument.Parse(downstreamBody).RootElement;
        Assert.Equal(StoragePath, payload.GetProperty("storagePath").GetString());
        Assert.Equal(60, payload.GetProperty("expirationMinutes").GetInt32());
    }

    [Fact]
    public async Task GetPartLargeThumbnailUrl_WhenPartHasNoLargeThumbnail_ShouldReturnNotFound()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var uploadWasCalled = false;
        var project = new ProjectDetailDto
        {
            Id = projectId,
            Parts =
            [
                new ProjectPartDto { Id = partId }
            ]
        };
        var uploadHandler = new MockHttpMessageHandler((request, _) =>
        {
            uploadWasCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var controller = new ProjectsController(
            CreateClient(project),
            StubJobClient(),
            StubFacilityClient(),
            Logger,
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://test") }));

        var result = await controller.GetPartLargeThumbnailUrl(projectId, partId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.False(uploadWasCalled);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturn201Created()
    {
        var created = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-002" };
        var controller = new ProjectsController(CreateClient(created, HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Create(new CreateProjectRequest
        {
            CustomerId = Guid.NewGuid(),
            Title = "Test Project"
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenDownstreamFails_ShouldReturn502()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.InternalServerError), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Create(new CreateProjectRequest
        {
            CustomerId = Guid.NewGuid(),
            Title = "Test"
        }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Update(Guid.NewGuid(), new { title = "Updated" }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.Conflict), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Update(Guid.NewGuid(), new { title = "Conflict" }, CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(409, status.StatusCode);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NoContent), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Parts ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddPart_WhenSuccessful_ShouldReturnOk()
    {
        var part = new ProjectPartDto { Id = Guid.NewGuid(), FileName = "bracket.stl" };
        var controller = new ProjectsController(CreateClient(part), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.AddPart(Guid.NewGuid(),
            new AddProjectPartRequest { FileId = Guid.NewGuid(), FileName = "bracket.stl" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task AddPart_WhenProjectServiceReturnsBadRequest_ShouldForwardBadRequest()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.BadRequest), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.AddPart(Guid.NewGuid(),
            new AddProjectPartRequest { FileId = Guid.NewGuid(), FileName = "bracket.stl" },
            CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, status.StatusCode);
    }

    [Fact]
    public async Task Duplicate_WhenSuccessful_CreatesCopiedProjectPartsFilesAndClonesAnalysisStatus()
    {
        var sourceProjectId = Guid.NewGuid();
        var duplicateProjectId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var sourceFileId = Guid.NewGuid();
        var copiedFileId = Guid.NewGuid();
        JsonElement? createdProjectPayload = null;
        JsonElement? addedPartPayload = null;

        var sourceProject = new ProjectDetailDto
        {
            Id = sourceProjectId,
            CustomerId = customerId,
            CustomerName = "ACME",
            Title = "Bracket",
            Currency = "THB",
            Parts =
            [
                new ProjectPartDto
                {
                    Id = Guid.NewGuid(),
                    FileId = sourceFileId,
                    FileName = "bracket.step",
                    FileReference = $"customers/{customerId}/projects/{sourceProjectId}/source/bracket.step",
                    ThumbnailSmallGcsPath = "customers/c1/projects/p1/source/bracket_small.webp",
                    ThumbnailLargeGcsPath = "customers/c1/projects/p1/source/bracket_large.webp",
                    GlbStoragePath = "customers/c1/projects/p1/source/bracket_viewer.glb",
                    OverlayPaths = new Dictionary<string, string>
                    {
                        ["CNC__tool_access"] = "customers/c1/projects/p1/source/bracket_tool.glb"
                    },
                    ProcessType = "CNC_MILL",
                    MaterialName = "Aluminium 6061",
                    MaterialCode = "AL6061",
                    Quantity = 4,
                    Finish = "Anodized",
                    Color = "Black",
                    Tolerance = "ISO 2768-m",
                    RoughnessCode = "Ra1.6",
                    MarkingType = PartMarkingType.Engraving,
                    MarkingText = "PN-100",
                    DfmAcknowledged = true,
                    HasThreadedHoles = true,
                    ThreadedHoleSpec = "M6",
                    ThreadedHoleCount = 4,
                    HasInserts = true,
                    InsertType = InsertType.HeatSet,
                    InsertCount = 2,
                    BagAndTag = false,
                    InspectionLevel = InspectionLevel.Dimensional,
                    Certificates = ["MaterialCert"],
                    DrawingFiles =
                    [
                        new ProjectPartAttachmentDto
                        {
                            FileId = Guid.NewGuid(),
                            FileName = "drawing.pdf",
                            StoragePath = "customers/c1/projects/p1/drawing.pdf"
                        }
                    ],
                    ProcessConfig = new Dictionary<string, string>
                    {
                        ["anodizeColor"] = "Black"
                    },
                    BodyCount = 2,
                    BodiesJson = """[{"index":0,"name":"Body_01"}]""",
                    SelectedBodyIndex = 0
                }
            ]
        };
        var createdProject = new ProjectDetailDto
        {
            Id = duplicateProjectId,
            CustomerId = customerId,
            CustomerName = "ACME",
            Title = "Bracket (Copy)",
            Currency = "THB"
        };

        var projectHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{sourceProjectId}", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(sourceProject) };

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/project/v1/projects", StringComparison.Ordinal))
            {
                createdProjectPayload = await req.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(createdProject) };
            }

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{duplicateProjectId}/parts", StringComparison.Ordinal))
            {
                addedPartPayload = await req.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        id = Guid.NewGuid(),
                        fileId = copiedFileId,
                        fileName = "bracket.step",
                        fileReference = addedPartPayload.Value.GetProperty("fileReference").GetString(),
                        thumbnailSmallGcsPath = addedPartPayload.Value.GetProperty("thumbnailSmallGcsPath").GetString(),
                        glbStoragePath = addedPartPayload.Value.GetProperty("glbStoragePath").GetString(),
                        overlayPaths = new Dictionary<string, string> { ["CNC__tool_access"] = "copied-overlay.glb" },
                        processType = "CNC_Milling",
                        quantity = 4,
                        roughnessCode = "Ra1.6",
                        markingType = "Engraving",
                        insertType = "HeatSet",
                        inspectionLevel = "Dimensional",
                        certificates = new[] { "MaterialCert" },
                        processConfig = new Dictionary<string, string> { ["anodizeColor"] = "Black" },
                        bodyCount = 2,
                        bodiesJson = """[{"index":0,"name":"Body_01"}]""",
                        selectedBodyIndex = 0
                    })
                };
            }

            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{duplicateProjectId}", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(createdProject) };

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var uploadHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/upload/v1/admin/copy-file-with-metadata", StringComparison.Ordinal))
            {
                var body = await req.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                var sourcePath = body.GetProperty("sourcePath").GetString()!;
                var fileName = body.GetProperty("fileName").GetString()!;
                var fileId = sourcePath.EndsWith("drawing.pdf", StringComparison.Ordinal) ? Guid.NewGuid() : copiedFileId;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CopyFileWithMetadataResponse
                    {
                        FileId = fileId.ToString(),
                        UploadId = Guid.NewGuid().ToString(),
                        StoragePath = body.GetProperty("destinationPath").GetString()!,
                        FileName = fileName,
                        SizeBytes = 123,
                        ContentType = "application/octet-stream",
                        UploadedAt = DateTime.UtcNow
                    })
                };
            }

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/upload/v1/admin/copy-file", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { storagePath = "copied-artifact", sizeBytes = 1 }) };

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/upload/v1/files/by-path/signed-url", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { signedUrl = "https://signed.example/file" }) };

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var statusService = new Mock<IFileAnalysisStatusService>();
        var controller = new ProjectsController(
            new ProjectServiceClient(new HttpClient(projectHandler) { BaseAddress = new Uri("http://test") }),
            StubJobClient(),
            StubFacilityClient(),
            Logger,
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://test") }),
            statusService.Object);

        var result = await controller.Duplicate(sourceProjectId, new DuplicateProjectRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var duplicated = Assert.IsType<ProjectDetailDto>(ok.Value);
        Assert.Equal(duplicateProjectId, duplicated.Id);
        Assert.Single(duplicated.Parts);
        Assert.NotEqual(sourceFileId, duplicated.Parts[0].FileId);
        Assert.NotNull(createdProjectPayload);
        Assert.Equal("Bracket (Copy)", createdProjectPayload.Value.GetProperty("title").GetString());
        Assert.NotNull(addedPartPayload);
        Assert.Equal(copiedFileId, addedPartPayload.Value.GetProperty("fileId").GetGuid());
        Assert.True(addedPartPayload.Value.GetProperty("fileReference").GetString()!.Contains($"/projects/{duplicateProjectId}/", StringComparison.Ordinal));
        Assert.True(addedPartPayload.Value.TryGetProperty("overlayPaths", out _));
        Assert.Equal("Ra1.6", addedPartPayload.Value.GetProperty("roughnessCode").GetString());
        statusService.Verify(service => service.CloneStatusAsync(
            sourceProject.Parts[0].FileReference!,
            It.Is<string>(path => path.Contains($"/projects/{duplicateProjectId}/", StringComparison.Ordinal)),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Duplicate_WhenPartCreationFails_CleansUpCreatedProjectAndCopiedFiles()
    {
        var sourceProjectId = Guid.NewGuid();
        var duplicateProjectId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var copiedFileId = Guid.NewGuid();
        var deleteProjectCalled = false;
        var deletedFileId = Guid.Empty;

        var sourceProject = new ProjectDetailDto
        {
            Id = sourceProjectId,
            CustomerId = customerId,
            CustomerName = "ACME",
            Title = "Bracket",
            Currency = "THB",
            Parts =
            [
                new ProjectPartDto
                {
                    Id = Guid.NewGuid(),
                    FileId = Guid.NewGuid(),
                    FileName = "bracket.step",
                    FileReference = $"customers/{customerId}/projects/{sourceProjectId}/source/bracket.step",
                    ProcessType = "FDM",
                    Quantity = 1
                }
            ]
        };
        var createdProject = new ProjectDetailDto
        {
            Id = duplicateProjectId,
            CustomerId = customerId,
            CustomerName = "ACME",
            Title = "Bracket (Copy)",
            Currency = "THB"
        };

        var projectHandler = new MockHttpMessageHandler((req, ct) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{sourceProjectId}", StringComparison.Ordinal))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(sourceProject) });

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/project/v1/projects", StringComparison.Ordinal))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(createdProject) });

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{duplicateProjectId}/parts", StringComparison.Ordinal))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = JsonContent.Create(new { error = "failed" }) });

            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{duplicateProjectId}", StringComparison.Ordinal))
            {
                deleteProjectCalled = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var uploadHandler = new MockHttpMessageHandler((req, ct) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/upload/v1/admin/copy-file-with-metadata", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CopyFileWithMetadataResponse
                    {
                        FileId = copiedFileId.ToString(),
                        UploadId = Guid.NewGuid().ToString(),
                        StoragePath = $"customers/{customerId}/projects/{duplicateProjectId}/copy/bracket.step",
                        FileName = "bracket.step",
                        SizeBytes = 123,
                        ContentType = "application/octet-stream",
                        UploadedAt = DateTime.UtcNow
                    })
                });
            }

            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath.EndsWith($"/upload/v1/files/{copiedFileId}", StringComparison.Ordinal))
            {
                deletedFileId = copiedFileId;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var controller = new ProjectsController(
            new ProjectServiceClient(new HttpClient(projectHandler) { BaseAddress = new Uri("http://test") }),
            StubJobClient(),
            StubFacilityClient(),
            Logger,
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://test") }),
            Mock.Of<IFileAnalysisStatusService>());

        var result = await controller.Duplicate(sourceProjectId, new DuplicateProjectRequest(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
        Assert.True(deleteProjectCalled);
        Assert.Equal(copiedFileId, deletedFileId);
    }

    [Fact]
    public async Task UpdatePart_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.UpdatePart(Guid.NewGuid(), Guid.NewGuid(),
            new UpdateProjectPartRequest { ProcessType = "FDM", Quantity = 2 },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePart_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NoContent), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.DeletePart(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Pricing ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPartPrice_WhenSuccessful_ShouldReturnBreakdown()
    {
        var projectServicePart = new { effectiveUnitPrice = 250m };
        var controller = new ProjectsController(CreateClient(projectServicePart), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartPrice(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectPriceBreakdownDto>(ok.Value);
        Assert.Equal(250m, dto.TotalPerUnit);
    }

    [Fact]
    public async Task GetPartPrice_WhenDownstreamFails_ShouldReturn502()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.ServiceUnavailable), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartPrice(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
    }

    [Fact]
    public async Task ConfirmPartPrice_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.ConfirmPartPrice(
            Guid.NewGuid(), Guid.NewGuid(),
            new ConfirmPartPriceRequest { ConfirmedUnitPrice = 300m },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Quotation lifecycle ───────────────────────────────────────────────────

    [Fact]
    public async Task GenerateQuotation_WhenProjectResponseContainsQuotationId_GeneratesPdfWithoutReloadingProject()
    {
        var projectId = Guid.NewGuid();
        var quotationId = Guid.NewGuid();
        const string QuotationNumber = "Q-3EF52DCB";
        var projectReloadCount = 0;
        var attachedPdfArtifact = false;
        JsonElement? pdfRequestPayload = null;
        var generatedProject = new ProjectDetailDto
        {
            Id = projectId,
            ProjectNumber = "PRJ-2026-0001",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Somchai Patel",
            QuotationId = quotationId,
            QuotationNumber = QuotationNumber,
            CurrentQuotationVersionNumber = 1,
            CreatedAt = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Currency = "THB",
            TotalPrice = 1500m
        };
        var quotation = new QuotationDetailDto
        {
            Id = quotationId,
            QuotationNumber = QuotationNumber,
            CustomerId = generatedProject.CustomerId,
            CustomerName = generatedProject.CustomerName,
            CurrentVersionNumber = 1,
            CurrencyCode = "THB",
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
            SubTotal = 1500m,
            Total = 1655m,
            CreatedAt = DateTime.UtcNow,
            Versions =
            [
                new QuotationVersionDto
                {
                    Id = Guid.NewGuid(),
                    VersionNumber = 1,
                    CurrencyCode = "THB",
                    ShippingCost = 200m,
                    ManualDiscountAmount = 45m,
                    TaxAmount = 0m,
                    TotalPrice = 1655m,
                    SpecialTerms = "50% deposit before production.",
                    LineItems =
                    [
                        new QuotationItemDto
                        {
                            Description = "CNC fixture",
                            Quantity = 1m,
                            UnitPrice = 1500m
                        }
                    ]
                }
            ]
        };
        var projectHandler = new MockHttpMessageHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Post &&
                req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{projectId}/generate-quotation", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(generatedProject)
                });
            }

            if (req.Method == HttpMethod.Get &&
                req.RequestUri!.AbsolutePath.EndsWith($"/project/v1/projects/{projectId}", StringComparison.Ordinal))
            {
                projectReloadCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var quotationHandler = new MockHttpMessageHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Post &&
                req.RequestUri!.AbsolutePath.EndsWith($"/quotation/v1/quotations/{quotationId}/versions/1/pdf-artifact", StringComparison.Ordinal))
            {
                attachedPdfArtifact = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.EndsWith($"/quotation/v1/quotations/{quotationId}", req.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(quotation)
            });
        });
        var pdfHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.EndsWith("/pdf/v1/generations/generate", req.RequestUri!.AbsolutePath);
            pdfRequestPayload = await req.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { storageUrl = "https://storage.example/quote.pdf" })
            };
        });
        var controller = new ProjectsController(
            new ProjectServiceClient(new HttpClient(projectHandler) { BaseAddress = new Uri("http://test") }),
            StubJobClient(),
            StubFacilityClient(),
            Logger,
            quotationClient: new QuotationServiceClient(new HttpClient(quotationHandler) { BaseAddress = new Uri("http://test") }),
            pdfClient: new PdfServiceClient(new HttpClient(pdfHandler) { BaseAddress = new Uri("http://test") }));

        var result = await controller.GenerateQuotation(projectId, new GenerateQuotationRequest(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, projectReloadCount);
        Assert.NotNull(pdfRequestPayload);
        Assert.Equal("Quotation", pdfRequestPayload.Value.GetProperty("documentType").GetString());
        Assert.Equal(QuotationNumber, pdfRequestPayload.Value.GetProperty("referenceId").GetString());
        Assert.Equal("Quotation", pdfRequestPayload.Value.GetProperty("templateCode").GetString());
        Assert.True(attachedPdfArtifact);
    }

    [Fact]
    public async Task GenerateQuotation_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.UnprocessableEntity), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GenerateQuotation(Guid.NewGuid(), new GenerateQuotationRequest(), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(422, status.StatusCode);
    }

    [Fact]
    public async Task AcceptQuotation_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.AcceptQuotation(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Routing ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPartRouting_MissingProcessType_ReturnsBadRequest()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPartRouting_EmptyProcessType_ReturnsBadRequest()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPartRouting_BothServicesSucceed_ReturnsRoutingWithQueueAndMachine()
    {
        var queueJson = "{\"FDM\":3}";
        var machineJson = "{\"Items\":[{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"AssetCode\":\"MAL-FDM-001\",\"Name\":\"Bambu X1C\",\"Category\":\"FdmPrinter\",\"Status\":\"Active\",\"UpdatedAt\":\"2026-01-01T00:00:00Z\"}],\"TotalCount\":1,\"Page\":1}";

        var jobHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(queueJson, System.Text.Encoding.UTF8, "application/json") }));
        var jobClient = new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") });

        var facilityHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(machineJson, System.Text.Encoding.UTF8, "application/json") }));
        var facilityClient = new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") });

        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), jobClient, facilityClient, Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "FDM", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal(3, dto.QueueAhead);
        Assert.Equal("MAL-FDM-001", dto.MachineCode);
        Assert.Equal("Bambu X1C", dto.MachineName);
        Assert.Equal(new Guid("11111111-1111-1111-1111-111111111111"), dto.MachineId);
        // Non-CNC: setupDays=1 + queueAhead=3 = 4 days minimum
        Assert.True(dto.EstimatedStartDate >= DateTimeOffset.UtcNow.AddDays(3));
    }

    [Fact]
    public async Task GetPartRouting_JobServiceDown_ReturnsZeroQueueWithMachine()
    {
        var machineJson = "{\"Items\":[{\"Id\":\"22222222-2222-2222-2222-222222222222\",\"AssetCode\":\"MAL-FDM-002\",\"Name\":\"Bambu P1S\",\"Category\":\"FdmPrinter\",\"Status\":\"Active\",\"UpdatedAt\":\"2026-01-01T00:00:00Z\"}],\"TotalCount\":1,\"Page\":1}";

        var facilityHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(machineJson, System.Text.Encoding.UTF8, "application/json") }));
        var facilityClient = new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") });

        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), facilityClient, Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "FDM", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal(0, dto.QueueAhead);
        Assert.Equal("MAL-FDM-002", dto.MachineCode);
    }

    [Fact]
    public async Task GetPartRouting_FacilityServiceDown_ReturnsFallbackMachineFields()
    {
        var queueJson = "{\"FDM\":1}";
        var jobHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(queueJson, System.Text.Encoding.UTF8, "application/json") }));
        var jobClient = new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") });

        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), jobClient, StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "FDM", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal(1, dto.QueueAhead);
        Assert.Equal("TBD", dto.MachineCode);
        Assert.Equal("Unassigned", dto.MachineName);
        Assert.Equal(Guid.Empty, dto.MachineId);
    }

    [Fact]
    public async Task GetPartRouting_CncProcess_UsesTwoSetupDays()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);
        var before = DateTimeOffset.UtcNow.AddDays(2); // setup=2, queue=0

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "CNC_Milling", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.True(dto.EstimatedStartDate >= before);
    }

    [Fact]
    public async Task GetPartRouting_ProjectServiceCncMilling_UsesCanonicalQueueAndCncMachineCategory()
    {
        string? capturedQueueUrl = null;
        string? capturedEquipmentUrl = null;
        var machineId = Guid.NewGuid();
        var jobHandler = new MockHttpMessageHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/job/v1/jobs/queue-depth", StringComparison.Ordinal))
            {
                capturedQueueUrl = req.RequestUri.ToString();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new Dictionary<string, int> { ["CNC_MILL"] = 3 })
                });
            }

            if (req.RequestUri!.AbsolutePath.Contains("/job/v1/jobs/machine/", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(Array.Empty<MachineScheduleItemDto>())
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var facilityHandler = new MockHttpMessageHandler((req, _) =>
        {
            capturedEquipmentUrl = req.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new FacilityPagedResult<EquipmentSummaryDto>
                {
                    Items =
                    [
                        new EquipmentSummaryDto
                        {
                            Id = machineId,
                            AssetCode = "MAL-CNC-0001",
                            Name = "CNC Mill 1",
                            Category = "CncMachine",
                            Status = "Active"
                        }
                    ],
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 50
                })
            });
        });
        var controller = new ProjectsController(
            CreateRawClient(HttpStatusCode.OK),
            new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") }),
            new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") }),
            Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "CNC_Milling", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal(3, dto.QueueAhead);
        Assert.Equal(machineId, dto.MachineId);
        Assert.Equal("MAL-CNC-0001", dto.MachineCode);
        Assert.Contains("technology=CNC_MILL", capturedQueueUrl);
        Assert.Contains("category=CncMachine", capturedEquipmentUrl);
    }

    [Fact]
    public async Task GetProductionPlan_EnrichesThumbnailAndHumanizesConfiguration()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        const string ThumbnailPath = "customers/seed/projects/prj/part_thumb_small.webp";
        const string SignedThumbnailUrl = "https://storage.example/part_thumb_small.webp";
        var machineId = Guid.NewGuid();

        var project = new ProjectDetailDto
        {
            Id = projectId,
            ProjectNumber = "PRJ-2026-0001",
            Parts =
            [
                new ProjectPartDto
                {
                    Id = partId,
                    FileName = "fixture-base.step",
                    ThumbnailSmallGcsPath = ThumbnailPath,
                    ProcessType = "CNC_Milling",
                    MaterialName = "Aluminium 6061-T6",
                    Finish = "AS_MACHINED",
                    Color = "FDM_STD",
                    Tolerance = "ISO2768-m",
                    Quantity = 2,
                    Status = "Confirmed",
                    Dimensions = new ModelDimensionsDto { X = 80, Y = 48, Z = 12 }
                }
            ]
        };

        var projectClient = CreateClient(project);
        var uploadHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = SignedThumbnailUrl })
            }));
        var jobHandler = new MockHttpMessageHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/job/v1/jobs/planning-holds", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(Array.Empty<ProductionPlanningHoldDto>())
                });
            }

            if (req.RequestUri!.AbsolutePath.EndsWith("/job/v1/jobs/queue-depth", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new Dictionary<string, int> { ["CNC_MILL"] = 2 })
                });
            }

            if (req.RequestUri!.AbsolutePath.Contains("/job/v1/jobs/machine/", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(Array.Empty<MachineScheduleItemDto>())
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var facilityHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new FacilityPagedResult<EquipmentSummaryDto>
                {
                    Items =
                    [
                        new EquipmentSummaryDto
                        {
                            Id = machineId,
                            AssetCode = "MAL-CNC-001",
                            Name = "HAAS VF2",
                            Category = "CncMachine",
                            Status = "Active"
                        }
                    ],
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 50
                })
            }));
        var controller = new ProjectsController(
            projectClient,
            new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") }),
            new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") }),
            Logger,
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://test") }));

        var result = await controller.GetProductionPlan(projectId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var plan = Assert.IsType<ProjectProductionPlanDto>(ok.Value);
        var part = Assert.Single(plan.Parts);
        Assert.Equal(SignedThumbnailUrl, part.ThumbnailUrl);
        Assert.Equal("As Machined / Standard FDM settings / ISO 2768-m", part.Configuration);
        Assert.Equal(machineId, part.Routing?.MachineId);
        Assert.Equal(2, part.Routing?.QueueAhead);
        Assert.True(part.CanCreateHold);
    }

    [Fact]
    public async Task GetProductionPlan_IncludesMixedProcessScheduleBoardWithReservedSlots()
    {
        var projectId = Guid.NewGuid();
        var fdmPartId = Guid.NewGuid();
        var cncPartId = Guid.NewGuid();
        var cncHoldId = Guid.NewGuid();
        var fdmMachineId = Guid.NewGuid();
        var cncMachineId = Guid.NewGuid();
        var rangeStart = DateTime.UtcNow.Date.AddDays(1);

        var project = new ProjectDetailDto
        {
            Id = projectId,
            ProjectNumber = "PRJ-2026-0002",
            Parts =
            [
                new ProjectPartDto
                {
                    Id = fdmPartId,
                    FileName = "fdm-cover.stl",
                    ProcessType = "FDM",
                    MaterialName = "ABS",
                    Quantity = 4,
                    Status = "Quoted",
                    Dimensions = new ModelDimensionsDto { X = 12, Y = 12, Z = 7 }
                },
                new ProjectPartDto
                {
                    Id = cncPartId,
                    FileName = "cnc-fixture.step",
                    ProcessType = "CNC_Milling",
                    MaterialName = "Aluminium 6061-T6",
                    Quantity = 1,
                    Status = "Quoted",
                    Dimensions = new ModelDimensionsDto { X = 80, Y = 40, Z = 12 }
                }
            ]
        };

        var projectClient = CreateClient(project);
        var jobHandler = new MockHttpMessageHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/job/v1/jobs/planning-holds", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new[]
                    {
                        new ProductionPlanningHoldDto
                        {
                            Id = cncHoldId,
                            ProjectId = projectId,
                            ProjectPartId = cncPartId,
                            Technology = "CNC_MILL",
                            MachineId = "MAL-CNC-001",
                            MachineName = "HAAS VF2",
                            QueuePosition = 2,
                            ScheduledStartTime = rangeStart.AddHours(5),
                            ScheduledEndTime = rangeStart.AddHours(7),
                            SetupTimeMinutes = 60,
                            ProductionTimeMinutes = 60,
                            Quantity = 1,
                            Status = "Active",
                            ExpiresAt = DateTime.UtcNow.AddHours(72)
                        }
                    })
                });
            }

            if (req.RequestUri!.AbsolutePath.EndsWith("/job/v1/jobs/queue-depth", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new Dictionary<string, int>
                    {
                        ["FDM"] = 3,
                        ["CNC_MILL"] = 2
                    })
                });
            }

            if (req.RequestUri!.AbsolutePath.EndsWith("/job/v1/jobs/schedule", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new[]
                    {
                        new
                        {
                            MachineId = "MAL-FDM-001",
                            Schedule = new[]
                            {
                                new
                                {
                                    JobId = Guid.NewGuid(),
                                    Technology = "FDM",
                                    ScheduledStart = rangeStart.AddHours(1),
                                    ScheduledEnd = rangeStart.AddHours(3),
                                    SetupMinutes = 15,
                                    PrintMinutes = 105,
                                    QueuePosition = 1,
                                    Status = "Queued",
                                    OrderId = Guid.NewGuid(),
                                    IsHold = false,
                                    HoldId = (Guid?)null,
                                    ProjectId = (Guid?)null,
                                    ProjectPartId = (Guid?)null,
                                    ExpiresAt = (DateTime?)null
                                }
                            }
                        },
                        new
                        {
                            MachineId = "MAL-CNC-001",
                            Schedule = new[]
                            {
                                new
                                {
                                    JobId = cncHoldId,
                                    Technology = "CNC_MILL",
                                    ScheduledStart = rangeStart.AddHours(5),
                                    ScheduledEnd = rangeStart.AddHours(7),
                                    SetupMinutes = 60,
                                    PrintMinutes = 60,
                                    QueuePosition = 2,
                                    Status = "Planning Hold",
                                    OrderId = Guid.Empty,
                                    IsHold = true,
                                    HoldId = (Guid?)cncHoldId,
                                    ProjectId = (Guid?)projectId,
                                    ProjectPartId = (Guid?)cncPartId,
                                    ExpiresAt = (DateTime?)DateTime.UtcNow.AddHours(72)
                                }
                            }
                        }
                    })
                });
            }

            if (req.RequestUri!.AbsolutePath.Contains("/job/v1/jobs/machine/", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(Array.Empty<MachineScheduleItemDto>())
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var facilityHandler = new MockHttpMessageHandler((req, _) =>
        {
            var category = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query).Get("category");
            var items = category switch
            {
                "FdmPrinter" =>
                [
                    new EquipmentSummaryDto
                    {
                        Id = fdmMachineId,
                        AssetCode = "MAL-FDM-001",
                        Name = "Bambulab X1C #1",
                        Category = "FdmPrinter",
                        Status = "Active"
                    }
                ],
                "CncMachine" =>
                [
                    new EquipmentSummaryDto
                    {
                        Id = cncMachineId,
                        AssetCode = "MAL-CNC-001",
                        Name = "HAAS VF2",
                        Category = "CncMachine",
                        Status = "Active"
                    }
                ],
                _ => Array.Empty<EquipmentSummaryDto>()
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new FacilityPagedResult<EquipmentSummaryDto>
                {
                    Items = items,
                    TotalCount = items.Count(),
                    Page = 1,
                    PageSize = 50
                })
            });
        });
        var controller = new ProjectsController(
            projectClient,
            new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") }),
            new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") }),
            Logger);

        var result = await controller.GetProductionPlan(projectId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var plan = Assert.IsType<ProjectProductionPlanDto>(ok.Value);
        Assert.NotNull(plan.ScheduleBoard);
        Assert.Contains(plan.ScheduleBoard.Machines, machine => machine.MachineId == "MAL-FDM-001");
        var cncMachine = Assert.Single(plan.ScheduleBoard.Machines, machine => machine.MachineId == "MAL-CNC-001");
        Assert.Contains(cncMachine.Slots, slot =>
            slot.HoldId == cncHoldId &&
            slot.ProjectPartId == cncPartId &&
            slot.IsHold &&
            slot.IsCurrentProject);
        Assert.Contains(plan.ScheduleBoard.Machines.SelectMany(machine => machine.Slots), slot =>
            slot.ProjectPartId == fdmPartId &&
            slot.IsProposed &&
            slot.IsCurrentProject);
    }

    [Fact]
    public async Task GetPartRouting_UnknownProcessType_ReturnsOkWithFallbackMachineFields()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "UNKNOWN_PROCESS", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal("TBD", dto.MachineCode);
        Assert.Equal("Unassigned", dto.MachineName);
    }
}
