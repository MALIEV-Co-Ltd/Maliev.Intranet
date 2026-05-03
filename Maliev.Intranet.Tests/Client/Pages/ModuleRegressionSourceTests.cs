namespace Maliev.Intranet.Tests.Client.Pages;

public class ModuleRegressionSourceTests
{
    [Fact]
    public void CustomerList_SubscribesToCustomerChangedRealtimeSignal()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerList.razor");

        Assert.Contains("ISignalRCustomerService", source);
        Assert.Contains("OnCustomerChanged", source);
        Assert.Contains("StartAsync", source);
        Assert.Contains("LoadAsync", source);
    }

    [Fact]
    public void AdminPage_DoesNotExposeIntegrationsTab()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "AdminPage.razor");

        Assert.DoesNotContain("Integrations", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClientLoginRoute_IsNotUsedForEmployeeLogin()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Login.razor");

        Assert.DoesNotContain("@page \"/login\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_DoesNotContainDesignIterationDisplayTweaks()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");

        Assert.DoesNotContain("Display tweaks", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Accent color", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SetAccentHue", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_UsesLogoForDashboardAndProfileMenuHasSignOut()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");

        Assert.DoesNotContain("new(\"Dashboard\"", source, StringComparison.Ordinal);
        Assert.Contains("Navigation.NavigateTo(\"/\")", source, StringComparison.Ordinal);
        Assert.Contains("My Profile", source, StringComparison.Ordinal);
        Assert.Contains("Preferences", source, StringComparison.Ordinal);
        Assert.Contains("Sign out", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectsPage_UsesSharedShellAndQueryBackedPagination()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Projects.razor");

        Assert.Contains("ModuleHeader Title=\"Projects\"", source, StringComparison.Ordinal);
        Assert.Contains("[SupplyParameterFromQuery(Name = \"status\")]", source, StringComparison.Ordinal);
        Assert.Contains("PaginationFooter", source, StringComparison.Ordinal);
        Assert.Contains("pageSize={_pageSize}", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerList_UsesServerPaginationInsteadOfFixedFirstPage()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerList.razor");

        Assert.Contains("PaginationFooter", source, StringComparison.Ordinal);
        Assert.Contains("pageSize={_pageSize}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("page=1&pageSize=50", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FilteredCustomers", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchasingPages_UseIntIdsAndServerPagination()
    {
        var list = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Purchasing", "PoList.razor");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Purchasing", "PoDetail.razor");

        Assert.Contains("PaginationFooter", list, StringComparison.Ordinal);
        Assert.Contains("pageSize={_pageSize}", list, StringComparison.Ordinal);
        Assert.DoesNotContain("page=1&pageSize=50", list, StringComparison.Ordinal);
        Assert.Contains("@page \"/purchasing/{Id:int}\"", detail, StringComparison.Ordinal);
        Assert.Contains("/approve", detail, StringComparison.Ordinal);
        Assert.Contains("/send-to-supplier", detail, StringComparison.Ordinal);
        Assert.Contains("/receive", detail, StringComparison.Ordinal);
        Assert.Contains("/cancel", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void AccountingPage_ExposesFinanceWorkstreamsAndServerPagination()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Accounting", "InvoiceList.razor");

        Assert.Contains("Journal Entries", source, StringComparison.Ordinal);
        Assert.Contains("Income Entry", source, StringComparison.Ordinal);
        Assert.Contains("Expense Entry", source, StringComparison.Ordinal);
        Assert.Contains("Payroll Journals", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/invoices?page={_invoicePage}&pageSize={_pageSize}", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/accounting/journal-entries?page={_journalPage}&pageSize={_pageSize}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("page=1&pageSize=50", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_CurrencySelectorTrigger_RemainsCompact()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");

        Assert.Contains("flex: 0 0 86px;", source, StringComparison.Ordinal);
        Assert.Contains("width: 86px;", source, StringComparison.Ordinal);
        Assert.Contains("min-width: 220px;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("min-width: 120px;", source, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
