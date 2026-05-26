using System.Reflection;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class CommerceCollectionsTests
{
    [Fact]
    public void OnCollectionTitleChanged_NewCollection_GeneratesUrlSlug()
    {
        var page = new global::Maliev.Intranet.Client.Pages.Commerce.Collections();
        SetPrivateField(page, "_collectionForm", new CommerceCollectionMutationRequest());
        SetPrivateField(page, "_handleManuallyEdited", false);

        InvokePrivateVoidWithArgs(page, "OnCollectionTitleChanged", "Injection Molding Machines");

        var form = GetPrivateField<CommerceCollectionMutationRequest>(page, "_collectionForm");
        Assert.Equal("Injection Molding Machines", form.Title);
        Assert.Equal("injection-molding-machines", form.Handle);
    }

    [Fact]
    public void OnCollectionHandleChanged_NormalizesManualSlugAndStopsTitleOverwrite()
    {
        var page = new global::Maliev.Intranet.Client.Pages.Commerce.Collections();
        SetPrivateField(page, "_collectionForm", new CommerceCollectionMutationRequest());

        InvokePrivateVoidWithArgs(page, "OnCollectionHandleChanged", "  Custom Machines!!  ");
        InvokePrivateVoidWithArgs(page, "OnCollectionTitleChanged", "Different Title");

        var form = GetPrivateField<CommerceCollectionMutationRequest>(page, "_collectionForm");
        Assert.Equal("Different Title", form.Title);
        Assert.Equal("custom-machines", form.Handle);
    }

    private static void InvokePrivateVoidWithArgs(
        global::Maliev.Intranet.Client.Pages.Commerce.Collections page,
        string methodName,
        params object[] args)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.Commerce.Collections)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(page, args);
    }

    private static void SetPrivateField<T>(
        global::Maliev.Intranet.Client.Pages.Commerce.Collections page,
        string fieldName,
        T value)
    {
        var field = typeof(global::Maliev.Intranet.Client.Pages.Commerce.Collections)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(page, value);
    }

    private static T GetPrivateField<T>(
        global::Maliev.Intranet.Client.Pages.Commerce.Collections page,
        string fieldName)
    {
        var field = typeof(global::Maliev.Intranet.Client.Pages.Commerce.Collections)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(page));
    }
}
