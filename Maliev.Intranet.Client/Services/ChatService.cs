using Maliev.Intranet.Client.Helpers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Represents a single message in a chat conversation, including metadata such as thinking steps and suggested actions.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets or sets a value indicating whether the message was sent by the user.
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// Gets or sets the context (e.g., page URL or resource ID) associated with this message.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the list of suggested actions provided by the chatbot for this message.
    /// </summary>
    public List<BffSuggestedAction>? SuggestedActions { get; set; }

    /// <summary>
    /// Gets or sets the collection of thinking steps taken by the AI to generate the response.
    /// </summary>
    public List<ThinkingStepDto> ThinkingSteps { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the message is currently being processed by the AI.
    /// </summary>
    public bool IsProcessing { get; set; }
}

/// <summary>
/// Manages the chat session and communication with the Backend-for-Frontend (BFF) and SignalR hub.
/// </summary>
public class ChatService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private readonly CookieProvider _cookieProvider;
    private HubConnection? _hubConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class with required dependencies.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API requests.</param>
    /// <param name="navigationManager">The navigation manager for resolving hub URLs.</param>
    /// <param name="cookieProvider">Provides cookies captured during SSR for server-side hub auth.</param>
    public ChatService(HttpClient httpClient, NavigationManager navigationManager, CookieProvider cookieProvider)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
        _cookieProvider = cookieProvider;
    }

    /// <summary>
    /// Gets the list of messages in the current chat session.
    /// </summary>
    public List<ChatMessage> Messages { get; } = new();

    /// <summary>
    /// Gets the conversation summaries available to the current employee.
    /// </summary>
    public List<BffChatConversationSummary> Conversations { get; } = new();

    /// <summary>
    /// Gets the unique identifier of the active chat session.
    /// </summary>
    public Guid? SessionId { get; private set; }

    /// <summary>
    /// Gets the active conversation summary, if the active session is stored in history.
    /// </summary>
    public BffChatConversationSummary? ActiveConversation =>
        SessionId.HasValue ? Conversations.FirstOrDefault(c => c.SessionId == SessionId.Value) : null;

    /// <summary>
    /// Gets a value indicating whether a message or session initialization is currently loading.
    /// </summary>
    public bool IsLoading { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the SignalR hub connection is currently active.
    /// </summary>
    public bool IsSignalRConnected => _hubConnection?.State == HubConnectionState.Connected;

    private string _currentContext = "/";

    /// <summary>
    /// Gets or sets the current navigation context used for new chat messages.
    /// </summary>
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

    /// <summary>
    /// Raised when the service state changes, notifying UI components to re-render.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Adds a message to the local message list and notifies listeners of the state change.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    /// <param name="isUser">True if the message is from the user; false if from the AI.</param>
    /// <param name="suggestedActions">Optional list of suggested actions to display with the message.</param>
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
    /// Clears the active conversation and lets the next sent message create a fresh backend session.
    /// </summary>
    public void StartNewConversation()
    {
        Messages.Clear();
        SessionId = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Loads conversation summaries for the authenticated employee.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadConversationsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/chat/conversations?channel=intranet&page={page}&pageSize={pageSize}");
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<BffChatConversationListResponse>();
            Conversations.Clear();
            if (result?.Data is not null)
            {
                Conversations.AddRange(result.Data);
            }

            NotifyStateChanged();
        }
        catch
        {
            // Conversation history is additive; chat can still work without it.
        }
    }

    /// <summary>
    /// Loads and activates a previous conversation.
    /// </summary>
    /// <param name="sessionId">The conversation session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SelectConversationAsync(Guid sessionId)
    {
        IsLoading = true;
        NotifyStateChanged();

        try
        {
            var response = await _httpClient.GetAsync($"api/v1/chat/conversations/{sessionId}/messages");
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<BffChatConversationMessagesResponse>();
            if (result is null)
            {
                return;
            }

            SessionId = result.SessionId;
            Messages.Clear();
            Messages.AddRange(result.Messages.Select(message => new ChatMessage
            {
                Text = message.Content,
                IsUser = message.Role.Equals("user", StringComparison.OrdinalIgnoreCase),
                Context = _currentContext,
                Timestamp = message.CreatedAt.LocalDateTime
            }));

            await JoinSessionGroupAsync();
        }
        catch
        {
            // Leave the active conversation untouched if history loading fails.
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Connects to the ChatHub for real-time thinking step updates.
    /// </summary>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    public async Task ConnectSignalRAsync()
    {
        if (_hubConnection != null || _navigationManager == null) return;

        var hubUrl = _navigationManager.BaseUri.TrimEnd('/') + "/hubs/chat";

        _hubConnection = new HubConnectionBuilder()
            .WithUrlAndCookies(hubUrl, _cookieProvider.CookieHeader)
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
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task InitializeSessionAsync()
    {
        if (SessionId.HasValue) return;

        try
        {
            var language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var request = new BffChatSessionRequest { Channel = "intranet", Language = language };
            var response = await _httpClient.PostAsJsonAsync("api/v1/chat/session", request);

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
    /// <param name="text">The text content of the message to send.</param>
    /// <returns>The response from the chatbot, or null if the message could not be sent.</returns>
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
                ? "api/v1/chat/message/stream"
                : "api/v1/chat/message";

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

                    await LoadConversationsAsync();
                    return result;
                }

                await LoadConversationsAsync();
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
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}
