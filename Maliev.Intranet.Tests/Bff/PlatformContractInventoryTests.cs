using System.Text.RegularExpressions;
using RegexMatch = System.Text.RegularExpressions.Match;

namespace Maliev.Intranet.Tests.Bff;

public sealed class PlatformContractInventoryTests
{
    private static readonly Regex ClientCallRegex = new(
        @"(?<call>GetFromJsonAsync|GetAsync|PostAsJsonAsync|PostAsync|PutAsJsonAsync|PutAsync|PatchAsJsonAsync|PatchAsync|DeleteAsync)\s*(?:<[^>]+>)?\s*\(\s*\$?""(?<route>/[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex ClassRouteRegex = new(
        @"\[Route\(""(?<route>[^""]+)""\)\]",
        RegexOptions.Compiled);

    private static readonly Regex HttpRouteRegex = new(
        @"\[Http(?<verb>Get|Post|Put|Patch|Delete)(?:\((?<args>[^\)]*)\))?\]",
        RegexOptions.Compiled);

    private static readonly Regex QuotedRouteRegex = new(
        @"""(?<route>[^""]*)""",
        RegexOptions.Compiled);

    private static readonly Regex PlaceholderRegex = new(
        @"\{[^}/]+\}",
        RegexOptions.Compiled);

    [Fact]
    public void BffClientLiteralRoutes_TargetDownstreamControllerInventory()
    {
        var workspaceRoot = TryFindWorkspaceRoot();
        if (workspaceRoot is null)
        {
            return;
        }

        var clientsRoot = Path.Combine(workspaceRoot, "Maliev.Intranet", "Maliev.Intranet.Bff", "Clients");
        var checkedServices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CareerServiceClient.cs"] = "Maliev.CareerService",
            ["ComplianceServiceClient.cs"] = "Maliev.ComplianceService",
            ["CompensationServiceClient.cs"] = "Maliev.CompensationService",
            ["EmployeeServiceClient.cs"] = "Maliev.EmployeeService",
            ["JobServiceClient.cs"] = "Maliev.JobService",
            ["LeaveServiceClient.cs"] = "Maliev.LeaveService",
            ["LifecycleServiceClient.cs"] = "Maliev.LifecycleService",
            ["OrderServiceClient.cs"] = "Maliev.OrderService",
            ["PaymentServiceClient.cs"] = "Maliev.PaymentService",
            ["PricingServiceClient.cs"] = "Maliev.PricingService",
            ["SupplierServiceClient.cs"] = "Maliev.SupplierService",
        };

        var failures = new List<string>();
        foreach (var (clientFile, serviceDirectory) in checkedServices)
        {
            var clientPath = Path.Combine(clientsRoot, clientFile);
            var downstreamRoutes = LoadDownstreamRoutes(Path.Combine(workspaceRoot, serviceDirectory));

            foreach (var route in LoadClientRoutes(clientPath))
            {
                if (!downstreamRoutes.Contains(route))
                {
                    failures.Add($"{clientFile}: {route.Verb} {route.Path}");
                }
            }
        }

        Assert.True(failures.Count == 0, "BFF client routes missing from downstream controller inventory:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void ProductionMassTransitConsumers_UseCentralMessagingContracts()
    {
        var workspaceRoot = TryFindWorkspaceRoot();
        if (workspaceRoot is null)
        {
            return;
        }

        var serviceRoots = Directory.EnumerateDirectories(workspaceRoot, "Maliev.*Service");
        var failures = new List<string>();
        var consumerRegex = new Regex(@"IConsumer\s*<\s*(?<message>[^>]+)\s*>", RegexOptions.Compiled);

        foreach (var serviceRoot in serviceRoots)
        {
            foreach (var file in Directory.EnumerateFiles(serviceRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsIgnoredCodePath(file) || file.Contains(".Tests" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                if (!consumerRegex.IsMatch(source) || source.Contains("Maliev.MessagingContracts", StringComparison.Ordinal))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(workspaceRoot, file);
                foreach (RegexMatch match in consumerRegex.Matches(source))
                {
                    failures.Add($"{relativePath}: {match.Groups["message"].Value.Trim()}");
                }
            }
        }

        Assert.True(failures.Count == 0, "Production consumers must import centralized Maliev.MessagingContracts types:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private static IEnumerable<RouteSignature> LoadClientRoutes(string clientPath)
    {
        var source = File.ReadAllText(clientPath);
        foreach (RegexMatch match in ClientCallRegex.Matches(source))
        {
            var rawPath = match.Groups["route"].Value.Split('?', 2)[0];
            if (rawPath.Contains("{query}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new RouteSignature(MapClientVerb(match.Groups["call"].Value), NormalizeRoute(rawPath));
        }
    }

    private static HashSet<RouteSignature> LoadDownstreamRoutes(string serviceRoot)
    {
        var routes = new HashSet<RouteSignature>();
        var controllersRoot = Path.Combine(serviceRoot, Path.GetFileName(serviceRoot) + ".Api", "Controllers");
        foreach (var file in Directory.EnumerateFiles(controllersRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var classRouteMatch = ClassRouteRegex.Match(source);
            if (!classRouteMatch.Success)
            {
                continue;
            }

            var controllerName = Path.GetFileNameWithoutExtension(file).Replace("Controller", string.Empty, StringComparison.OrdinalIgnoreCase);
            var classRoute = ReplaceControllerToken(classRouteMatch.Groups["route"].Value, controllerName);

            foreach (RegexMatch httpMatch in HttpRouteRegex.Matches(source))
            {
                var methodRoute = ExtractHttpRoute(httpMatch.Groups["args"].Value);
                var route = CombineRoutes(classRoute, methodRoute);
                routes.Add(new RouteSignature(httpMatch.Groups["verb"].Value.ToUpperInvariant(), NormalizeRoute(route)));
            }
        }

        return routes;
    }

    private static string ExtractHttpRoute(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return string.Empty;
        }

        var quoted = QuotedRouteRegex.Match(args);
        return quoted.Success ? quoted.Groups["route"].Value : string.Empty;
    }

    private static string CombineRoutes(string classRoute, string methodRoute)
    {
        if (methodRoute.StartsWith("~/", StringComparison.Ordinal))
        {
            return methodRoute[2..];
        }

        if (methodRoute.StartsWith("/", StringComparison.Ordinal))
        {
            return methodRoute.TrimStart('/');
        }

        if (string.IsNullOrWhiteSpace(methodRoute))
        {
            return classRoute;
        }

        return $"{classRoute.TrimEnd('/')}/{methodRoute.TrimStart('/')}";
    }

    private static string NormalizeRoute(string route)
    {
        var normalized = route.Replace("v{version:apiVersion}", "v1", StringComparison.OrdinalIgnoreCase)
            .Replace("{version:apiVersion}", "1", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .TrimStart('/')
            .ToLowerInvariant();

        normalized = PlaceholderRegex.Replace(normalized, "{}");
        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        return "/" + normalized.TrimEnd('/');
    }

    private static string ReplaceControllerToken(string route, string controllerName)
    {
        return route.Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase);
    }

    private static string MapClientVerb(string call)
    {
        return call.StartsWith("Get", StringComparison.Ordinal) ? "GET" :
            call.StartsWith("Post", StringComparison.Ordinal) ? "POST" :
            call.StartsWith("Put", StringComparison.Ordinal) ? "PUT" :
            call.StartsWith("Patch", StringComparison.Ordinal) ? "PATCH" :
            call.StartsWith("Delete", StringComparison.Ordinal) ? "DELETE" :
            throw new InvalidOperationException($"Unsupported client call '{call}'.");
    }

    private static string? TryFindWorkspaceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Maliev.Intranet")) &&
                Directory.Exists(Path.Combine(current.FullName, "Maliev.MessagingContracts")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool IsIgnoredCodePath(string file)
    {
        var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Contains("bin", StringComparer.OrdinalIgnoreCase) ||
            parts.Contains("obj", StringComparer.OrdinalIgnoreCase) ||
            parts.Contains(".git", StringComparer.OrdinalIgnoreCase);
    }

    private readonly record struct RouteSignature(string Verb, string Path);
}
