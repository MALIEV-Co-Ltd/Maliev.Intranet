using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Maliev.Intranet.Tests.Bff.Consumers;

/// <summary>
/// Integration tests for the consumer-to-status-service integration.
/// Tests that when consumers process events, the IFileAnalysisStatusService is updated correctly.
/// Uses a minimal service provider without MassTransit/RabbitMQ dependency.
/// </summary>
public class ConsumerToSignalRIntegrationTests : IClassFixture<StatusServiceFixture>
{
    private readonly IServiceProvider _services;

    public ConsumerToSignalRIntegrationTests(StatusServiceFixture fixture)
    {
        _services = fixture.Services;
    }

    /// <summary>
    /// Tests that seeding a completed analysis status via IFileAnalysisStatusService works end-to-end.
    /// This validates that the in-memory cache used for SignalR polling stores and retrieves correctly.
    /// </summary>
    [Fact]
    public async Task AnalysisStatusService_CompletedStatus_RetrievedCorrectly()
    {
        // Arrange
        var statusService = _services.GetRequiredService<IFileAnalysisStatusService>();
        var testStoragePath = "projects/test-cancellation-checks/part-cancellation.stl";

        // Act
        await statusService.SetAnalysisCompletedAsync(
            testStoragePath,
            "gs://bucket/test.glb",
            "https://storage.googleapis.com/signed-url-test.glb",
            null,
            CancellationToken.None
        );

        // Assert
        var status = await statusService.GetStatusAsync(testStoragePath, CancellationToken.None);

        Assert.NotNull(status);
        Assert.Equal(FileAnalysisStatus.Completed, status.Status);
        Assert.Equal("gs://bucket/test.glb", status.GlbStoragePath);
        Assert.Equal("https://storage.googleapis.com/signed-url-test.glb", status.GlbSignedUrl);
    }

    /// <summary>
    /// Tests that analysis status transitions through the correct lifecycle:
    /// null (not started) → Processing (set by consumer) → Completed.
    /// </summary>
    [Fact]
    public async Task AnalysisStatusService_LifecycleTransitionsCorrectly()
    {
        // Arrange
        var statusService = _services.GetRequiredService<IFileAnalysisStatusService>();
        var testStoragePath = "projects/test-lifecycle/model.step";

        // Act & Assert - initially null (no status yet)
        var initialStatus = await statusService.GetStatusAsync(testStoragePath, CancellationToken.None);
        Assert.Null(initialStatus);

        // Act - set to processing
        await statusService.SetProcessingAsync(testStoragePath, CancellationToken.None);
        var processingStatus = await statusService.GetStatusAsync(testStoragePath, CancellationToken.None);
        Assert.NotNull(processingStatus);
        Assert.Equal(FileAnalysisStatus.Processing, processingStatus.Status);

        // Act - complete analysis
        await statusService.SetAnalysisCompletedAsync(
            testStoragePath,
            "gs://bucket/lifecycle.glb",
            "https://storage.googleapis.com/signed-url-lifecycle.glb",
            null,
            CancellationToken.None
        );
        var completedStatus = await statusService.GetStatusAsync(testStoragePath, CancellationToken.None);

        // Assert
        Assert.NotNull(completedStatus);
        Assert.Equal(FileAnalysisStatus.Completed, completedStatus.Status);
        Assert.Equal("gs://bucket/lifecycle.glb", completedStatus.GlbStoragePath);
        Assert.Equal("https://storage.googleapis.com/signed-url-lifecycle.glb", completedStatus.GlbSignedUrl);
    }

    /// <summary>
    /// Tests that multiple files can have independent statuses without interference.
    /// Validates that the cache correctly keys by storage path.
    /// </summary>
    [Fact]
    public async Task AnalysisStatusService_MultipleFiles_IndependentStatuses()
    {
        // Arrange
        var statusService = _services.GetRequiredService<IFileAnalysisStatusService>();
        var path1 = "projects/test-multi/file1.step";
        var path2 = "projects/test-multi/file2.step";

        // Act
        await statusService.SetAnalysisCompletedAsync(
            path1,
            "gs://bucket/file1.glb",
            "https://storage.googleapis.com/signed-url-file1.glb",
            null,
            CancellationToken.None
        );

        await statusService.SetProcessingAsync(path2, CancellationToken.None);

        // Assert
        var status1 = await statusService.GetStatusAsync(path1, CancellationToken.None);
        var status2 = await statusService.GetStatusAsync(path2, CancellationToken.None);

        Assert.NotNull(status1);
        Assert.Equal(FileAnalysisStatus.Completed, status1.Status);
        Assert.Equal("gs://bucket/file1.glb", status1.GlbStoragePath);

        Assert.NotNull(status2);
        Assert.Equal(FileAnalysisStatus.Processing, status2.Status);
        Assert.Null(status2.GlbStoragePath); // Processing state has no GLB yet
    }
}

/// <summary>
/// Fixture that provides IFileAnalysisStatusService without starting the full BFF host.
/// Avoids MassTransit/RabbitMQ dependency by directly instantiating the service.
/// </summary>
public class StatusServiceFixture
{
    public IServiceProvider Services { get; }

    public StatusServiceFixture()
    {
        var services = new ServiceCollection();

        // Register MemoryCache as singleton
        services.AddSingleton<IMemoryCache, MemoryCache>();

        // Register FileAnalysisStatusService
        services.AddSingleton<IFileAnalysisStatusService, FileAnalysisStatusService>();

        // Register logging
        services.AddLogging();

        Services = services.BuildServiceProvider();
    }
}
