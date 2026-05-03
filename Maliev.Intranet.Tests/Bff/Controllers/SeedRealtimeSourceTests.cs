namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SeedRealtimeSourceTests
{
    [Fact]
    public void SeedController_BroadcastsCustomerChangedAfterCustomerSeed()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "SeedController.cs");

        Assert.Contains("IHubContext<NotificationHub>", source);
        Assert.Contains("CustomerChanged", source);
        Assert.Contains("SendAsync", source);
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
