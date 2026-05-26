using Maliev.Intranet.Bff.Security;

namespace Maliev.Intranet.Tests.Bff.Security;

public class WorkspaceEmailDomainPolicyTests
{
    [Theory]
    [InlineData("employee@maliev.com")]
    [InlineData("Employee@MALIEV.COM")]
    [InlineData("  employee@maliev.com  ")]
    public void IsAllowedEmployeeEmail_MalievWorkspaceAddress_ReturnsTrue(string email)
    {
        Assert.True(WorkspaceEmailDomainPolicy.IsAllowedEmployeeEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("employee")]
    [InlineData("employee@example.com")]
    [InlineData("employee@sub.maliev.com")]
    [InlineData("employee@maliev.com.example")]
    [InlineData("employee @maliev.com")]
    [InlineData("employee@maliev.com extra")]
    public void IsAllowedEmployeeEmail_NonWorkspaceAddress_ReturnsFalse(string email)
    {
        Assert.False(WorkspaceEmailDomainPolicy.IsAllowedEmployeeEmail(email));
    }
}
