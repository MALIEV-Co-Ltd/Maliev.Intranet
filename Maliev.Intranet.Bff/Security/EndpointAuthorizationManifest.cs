using System.Reflection;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Security;

/// <summary>Classifies how an endpoint establishes access to a specific resource.</summary>
public enum ResourceOwnershipKind
{
    /// <summary>The endpoint has not yet declared a resource rule.</summary>
    NotDeclared,
    /// <summary>The BFF validates access before completing the operation.</summary>
    BffValidated,
    /// <summary>An authoritative downstream service validates access.</summary>
    DownstreamValidated,
    /// <summary>A signed, resource-bound capability validates access.</summary>
    SignedCapability
}

/// <summary>Describes a resource-ownership assertion applied by an endpoint or hub method.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class ResourceOwnershipAttribute(
    ResourceOwnershipKind kind,
    string authority,
    string resourceParameter) : Attribute
{
    /// <summary>The ownership enforcement category.</summary>
    public ResourceOwnershipKind Kind { get; } = kind;
    /// <summary>The BFF component or downstream service that makes the decision.</summary>
    public string Authority { get; } = authority;
    /// <summary>The request parameter used only as a resource locator.</summary>
    public string ResourceParameter { get; } = resourceParameter;
}

/// <summary>Machine-readable resource authorization metadata.</summary>
public sealed record ResourceOwnershipManifest(
    ResourceOwnershipKind Kind,
    string? Authority,
    string? ResourceParameter);

/// <summary>Identifies a SignalR hub and its externally mapped route.</summary>
public sealed record HubManifestSurface(string Route, Type HubType);

/// <summary>One controller action, hub connection, or client-callable hub method.</summary>
public sealed record EndpointAuthorizationManifestEntry(
    string Kind,
    string Surface,
    string Member,
    IReadOnlyList<string> Methods,
    string Audience,
    string Authentication,
    string Authorization,
    IReadOnlyList<string> Permissions,
    ResourceOwnershipManifest Ownership,
    string RateLimit,
    string Coverage);

/// <summary>Generated authorization inventory for the Intranet BFF.</summary>
public sealed record EndpointAuthorizationManifest(
    string Application,
    string GeneratedAtUtc,
    IReadOnlyList<EndpointAuthorizationManifestEntry> Entries);

/// <summary>Generates the current authorization inventory from ASP.NET runtime metadata.</summary>
public static class EndpointAuthorizationManifestBuilder
{
    /// <summary>The SignalR hubs mapped by the Intranet BFF.</summary>
    public static IReadOnlyList<HubManifestSurface> IntranetHubs { get; } =
    [
        new("/hubs/notifications", typeof(NotificationHub)),
        new("/hubs/chat", typeof(ChatHub)),
        new("/hubs/production", typeof(ProductionHub))
    ];

    /// <summary>Builds the manifest from controller and hub reflection metadata.</summary>
    public static EndpointAuthorizationManifest Build(
        EndpointDataSource endpointDataSource,
        IReadOnlyList<HubManifestSurface> hubs)
    {
        var endpoints = endpointDataSource.Endpoints.OfType<RouteEndpoint>().ToArray();
        var entries = BuildControllerEntries(endpoints)
            .Concat(BuildHubEntries(endpoints, hubs))
            .OrderBy(entry => entry.Surface, StringComparer.Ordinal)
            .ThenBy(entry => entry.Member, StringComparer.Ordinal)
            .ToArray();

        return new EndpointAuthorizationManifest(
            "Maliev.Intranet.Bff",
            DateTimeOffset.UtcNow.ToString("O"),
            entries);
    }

    private static IEnumerable<EndpointAuthorizationManifestEntry> BuildControllerEntries(
        IReadOnlyList<RouteEndpoint> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (action is null)
            {
                continue;
            }

            yield return CreateEntry(
                "controller",
                "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/'),
                action.MethodInfo.Name,
                endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"],
                endpoint.Metadata,
                action.ControllerTypeInfo.AsType(),
                action.MethodInfo);
        }
    }

    private static IEnumerable<EndpointAuthorizationManifestEntry> BuildHubEntries(
        IReadOnlyList<RouteEndpoint> endpoints,
        IReadOnlyList<HubManifestSurface> hubs)
    {
        foreach (var hub in hubs)
        {
            var connectEndpoint = endpoints.FirstOrDefault(endpoint =>
                string.Equals("/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/'), hub.Route, StringComparison.Ordinal));
            var connectMetadata = connectEndpoint?.Metadata ?? new EndpointMetadataCollection();
            yield return CreateEntry("hub", hub.Route, "<connect>", ["CONNECT"], connectMetadata, hub.HubType, null);

            foreach (var method in hub.HubType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var exposedName = method.GetCustomAttribute<HubMethodNameAttribute>(true)?.Name ?? method.Name;
                yield return CreateEntry("hub-method", hub.Route, exposedName, ["INVOKE"], connectMetadata, hub.HubType, method);
            }
        }
    }

    private static EndpointAuthorizationManifestEntry CreateEntry(
        string kind,
        string surface,
        string member,
        IReadOnlyList<string> methods,
        EndpointMetadataCollection endpointMetadata,
        Type declaringType,
        MethodInfo? method)
    {
        var allowAnonymous = endpointMetadata.GetMetadata<IAllowAnonymous>()
            ?? method?.GetCustomAttribute<AllowAnonymousAttribute>(true)
            ?? declaringType.GetCustomAttribute<AllowAnonymousAttribute>(true);
        var permissions = endpointMetadata.GetOrderedMetadata<RequirePermissionAttribute>()
            .Concat(method?.GetCustomAttributes<RequirePermissionAttribute>(true) ?? [])
            .Concat(declaringType.GetCustomAttributes<RequirePermissionAttribute>(true))
            .Select(attribute => attribute.Permission)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var authorizeData = endpointMetadata.GetOrderedMetadata<IAuthorizeData>()
            .Concat(method?.GetCustomAttributes<AuthorizeAttribute>(true) ?? [])
            .Concat(declaringType.GetCustomAttributes<AuthorizeAttribute>(true))
            .Distinct()
            .ToArray();
        var ownership = method?.GetCustomAttribute<ResourceOwnershipAttribute>(true)
            ?? declaringType.GetCustomAttribute<ResourceOwnershipAttribute>(true);

        var audience = allowAnonymous is not null
            ? "anonymous"
            : permissions.Length > 0 ? "employee-permission" : "employee-authenticated";
        var authentication = allowAnonymous is not null
            ? "none"
            : string.Join(',', authorizeData
                .Select(data => data.AuthenticationSchemes)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .SelectMany(value => value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)) is { Length: > 0 } schemes ? schemes : "default";
        var authorization = allowAnonymous is not null
            ? "AllowAnonymous"
            : permissions.Length > 0
                ? "Permissions:" + string.Join(',', permissions)
                : authorizeData.Length > 0
                    ? "AuthenticatedPolicy:" + string.Join(',', authorizeData.Select(data => data.Policy ?? "default").Distinct())
                    : "UNCLASSIFIED";
        var ownershipManifest = ownership is null
            ? new ResourceOwnershipManifest(ResourceOwnershipKind.NotDeclared, null, null)
            : new ResourceOwnershipManifest(ownership.Kind, ownership.Authority, ownership.ResourceParameter);

        return new EndpointAuthorizationManifestEntry(
            kind,
            surface,
            member,
            methods,
            audience,
            authentication,
            authorization,
            permissions,
            ownershipManifest,
            ResolveRateLimit(endpointMetadata),
            "uncovered");
    }

    private static string ResolveRateLimit(EndpointMetadataCollection metadata)
    {
        var rateLimit = metadata.FirstOrDefault(attribute =>
            attribute.GetType().Name.Contains("RateLimit", StringComparison.Ordinal));
        if (rateLimit is null)
        {
            return "none";
        }

        var policyName = rateLimit.GetType().GetProperty("PolicyName")?.GetValue(rateLimit)?.ToString();
        return string.IsNullOrWhiteSpace(policyName) ? rateLimit.GetType().Name : policyName;
    }

}
