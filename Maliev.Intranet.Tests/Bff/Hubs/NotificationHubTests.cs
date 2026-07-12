using Maliev.Intranet.Bff.Hubs;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class NotificationHubTests
{
    [Fact]
    public void NotificationHub_DoesNotExposeClientCallableBroadcastMethods()
    {
        var browserMethods = typeof(NotificationHub)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.DeclaredOnly)
            .Select(method => method.Name);

        Assert.DoesNotContain("SendNotification", browserMethods);
        Assert.DoesNotContain("NotifyCustomerChanged", browserMethods);
    }
}
