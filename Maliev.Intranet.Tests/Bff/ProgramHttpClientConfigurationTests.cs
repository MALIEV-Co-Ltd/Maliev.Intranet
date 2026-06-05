namespace Maliev.Intranet.Tests.Bff;

public class ProgramHttpClientConfigurationTests
{
    [Theory]
    [InlineData("\"https+http://CountryService\"")]
    [InlineData("\"https+http://CustomerService\"")]
    [InlineData("\"https+http://IAMService\"")]
    [InlineData("\"https+http://UploadService\"")]
    public void ServiceAccountClients_PreferHttpsServiceDiscovery_ToPreserveAuthorizationHeader(string expectedBaseAddress)
    {
        var programSource = File.ReadAllText(FindProgramSource());

        Assert.Contains(expectedBaseAddress, programSource, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryNotesController_ClientInterface_IsRegistered()
    {
        var programSource = File.ReadAllText(FindProgramSource());

        Assert.Contains(
            "builder.AddBffServiceClient<IDeliveryServiceClient, DeliveryServiceClient>(\"DeliveryService\")",
            programSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Program_DoesNotSynchronouslyConnectRedisDuringStartup()
    {
        var programSource = File.ReadAllText(FindProgramSource());

        Assert.DoesNotContain(
            "ConnectionMultiplexer.Connect(redisConnectionString)",
            programSource,
            StringComparison.Ordinal);
        Assert.Contains("IPostConfigureOptions<KeyManagementOptions>", programSource, StringComparison.Ordinal);
        Assert.Contains("RedisXmlRepository", programSource, StringComparison.Ordinal);
    }

    private static string FindProgramSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "Maliev.Intranet.Bff", "Program.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Unable to locate Maliev.Intranet.Bff/Program.cs.");
    }
}
