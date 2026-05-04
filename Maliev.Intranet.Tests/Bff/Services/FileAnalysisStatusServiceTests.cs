using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maliev.Intranet.Tests.Bff.Services;

public sealed class FileAnalysisStatusServiceTests
{
    [Fact]
    public async Task RegisterStoragePathAliasAsync_WhenLateOldStatusIsWritten_MirrorsStatusToNewPath()
    {
        const string OldPath = "projects/temp-project/part.stl";
        const string NewPath = "customers/customer-1/projects/temp-project/part.stl";

        var service = CreateService();

        await service.RegisterStoragePathAliasAsync(OldPath, NewPath);
        await service.SetDimensionsAsync(
            OldPath,
            new FileAnalysisDimensionsDto
            {
                X = 76,
                Y = 40,
                Z = 40,
                VolumeMm3 = 121600
            },
            isManifold: true);
        await service.SetThumbnailAsync(
            OldPath,
            "https://signed.example/thumb-small.webp",
            OldPath + "_thumb_256.webp");
        await service.SetAnalysisCompletedAsync(
            OldPath,
            OldPath + "_viewer.glb",
            "https://signed.example/viewer.glb");

        var status = await service.GetStatusAsync(NewPath);

        Assert.NotNull(status);
        Assert.Equal(NewPath, status.UploadId);
        Assert.Equal(FileAnalysisStatus.Completed, status.Status);
        Assert.Equal(76, status.Dimensions?.X);
        Assert.Equal(40, status.Dimensions?.Y);
        Assert.Equal(40, status.Dimensions?.Z);
        Assert.Equal(121600, status.Dimensions?.VolumeMm3);
        Assert.True(status.IsManifold);
        Assert.Equal("https://signed.example/thumb-small.webp", status.ThumbnailUrl);
        Assert.Equal("https://signed.example/thumb-small.webp", status.PreviewUrls?.ThumbnailSmall);
        Assert.Equal(NewPath + "_thumb_256.webp", status.PreviewUrls?.ThumbnailSmallGcsPath);
        Assert.Equal(NewPath + "_viewer.glb", status.GlbStoragePath);
        Assert.Equal("https://signed.example/viewer.glb", status.GlbSignedUrl);
    }

    [Fact]
    public async Task SetPreviewUrlsAsync_WhenIncomingThumbnailSmallIsMissing_PreservesExistingSmallThumbnail()
    {
        const string StoragePath = "projects/temp-project/part.stl";
        var service = CreateService();

        await service.SetThumbnailAsync(
            StoragePath,
            "https://signed.example/thumb-small.webp",
            StoragePath + "_thumb_256.webp");

        await service.SetPreviewUrlsAsync(
            StoragePath,
            new FileAnalysisPreviewUrlsDto
            {
                FrontSmall = "https://signed.example/front.webp",
                ThumbnailSmall = null,
                ThumbnailSmallGcsPath = null,
                ThumbnailLargeUrl = "https://signed.example/thumb-large.webp",
                ThumbnailLargeGcsPath = StoragePath + "_thumb_1200.webp"
            },
            thumbnailUrl: null,
            hiResThumbnailUrl: "https://signed.example/thumb-large.webp");

        var status = await service.GetStatusAsync(StoragePath);

        Assert.NotNull(status);
        Assert.Equal("https://signed.example/thumb-small.webp", status.ThumbnailUrl);
        Assert.Equal("https://signed.example/thumb-small.webp", status.PreviewUrls?.ThumbnailSmall);
        Assert.Equal(StoragePath + "_thumb_256.webp", status.PreviewUrls?.ThumbnailSmallGcsPath);
        Assert.Equal("https://signed.example/front.webp", status.PreviewUrls?.FrontSmall);
        Assert.Equal("https://signed.example/thumb-large.webp", status.PreviewUrls?.ThumbnailLargeUrl);
        Assert.Equal(StoragePath + "_thumb_1200.webp", status.PreviewUrls?.ThumbnailLargeGcsPath);
    }

    private static FileAnalysisStatusService CreateService()
    {
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        return new FileAnalysisStatusService(cache, NullLogger<FileAnalysisStatusService>.Instance);
    }
}
