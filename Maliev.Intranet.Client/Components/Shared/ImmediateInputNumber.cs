using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Maliev.Intranet.Client.Components.Shared;

/// <summary>
/// Numeric input component that updates the bound edit model on every input event.
/// </summary>
/// <typeparam name="TValue">The numeric value type.</typeparam>
public sealed class ImmediateInputNumber<TValue> : InputNumber<TValue>
{
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "number");
        builder.AddAttribute(3, "class", CssClass);
        builder.AddAttribute(4, "value", CurrentValueAsString);
        builder.AddAttribute(5, "oninput", EventCallback.Factory.CreateBinder<string?>(
            this,
            value => CurrentValueAsString = value,
            CurrentValueAsString));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    }
}
