using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Represents a message in the chat interface.
/// </summary>
public class ChatMessage
{
    /// <summary>Gets or sets the message text.</summary>
    public string Text { get; set; } = "";
    /// <summary>Gets or sets a value indicating whether the message was sent by the user.</summary>
    public bool IsUser { get; set; }
    /// <summary>Gets or sets the contextual path where the message was sent.</summary>
    public string? Context { get; set; }
    /// <summary>Gets or sets the message timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    /// <summary>Gets or sets suggested follow-up actions.</summary>
    public List<BffSuggestedAction>? SuggestedActions { get; set; }
    /// <summary>Gets or sets the collection of thinking steps for this message.</summary>
    public List<ThinkingStepDto> ThinkingSteps { get; set; } = new();
    /// <summary>Gets or sets a value indicating whether the message is still being processed.</summary>
    public bool IsProcessing { get; set; }
}

/// <summary>
/// Service for managing chat interactions and real-time updates.
/// </summary>
public class ChatService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private HubConnection? _hubConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="navigationManager">The navigation manager.</param>
    public ChatService(HttpClient httpClient, NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
    }

    /// <summary>Gets the list of messages in the current session.</summary>
    public List<ChatMessage> Messages { get; } = new();
    /// <summary>Gets the active session identifier.</summary>
    public Guid? SessionId { get; private set; }
    /// <summary>Gets a value indicating whether a request is currently in progress.</summary>
    public bool IsLoading { get; private set; }
    /// <summary>Gets a value indicating whether the SignalR connection is active.</summary>
    public bool IsSignalRConnected => _hubConnection?.State == HubConnectionState.Connected;

    private string _currentContext = "/";
    /// <summary>Gets or sets the current UI context path.</summary>
    public string CurrentContext
    {
        get => _currentContext;
        set
        {
            if (_currentContext != value)
            {
                _currentContext = value;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>Event raised when the service state has changed.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Adds a message to the chat history.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="isUser">True if sent by user, false if by assistant.</param>
    /// <param name="suggestedActions">Optional suggested actions.</param>
    public void AddMessage(string text, bool isUser, List<BffSuggestedAction>? suggestedActions = null)
    {
        Messages.Add(new ChatMessage
        {
            Text = text,
            IsUser = isUser,
            Context = _currentContext,
            SuggestedActions = suggestedActions
        });
        NotifyStateChanged();
    }

    /// <summary>
    /// Connects to the ChatHub for real-time thinking step updates.
    /// </summary>
    public async Task ConnectSignalRAsync()
    {
        if (!OperatingSystem.IsBrowser() || _hubConnection != null) return;

        var hubUrl = _navigationManager.BaseUri.TrimEnd('/') + "/hubs/chat";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ThinkingStepDto>("ReceiveThinkingStep", step =>
        {
            var processingMessage = Messages.LastOrDefault(m => m.IsProcessing);
            if (processingMessage != null)
            {
                processingMessage.ThinkingSteps.Add(step);
                NotifyStateChanged();
            }
        });

        _hubConnection.On<BffChatMessageResponse>("ReceiveMessage", response =>
        {
            // The final response arrives here via SignalR — UI can update if needed
            NotifyStateChanged();
        });

        _hubConnection.On<string>("ReceiveError", error =>
        {
            var processingMessage = Messages.LastOrDefault(m => m.IsProcessing);
            if (processingMessage != null)
            {
                processingMessage.IsProcessing = false;
            }
            NotifyStateChanged();
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch
        {
            // SignalR connection failed — will fall back to HTTP-only
        }
    }

    /// <summary>
    /// Joins a session group for receiving targeted SignalR messages.
    /// </summary>
    private async Task JoinSessionGroupAsync()
    {
        if (_hubConnection?.State == HubConnectionState.Connected && SessionId.HasValue)
        {
            try
            {
                await _hubConnection.InvokeAsync("JoinSession", SessionId.Value.ToString());
            }
            catch { /* Best effort */ }
        }
    }

    /// <summary>
    /// Initializes a chat session with the BFF.
    /// </summary>
    public async Task InitializeSessionAsync()
    {
        if (SessionId.HasValue) return;

        try
        {
            var language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var request = new BffChatSessionRequest { Channel = "intranet", Language = language };
            var response = await _httpClient.PostAsJsonAsync("api/chat/session", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BffChatSessionResponse>();
                if (result != null)
                {
                    SessionId = result.SessionId;
                    await JoinSessionGroupAsync();
                }
            }
        }
        catch
        {
            // Session initialization failed - will retry on next message
        }
    }

    /// <summary>
    /// Sends a message through the BFF to the ChatbotService.
    /// Uses the streaming endpoint when SignalR is connected.
    /// </summary>
    /// <param name="text">The message text to send.</param>
    /// <returns>The response from the assistant.</returns>
    public async Task<BffChatMessageResponse?> SendMessageAsync(string text)
    {
        if (!SessionId.HasValue)
        {
            await InitializeSessionAsync();
        }

        if (!SessionId.HasValue) return null;

        IsLoading = true;
        NotifyStateChanged();

        try
        {
            var request = new BffChatMessageRequest
            {
                SessionId = SessionId.Value,
                Content = text,
                Context = _currentContext
            };

            // Use streaming endpoint when SignalR is connected
            var endpoint = IsSignalRConnected
                ? "api/chat/message/stream"
                : "api/chat/message";

            // Add a placeholder processing message for thinking steps
            if (IsSignalRConnected)
            {
                Messages.Add(new ChatMessage
                {
                    Text = "",
                    IsUser = false,
                    Context = _currentContext,
                    IsProcessing = true
                });
                NotifyStateChanged();
            }

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BffChatMessageResponse>();

                // Remove the processing placeholder if it exists
                var placeholder = Messages.LastOrDefault(m => m.IsProcessing);
                if (placeholder != null)
                {
                    // Keep the thinking steps but mark as done
                    placeholder.IsProcessing = false;
                    placeholder.Text = result?.Content ?? "";
                    placeholder.SuggestedActions = result?.SuggestedActions;

                    // Merge any thinking steps from the HTTP response
                    if (result?.ThinkingSteps?.Count > 0 && placeholder.ThinkingSteps.Count == 0)
                    {
                        placeholder.ThinkingSteps = result.ThinkingSteps;
                    }

                    return result;
                }

                return result;
            }

            // Remove placeholder on failure
            var failPlaceholder = Messages.LastOrDefault(m => m.IsProcessing);
            if (failPlaceholder != null) Messages.Remove(failPlaceholder);

            return null;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    /// <inheritdoc />
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}
