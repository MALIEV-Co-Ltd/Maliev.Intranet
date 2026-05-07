using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Maliev.Intranet.Client.Components.Shared;

/// <summary>
/// Text input component that updates the bound edit model on every input event.
/// </summary>
public sealed class ImmediateInputText : InputBase<string?>
{
    /// <summary>
    /// Gets or sets the HTML input type.
    /// </summary>
    [Parameter]
    public string Type { get; set; } = "text";

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", Type);
        builder.AddAttribute(3, "class", CssClass);
        builder.AddAttribute(4, "value", CurrentValueAsString);
        builder.AddAttribute(5, "oninput", EventCallback.Factory.CreateBinder<string?>(
            this,
            value => CurrentValueAsString = value,
            CurrentValueAsString));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;
        return true;
    }
}
