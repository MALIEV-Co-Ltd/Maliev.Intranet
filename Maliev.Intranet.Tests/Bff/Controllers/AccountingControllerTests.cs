using System.Reflection;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public sealed class AccountingControllerTests
{
    [Theory]
    [InlineData(nameof(AccountingController.GetAccountsTree), MalievPermissions.Accounting.Read)]
    [InlineData(nameof(AccountingController.GetJournalEntries), MalievPermissions.Accounting.Journal.Read)]
    [InlineData(nameof(AccountingController.CreateJournalEntry), MalievPermissions.Accounting.Journal.Create)]
    [InlineData(nameof(AccountingController.PostJournalEntry), MalievPermissions.Accounting.Journal.Post)]
    [InlineData(nameof(AccountingController.OpenPeriod), MalievPermissions.Accounting.PeriodsOpen)]
    [InlineData(nameof(AccountingController.ClosePeriod), MalievPermissions.Accounting.PeriodsClose)]
    [InlineData(nameof(AccountingController.ReopenPeriod), MalievPermissions.Accounting.PeriodsReopen)]
    [InlineData(nameof(AccountingController.RunReconciliation), MalievPermissions.Accounting.ReconciliationRun)]
    [InlineData(nameof(AccountingController.UploadDocument), MalievPermissions.Accounting.Journal.Create)]
    public void AccountingEndpoints_RequireActualAccountingServicePermissions(string actionName, string expectedPermission)
    {
        var method = typeof(AccountingController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }
}
