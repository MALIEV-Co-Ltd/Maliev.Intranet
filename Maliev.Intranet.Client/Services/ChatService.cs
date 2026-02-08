using Maliev.Intranet.Shared;
using System.Net.Http.Json;

namespace Maliev.Intranet.Client.Services;

public class ChatMessage
{
    public string Text { get; set; } = "";
    public bool IsUser { get; set; }
    public string? Context { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<BffSuggestedAction>? SuggestedActions { get; set; }
}

public class ChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<ChatMessage> Messages { get; } = new();
    public Guid? SessionId { get; private set; }
    public bool IsLoading { get; private set; }

    private string _currentContext = "/";
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

    public event Action? OnChange;

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
    /// Initializes a chat session with the BFF.
    /// </summary>
    public async Task InitializeSessionAsync()
    {
        if (SessionId.HasValue) return;

        try
        {
            var request = new BffChatSessionRequest { Channel = "intranet", Language = "en" };
            var response = await _httpClient.PostAsJsonAsync("api/chat/session", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BffChatSessionResponse>();
                if (result != null)
                {
                    SessionId = result.SessionId;
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
    /// </summary>
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

            var response = await _httpClient.PostAsJsonAsync("api/chat/message", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BffChatMessageResponse>();
            }

            return null;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
