using Microsoft.AspNetCore.Components.Routing;
using Maliev.Intranet.Shared;
using MudBlazor;

namespace Maliev.Intranet.Client.Layout;

internal static class AppNavigation
{
    public static AppNavItem Quote { get; } = new(
        "Quote",
        "sales/projects/new",
        Icons.Material.Outlined.ReceiptLong,
        Permission: MalievPermissions.Project.Write,
        Match: NavLinkMatch.Prefix,
        Class: "topbar-nav-quote",
        Description: "Upload CAD and build a quote");

    public static IReadOnlyList<AppNavGroup> PrimaryGroups { get; } =
    [
        new("Sales", Icons.Material.Outlined.Business, "Sales & CRM",
        [
            new("Projects", "sales/projects", Icons.Material.Outlined.FolderSpecial, MalievPermissions.Project.Read, Description: "Quotes and project history"),
            new("Review queue", "sales/projects?status=CustomerReview", Icons.Material.Outlined.RateReview, MalievPermissions.Project.Read, Description: "Make Studio projects waiting for employee review"),
            new("Orders", "sales/orders", Icons.Material.Outlined.ReceiptLong, MalievPermissions.Order.Read, Description: "Order queue and payment state"),
            new("Customers", "customers", Icons.Material.Outlined.People, MalievPermissions.Customer.Read, Description: "Profiles, addresses, documents"),
            new("New customer", "customers/new", Icons.Material.Outlined.PersonAdd, MalievPermissions.Customer.Write, Description: "Onboard a customer"),
            new("Contact requests", "sales/contact-requests", Icons.Material.Outlined.ContactMail, MalievPermissions.ContactRequest.Read, Description: "Website contact queue"),
        ]),
        new("Commerce", Icons.Material.Outlined.Storefront, null,
        [
            new("Storefront catalog", "commerce/catalog", Icons.Material.Outlined.Storefront, MalievPermissions.Commerce.ProductsRead, Description: "Products, variants, media"),
            new("Product collections", "commerce/collections", Icons.Material.Outlined.Category, MalievPermissions.Commerce.CollectionsRead, Description: "Storefront collection groups"),
        ]),
        new("Web", Icons.Material.Outlined.Public, "Website Content",
        [
            new("Website content", "admin/web-content", Icons.Material.Outlined.Article, MalievPermissions.WebContent.Read, Match: NavLinkMatch.All, Description: "Main site content operations"),
            new("Blog posts", "admin/web-content?section=blog", Icons.Material.Outlined.Newspaper, MalievPermissions.WebContent.Read, Description: "Maliev.Web journal content"),
            new("Homepage content", "admin/web-content?section=homepage", Icons.Material.Outlined.Web, MalievPermissions.WebContent.Read, Description: "Hero, services, case studies"),
            new("Website chatbot", "admin/chatbot-instructions", Icons.Material.Outlined.SupportAgent, MalievPermissions.Chat.InstructionsRead, Description: "Mali persona and skill prompts"),
        ]),
        new("Finance", Icons.Material.Outlined.AccountBalanceWallet, null,
        [
            new("Accounting", "accounting", Icons.Material.Outlined.AccountBalance, MalievPermissions.Accounting.Read, Description: "Ledger, reports, periods"),
            new("Invoices", "finance/invoices", Icons.Material.Outlined.Receipt, MalievPermissions.Invoice.Read, Description: "Invoice list and lifecycle"),
            new("New invoice", "accounting/new", Icons.Material.Outlined.ReceiptLong, MalievPermissions.Invoice.Create, Description: "Create an invoice"),
        ]),
        new("Delivery", Icons.Material.Outlined.LocalShipping, null,
        [
            new("Delivery notes", "finance/delivery-notes", Icons.Material.Outlined.LocalShipping, MalievPermissions.Delivery.Read, Description: "Delivery note workflow"),
            new("New delivery note", "finance/delivery-notes/new", Icons.Material.Outlined.AddRoad, MalievPermissions.Delivery.Create, Description: "Create delivery documentation"),
        ]),
        new("Manufacturing", Icons.Material.Outlined.PrecisionManufacturing, null,
        [
            new("Materials", "mfg/materials", Icons.Material.Outlined.Inventory, MalievPermissions.Material.Read, Description: "Material catalog and properties"),
            new("Equipment", "mfg/equipment", Icons.Material.Outlined.PrecisionManufacturing, MalievPermissions.Facility.Read, Description: "Facility-backed equipment"),
            new("Production schedule", "mfg/production-schedule", Icons.Material.Outlined.Event, MalievPermissions.Job.Read, Description: "Job planning board"),
        ]),
        new("Purchasing", Icons.Material.Outlined.ShoppingBag, null,
        [
            new("Purchase orders", "purchasing", Icons.Material.Outlined.ShoppingBag, MalievPermissions.PurchaseOrder.Read, Description: "Procurement queue"),
            new("New PO", "purchasing/new", Icons.Material.Outlined.Inventory2, MalievPermissions.PurchaseOrder.Create, Description: "Create a purchase order"),
            new("Suppliers", "purchasing/suppliers", Icons.Material.Outlined.AddBusiness, MalievPermissions.Supplier.Read, Description: "Supplier profiles and documents"),
        ]),
        new("Reference", Icons.Material.Outlined.TravelExplore, "Reference Data",
        [
            new("Reference dashboard", "admin/reference-data", Icons.Material.Outlined.TravelExplore, MalievPermissions.Registry.LocationsRead, Match: NavLinkMatch.All, Description: "Countries, currencies, registry"),
            new("Countries", "admin/reference-data?section=countries", Icons.Material.Outlined.Flag, MalievPermissions.Registry.LocationsRead, Description: "CountryService data"),
            new("Currencies", "admin/reference-data?section=currencies", Icons.Material.Outlined.Paid, MalievPermissions.Currency.CurrenciesRead, Description: "CurrencyService catalog"),
            new("Exchange rates", "admin/reference-data?section=exchange-rates", Icons.Material.Outlined.CurrencyExchange, MalievPermissions.Currency.RatesRead, Description: "CurrencyService live rates"),
            new("Registry locations", "admin/reference-data?section=registry", Icons.Material.Outlined.Map, MalievPermissions.Registry.LocationsRead, Description: "RegistryService address data"),
        ]),
        new("Admin", Icons.Material.Outlined.Settings, null,
        [
            new("Admin hub", "admin", Icons.Material.Outlined.Settings, MalievPermissions.Iam.Manage, Match: NavLinkMatch.All, Description: "Operations and notifications"),
            new("Notification templates", "admin?tab=Notifications", Icons.Material.Outlined.Notifications, MalievPermissions.Notification.ReadTemplate, Description: "Email and message templates"),
            new("Chatbot instructions", "admin/chatbot-instructions", Icons.Material.Outlined.SupportAgent, MalievPermissions.Chat.InstructionsRead, Description: "Customer-facing AI prompts"),
            new("System health", "admin/system-health", Icons.Material.Outlined.HealthAndSafety, MalievPermissions.System.HealthRead, Description: "Service liveness and readiness"),
            new("IAM", "iam", Icons.Material.Outlined.Security, MalievPermissions.Iam.Manage, Description: "Users, roles, permissions"),
            new("New user", "iam/users/new", Icons.Material.Outlined.PersonAdd, MalievPermissions.Iam.PrincipalsCreate, Description: "Create employee access"),
        ]),
        new("People", Icons.Material.Outlined.Groups, null,
        [
            new("My profile", "hr/profile", Icons.Material.Outlined.AccountCircle, MalievPermissions.Employee.ProfileRead, Description: "Employee profile"),
            new("Preferences", "hr/profile?tab=preferences", Icons.Material.Outlined.Tune, MalievPermissions.Preference.Read, Description: "Personal settings"),
            new("Leave", "hr/leave", Icons.Material.Outlined.EventAvailable, MalievPermissions.Leave.Read, Description: "Leave requests"),
        ]),
    ];

    public static IReadOnlyList<AppNavGroup> DesktopGroups { get; } =
    [
        PrimaryGroups[0],
        PrimaryGroups[1],
        PrimaryGroups[3],
        PrimaryGroups[5],
    ];

    public static IReadOnlyList<AppNavGroup> DesktopOverflowGroups { get; } =
    [
        PrimaryGroups[2],
        PrimaryGroups[4],
        PrimaryGroups[6],
        PrimaryGroups[7],
        PrimaryGroups[8],
        PrimaryGroups[9],
    ];
}

internal sealed record AppNavGroup(
    string Label,
    string Icon,
    string? SidebarLabel,
    IReadOnlyList<AppNavItem> Items);

internal sealed record AppNavItem(
    string Label,
    string Href,
    string Icon,
    string? Permission = null,
    NavLinkMatch Match = NavLinkMatch.Prefix,
    string? Class = null,
    string? Description = null);
