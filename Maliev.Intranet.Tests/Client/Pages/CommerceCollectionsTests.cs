using System.Reflection;
using System.Runtime.CompilerServices;
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
    public void CollectionsMarkup_EditsSlugInStorefrontUrlField()
    {
        var collections = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "Collections.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "Collections.razor.css");

        Assert.DoesNotContain("Label=\"Storefront URL slug\"", collections, StringComparison.Ordinal);
        Assert.Contains("commerce-collection-url-field", collections, StringComparison.Ordinal);
        Assert.Contains("commerce-collection-url-prefix", collections, StringComparison.Ordinal);
        Assert.Contains("/shop?collection=", collections, StringComparison.Ordinal);
        Assert.Contains("OnCollectionStorefrontSlugChanged", collections, StringComparison.Ordinal);
        Assert.Contains("commerce-collection-url-input", collections, StringComparison.Ordinal);
        Assert.Contains("commerce-collection-url-field", styles, StringComparison.Ordinal);
        Assert.Contains("commerce-collection-url-prefix", styles, StringComparison.Ordinal);
        Assert.Contains("commerce-collection-url-input", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void OnCollectionStorefrontSlugChanged_NormalizesManualSlugAndStopsTitleOverwrite()
    {
        var page = new global::Maliev.Intranet.Client.Pages.Commerce.Collections();
        SetPrivateField(page, "_collectionForm", new CommerceCollectionMutationRequest());

        InvokePrivateVoidWithArgs(page, "OnCollectionStorefrontSlugChanged", "  /shop?collection=Custom Machines!!  ");
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

    private static string ReadRepoFile(params string[] path)
    {
        var startDirectories = new[]
        {
            GetSourceDirectory(),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var startDirectory in startDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(path).ToArray());
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(path)}.");
    }

    private static string GetSourceDirectory([CallerFilePath] string sourceFile = "")
        => Path.GetDirectoryName(sourceFile) ?? Directory.GetCurrentDirectory();
}
