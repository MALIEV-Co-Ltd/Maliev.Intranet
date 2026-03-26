using Maliev.Intranet.Shared;
using Microsoft.JSInterop;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// localStorage-backed implementation of <see cref="IProjectDraftService"/>.
/// localStorage persists across tabs and browser restarts, enabling draft recovery
/// via URL session IDs. Drafts are keyed by session ID for multi-session support.
/// </summary>
public sealed class ProjectDraftService : IProjectDraftService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ProjectDraftService> _logger;
    private const string DraftStorageKeyBase = "maliev_project_draft_";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectDraftService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime for accessing browser localStorage.</param>
    /// <param name="logger">The logger instance.</param>
    public ProjectDraftService(IJSRuntime jsRuntime, ILogger<ProjectDraftService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    private string GetStorageKey(string? sessionId) =>
        string.IsNullOrEmpty(sessionId) ? "maliev_project_draft_default" : $"{DraftStorageKeyBase}{sessionId}";

    /// <inheritdoc />
    async Task IProjectDraftService.SaveDraftAsync(DraftProjectState state, string? sessionId)
    {
        try
        {
            state.LastModified = DateTime.UtcNow;
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            var key = GetStorageKey(sessionId);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save project draft to localStorage.");
        }
    }

    /// <inheritdoc />
    async Task<DraftProjectState?> IProjectDraftService.LoadDraftAsync(string? sessionId)
    {
        try
        {
            var key = GetStorageKey(sessionId);
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            if (string.IsNullOrEmpty(json))
                return null;

            return System.Text.Json.JsonSerializer.Deserialize<DraftProjectState>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load project draft from localStorage.");
            return null;
        }
    }

    /// <inheritdoc />
    async Task IProjectDraftService.ClearDraftAsync(string? sessionId)
    {
        try
        {
            var key = GetStorageKey(sessionId);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear project draft from storage.");
        }
    }
}
