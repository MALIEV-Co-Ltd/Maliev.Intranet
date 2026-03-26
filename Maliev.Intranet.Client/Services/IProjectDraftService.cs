using Microsoft.JSInterop;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Service for persisting and retrieving the Project New draft state to and from browser localStorage.
/// Drafts persist across tabs and browser restarts, keyed by session ID for multi-session support.
/// </summary>
public interface IProjectDraftService
{
    /// <summary>
    /// Saves the given draft state to localStorage under a key based on the session ID.
    /// </summary>
    /// <param name="state">The draft project state to persist.</param>
    /// <param name="sessionId">The session identifier to use as the storage key.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveDraftAsync(DraftProjectState state, string? sessionId);

    /// <summary>
    /// Loads the persisted draft state from localStorage for the given session ID.
    /// Returns null when no draft is found or the stored JSON is invalid.
    /// </summary>
    /// <param name="sessionId">The session identifier to load the draft for.</param>
    /// <returns>The persisted draft state, or null if none exists.</returns>
    Task<DraftProjectState?> LoadDraftAsync(string? sessionId);

    /// <summary>
    /// Removes the persisted draft from localStorage for the given session ID.
    /// Should be called after a project is successfully created.
    /// </summary>
    /// <param name="sessionId">The session identifier to clear.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearDraftAsync(string? sessionId);
}
