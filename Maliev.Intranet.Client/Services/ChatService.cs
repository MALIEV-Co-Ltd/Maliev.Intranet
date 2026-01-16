using System;
using System.Collections.Generic;

namespace Maliev.Intranet.Client.Services;

public class ChatMessage
{
    public string Text { get; set; } = "";
    public bool IsUser { get; set; }
    public string? Context { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ChatService
{
    public List<ChatMessage> Messages { get; } = new();
    
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

    public void AddMessage(string text, bool isUser)
    {
        Messages.Add(new ChatMessage 
        { 
            Text = text, 
            IsUser = isUser,
            Context = _currentContext
        });
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
