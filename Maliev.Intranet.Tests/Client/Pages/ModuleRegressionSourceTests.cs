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
    public void AdminPage_DoesNotExposeTenantOrMachinesTab()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "AdminPage.razor");

        Assert.DoesNotContain("Tenant profile", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Machines\"", source, StringComparison.Ordinal);
        Assert.Contains("/admin/system-health", source, StringComparison.Ordinal);
        Assert.Contains("/mfg/equipment", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminPage_DoesNotRenderDuplicateHealthSnapshot()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "AdminPage.razor");

        Assert.Contains("System health checks", source, StringComparison.Ordinal);
        Assert.Contains("Href=\"/admin/system-health\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Health snapshot", source, StringComparison.Ordinal);
        Assert.DoesNotContain("api/v1/system-health", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientLoginRoute_IsNotUsedForEmployeeLogin()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Login.razor");

        Assert.DoesNotContain("@page \"/login\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void NotFoundPage_UsesAnimatedSearchingEyes()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "NotFound.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "NotFound.razor.css");

        Assert.Contains("not-found-eyes", page, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Animated eyes looking around\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Icons.Material.Outlined.SearchOff", page, StringComparison.Ordinal);
        Assert.Contains(".not-found-eyes::before", styles, StringComparison.Ordinal);
        Assert.Contains(".not-found-eyes::after", styles, StringComparison.Ordinal);
        Assert.Contains("@keyframes not-found-looking-around", styles, StringComparison.Ordinal);
        Assert.Contains("background-position: 65% 65%", styles, StringComparison.Ordinal);
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
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");

        Assert.DoesNotContain("new(\"Dashboard\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("title=\"Dashboard\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".topbar-logo-button:hover", styles, StringComparison.Ordinal);
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
        Assert.Contains("InputFile", source, StringComparison.Ordinal);
        Assert.Contains("Payroll journals are produced by CompensationService payroll runs", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/invoices?page={_invoicePage}&pageSize={_pageSize}", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/accounting/journal-entries?page={_journalPage}&pageSize={_pageSize}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreatePayrollAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Enter valid debit and credit account IDs", source, StringComparison.Ordinal);
        Assert.DoesNotContain("page=1&pageSize=50", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EquipmentPages_UseFacilityBackedModulePaginationAndDetailWorkstreams()
    {
        var list = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "EquipmentList.razor");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "EquipmentDetail.razor");

        Assert.Contains("@page \"/mfg/equipment\"", list, StringComparison.Ordinal);
        Assert.Contains("PaginationFooter", list, StringComparison.Ordinal);
        Assert.Contains("api/v1/equipments?{string.Join", list, StringComparison.Ordinal);
        Assert.DoesNotContain("page=1&pageSize=50", list, StringComparison.Ordinal);
        Assert.Contains("/notes", detail, StringComparison.Ordinal);
        Assert.Contains("/maintenance", detail, StringComparison.Ordinal);
        Assert.Contains("/loans", detail, StringComparison.Ordinal);
        Assert.Contains("/attachments", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void MaterialPages_UseSharedPageBodySpacing()
    {
        var list = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "MaterialList.razor");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "MaterialDetail.razor");

        Assert.Contains("@page \"/mfg/materials\"", list, StringComparison.Ordinal);
        Assert.Contains("@page \"/mfg/materials/{Id:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("<PageBody>", list, StringComparison.Ordinal);
        Assert.Contains("</PageBody>", list, StringComparison.Ordinal);
        Assert.Contains("<PageBody>", detail, StringComparison.Ordinal);
        Assert.Contains("</PageBody>", detail, StringComparison.Ordinal);
        Assert.Contains("<StatBar Class=\"mb-4\">", list, StringComparison.Ordinal);
        Assert.Contains("<StatBar Class=\"mb-4\">", detail, StringComparison.Ordinal);
        Assert.Contains("Label=\"Processes\"", detail, StringComparison.Ordinal);
        Assert.Contains("material-process-chip", detail, StringComparison.Ordinal);
        Assert.Contains("material-color-dot", detail, StringComparison.Ordinal);
        Assert.Contains("MalievPermissions.Material.Update", detail, StringComparison.Ordinal);
        Assert.Contains("roles.platform.owner", detail, StringComparison.Ordinal);
        Assert.Contains("Aggregate stock; barcode lots pending", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard>", list, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Profile\">", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Properties\">", detail, StringComparison.Ordinal);
        Assert.Contains("class=\"mlv-table\"", list, StringComparison.Ordinal);
        Assert.Contains("class=\"mlv-detail-list\"", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-stats-grid", list, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-stats-grid", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-panel", list, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-panel", detail, StringComparison.Ordinal);
        Assert.Contains("PaginationFooter", list, StringComparison.Ordinal);
        Assert.Contains("api/v1/materials?page={_page}&pageSize={_pageSize}", list, StringComparison.Ordinal);
        Assert.Contains("api/v1/materials/{Id}", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemHealthPage_RendersAllServiceHealthFields()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "SystemHealth.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "SystemHealth.razor.css");
        var controller = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "SystemHealthController.cs");
        var probeService = ReadRepoFile("Maliev.Intranet.Bff", "Services", "SystemHealthProbeService.cs");

        Assert.Contains("@page \"/admin/system-health\"", page, StringComparison.Ordinal);
        Assert.Contains("DomainGroup", page, StringComparison.Ordinal);
        Assert.Contains("PanelCard Title=\"Services\"", page, StringComparison.Ordinal);
        Assert.Contains("_history.Services.OrderBy(s => s.ServiceName)", page, StringComparison.Ordinal);
        Assert.Contains("CurrentServices.FirstOrDefault(s => s.ServiceName == service.ServiceName)", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleDetails", page, StringComparison.Ordinal);
        Assert.DoesNotContain("PanelCard Title=\"Current Probe Details\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GroupBy(s => s.DomainGroup)", page, StringComparison.Ordinal);
        Assert.Contains("LivenessPath", page, StringComparison.Ordinal);
        Assert.Contains("ReadinessPath", page, StringComparison.Ordinal);
        Assert.Contains("ErrorBody", page, StringComparison.Ordinal);
        Assert.Contains("api/v1/system-health/history?days=7", page, StringComparison.Ordinal);
        Assert.Contains("health-strip", page, StringComparison.Ordinal);
        Assert.Contains("DisplayBucketMinutes = 240", page, StringComparison.Ordinal);
        Assert.Contains("GetDisplayBuckets", page, StringComparison.Ordinal);
        Assert.Contains("7 days ago", page, StringComparison.Ordinal);
        Assert.Contains("% uptime", page, StringComparison.Ordinal);
        Assert.Contains("no data", page, StringComparison.Ordinal);
        Assert.Contains("system-health-probe-grid", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("system-health-detail-list", styles, StringComparison.Ordinal);
        Assert.Contains("gap: 20px;", styles, StringComparison.Ordinal);
        Assert.Contains("padding-bottom: 20px;", styles, StringComparison.Ordinal);
        Assert.Contains("gap: 16px;", styles, StringComparison.Ordinal);
        Assert.Contains("display: flex", styles, StringComparison.Ordinal);
        Assert.Contains("flex: 1 1 0", styles, StringComparison.Ordinal);
        Assert.Contains("overflow: hidden", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 480px)", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 768px)", styles, StringComparison.Ordinal);
        Assert.Contains("health-bucket-healthy", styles, StringComparison.Ordinal);
        Assert.Contains("health-bucket-unhealthy", styles, StringComparison.Ordinal);
        Assert.Contains("health-bucket-unreachable", styles, StringComparison.Ordinal);
        Assert.Contains("health-bucket-nodata", styles, StringComparison.Ordinal);
        Assert.Contains("GetSystemHealthHistory", controller, StringComparison.Ordinal);
        Assert.Contains("FacilityService", probeService, StringComparison.Ordinal);
        Assert.Contains("InventoryService", probeService, StringComparison.Ordinal);
        Assert.Contains("DeliveryService", probeService, StringComparison.Ordinal);
        Assert.Contains("ChatbotService", probeService, StringComparison.Ordinal);
        Assert.Contains("\"predictionservice\"", probeService, StringComparison.Ordinal);
        Assert.Contains("LivenessPath", probeService, StringComparison.Ordinal);
        Assert.Contains("ReadinessPath", probeService, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromSeconds(5)", probeService, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromSeconds(10)", probeService, StringComparison.Ordinal);
        Assert.Contains("Task.WhenAny", probeService, StringComparison.Ordinal);
        Assert.DoesNotContain("aspire-liveness", probeService, StringComparison.Ordinal);
    }

    [Fact]
    public void IamUserList_UsesPagedBffUsersEndpoint()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Iam", "UserList.razor");
        var controller = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "IamController.cs");

        Assert.Contains("PagedResponse<PrincipalSummaryDto>", source, StringComparison.Ordinal);
        Assert.Contains("PagedResponse<RoleDto>", source, StringComparison.Ordinal);
        Assert.Contains("PagedResponse<PermissionDto>", source, StringComparison.Ordinal);
        Assert.Contains("PaginationFooter", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/iam/users?{string.Join", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/iam/roles/paged?{string.Join", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/iam/permissions/paged?{string.Join", source, StringComparison.Ordinal);
        Assert.Contains("OnUserStatusChanged", source, StringComparison.Ordinal);
        Assert.Contains("OnUserTypeChanged", source, StringComparison.Ordinal);
        Assert.Contains("OnRoleServiceChanged", source, StringComparison.Ordinal);
        Assert.Contains("OnPermissionCategoryChanged", source, StringComparison.Ordinal);
        Assert.Contains("Task<ActionResult<PagedResponse<PrincipalSummaryDto>>> GetUsers", controller, StringComparison.Ordinal);
        Assert.Contains("Task<ActionResult<PagedResponse<RoleDto>>> GetRolesPaged", controller, StringComparison.Ordinal);
        Assert.Contains("Task<ActionResult<PagedResponse<PermissionDto>>> GetPermissionsPaged", controller, StringComparison.Ordinal);
        Assert.Contains("CreatePrincipalAsync", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void IamUserDetail_UsesSinglePrincipalEndpoint()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Iam", "UserDetail.razor");
        var controller = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "IamController.cs");

        Assert.Contains("GetFromJsonAsync<PrincipalSummaryDto>($\"api/v1/iam/users/{Id}\")", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetFromJsonAsync<List<PrincipalSummaryDto>>(\"api/v1/iam/users\")", source, StringComparison.Ordinal);
        Assert.Contains("Task<ActionResult<PrincipalSummaryDto>> GetUser", controller, StringComparison.Ordinal);
        Assert.Contains("GetPrincipalAsync(principalId", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void IamRoles_RenderDisplayNameWithRoleIdFallback()
    {
        var list = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Iam", "UserList.razor");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Iam", "UserDetail.razor");
        var client = ReadRepoFile("Maliev.Intranet.Bff", "Clients", "IAMServiceClient.cs");

        Assert.Contains("RoleDisplayName(role)", list, StringComparison.Ordinal);
        Assert.Contains("RoleSubtitle(role)", list, StringComparison.Ordinal);
        Assert.Contains("HumanizeRoleId(role.RoleId)", client, StringComparison.Ordinal);
        Assert.Contains("RoleDisplayName(role.RoleName, role.RoleId)", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_CurrencySelectorTrigger_RemainsCompact()
    {
        var razor = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var mudOverrides = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "mudblazor-overrides.css");

        Assert.Contains("Style=\"width: fit-content; min-width: max-content; max-width: max-content; flex: 0 0 auto;\"", razor, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete .mud-input-control", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete .mud-input-control-input-container", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete .mud-input-slot", source, StringComparison.Ordinal);
        Assert.Contains("width: 5ch !important;", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete .mud-input-underline::before", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete .mud-input-underline::after", source, StringComparison.Ordinal);
        Assert.Contains("border-bottom: 0 !important;", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-currency-popover", mudOverrides, StringComparison.Ordinal);
        Assert.Contains("min-width: 220px;", mudOverrides, StringComparison.Ordinal);
        Assert.DoesNotContain("width: 80px;", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("min-width: 120px;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_GlobalSearch_UsesTopbarSurfaceColors()
    {
        var razor = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var searchStyles = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "GlobalSearchBox.razor.css");
        var searchBlock = ExtractCssBlock(source, ".topbar-root ::deep .topbar-global-search .global-search-input");
        var topbarRootBlock = ExtractCssBlock(source, ".topbar-root");
        var searchIndex = razor.IndexOf("class=\"topbar-search\"", StringComparison.Ordinal);
        var currencyIndex = razor.IndexOf("Class=\"topbar-currency-autocomplete\"", StringComparison.Ordinal);

        Assert.NotEqual(-1, searchIndex);
        Assert.NotEqual(-1, currencyIndex);
        Assert.True(searchIndex < currencyIndex);
        Assert.Contains("overflow: visible;", topbarRootBlock, StringComparison.Ordinal);
        Assert.Contains("background: var(--maliev-panel-3);", searchBlock, StringComparison.Ordinal);
        Assert.Contains("color: var(--maliev-ink);", searchBlock, StringComparison.Ordinal);
        Assert.Contains("border: 0;", searchBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", searchBlock, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-global-search .global-search-input:focus", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-global-search .global-search-icon", source, StringComparison.Ordinal);
        Assert.Contains(".global-search.topbar-global-search .global-search-panel", searchStyles, StringComparison.Ordinal);
        Assert.Contains("right: 0;", searchStyles, StringComparison.Ordinal);
        Assert.Contains("left: auto;", searchStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_RightIconButtons_AreBorderless()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var currencyFieldBlock = ExtractCssBlock(source, ".topbar-root ::deep .topbar-currency-autocomplete .mud-input");
        var iconButtonBlock = ExtractCssBlock(source, ".topbar-right ::deep .mud-button-root.mud-icon-button");

        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", currencyFieldBlock, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete .mud-input-adornment", source, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", iconButtonBlock, StringComparison.Ordinal);
        Assert.Contains("background: transparent;", iconButtonBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", iconButtonBlock, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .mud-button-root.mud-icon-button:hover", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .mud-button-root.mud-icon-button:focus-visible", source, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-focus-ring) !important;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_TabletAndMobileLayoutKeepsNavSearchAndProfileInBounds()
    {
        var razor = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var searchStyles = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "GlobalSearchBox.razor.css");

        Assert.Contains("class=\"topbar-spacer\"", razor, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1200px)", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-profile-info { display: none; }", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1120px)", styles, StringComparison.Ordinal);
        Assert.Contains("flex-wrap: wrap;", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-search", styles, StringComparison.Ordinal);
        Assert.Contains("display: block;", styles, StringComparison.Ordinal);
        Assert.Contains("flex: 1 1 100%;", styles, StringComparison.Ordinal);
        Assert.DoesNotContain(".topbar-search { display: none; }", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("@media (max-width: 960px)", searchStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("display: none", searchStyles, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_QuoteNavigationAction_UsesPrimaryColor()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var quoteBlock = ExtractCssBlock(source, ".topbar-nav ::deep .mud-nav-link.topbar-nav-quote");

        Assert.Contains("--topbar-quote-accent: var(--mud-palette-primary);", quoteBlock, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", quoteBlock, StringComparison.Ordinal);
        Assert.Contains("background: var(--mud-palette-primary) !important;", quoteBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring)", quoteBlock, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav ::deep .topbar-nav-quote .mud-nav-link", source, StringComparison.Ordinal);
        Assert.DoesNotContain("--maliev-accent", quoteBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("#f97316", quoteBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginShell_UsesGatewayCardDesignWithoutRouteChanges()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Login.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Login.razor.css");
        var bffLogin = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "LoginPageController.cs");

        Assert.Contains("/api/v1/auth/login", source, StringComparison.Ordinal);
        Assert.Contains("Sign in with Google", source, StringComparison.Ordinal);
        Assert.Contains("class=\"login-gateway-card\"", source, StringComparison.Ordinal);
        Assert.Contains("footer-note-link", source, StringComparison.Ordinal);
        Assert.Contains("MALIEV CO., LTD.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Support", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System Status", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MALIEV INC. ALL RIGHTS RESERVED.", source, StringComparison.Ordinal);
        var loginHeaderBlock = ExtractCssBlock(styles, ".login-header {");
        var themeToggleBlock = ExtractCssBlock(styles, ".theme-toggle-btn {");
        var bffThemeToggleRootIndex = bffLogin.LastIndexOf(".theme-toggle-btn {", StringComparison.Ordinal);
        Assert.True(bffThemeToggleRootIndex >= 0, "Expected the BFF login page to have a root theme-toggle block.");
        var bffThemeToggleBlock = ExtractCssBlock(bffLogin[bffThemeToggleRootIndex..], ".theme-toggle-btn {");
        Assert.Contains("background: var(--maliev-bg);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", styles, StringComparison.Ordinal);
        Assert.Contains("border-radius: var(--maliev-radius-md);", styles, StringComparison.Ordinal);
        Assert.Contains("background: var(--mud-palette-primary);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", loginHeaderBlock, StringComparison.Ordinal);
        Assert.Contains("background: transparent;", themeToggleBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", themeToggleBlock, StringComparison.Ordinal);
        Assert.Contains(".login-gateway-card", styles, StringComparison.Ordinal);
        Assert.Contains("letter-spacing: 0;", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("glass-card", source, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("text-transform: uppercase;", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("letter-spacing: -", styles, StringComparison.Ordinal);

        Assert.Contains("/api/v1/auth/login-form", bffLogin, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/login?returnUrl", bffLogin, StringComparison.Ordinal);
        Assert.Contains("family=Geist:wght@400..700", bffLogin, StringComparison.Ordinal);
        Assert.Contains("family=Geist+Mono:wght@400..600", bffLogin, StringComparison.Ordinal);
        Assert.Contains("Noto+Sans+Thai", bffLogin, StringComparison.Ordinal);
        Assert.Contains("login-gateway-card", bffLogin, StringComparison.Ordinal);
        Assert.Contains("Sign in to MALIEV", bffLogin, StringComparison.Ordinal);
        Assert.Contains("MALIEV CO., LTD.", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("Support", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("System Status", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("MALIEV INC. ALL RIGHTS RESERVED.", bffLogin, StringComparison.Ordinal);
        Assert.Contains("--maliev-shadow-card", bffLogin, StringComparison.Ordinal);
        Assert.Contains("background: var(--maliev-bg);", bffLogin, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", ExtractCssBlock(bffLogin, ".login-header {"), StringComparison.Ordinal);
        Assert.Contains("background: transparent;", bffThemeToggleBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", bffThemeToggleBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("JetBrains+Mono", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("--accent-hue", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("SIGN IN", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("box-shadow: 0 18px", bffLogin, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectNewDelegatedCss_UsesSharedSurfaceTokens()
    {
        var projectNew = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.css");
        var partRow = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartListRow.razor.css");
        var sidebar = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartConfigSidebar.razor.css");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor.css");
        var bulkTable = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "ProjectPartsBulkTable.razor.css");

        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", projectNew, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", partRow, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", sidebar, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", detail, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", bulkTable, StringComparison.Ordinal);
        Assert.DoesNotContain("box-shadow: 0 22px 60px", bulkTable, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignFoundation_UsesGeistFontsAndOperationalAdaptation()
    {
        var clientHost = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "index.html");
        var bffHost = ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");
        var designBrief = ReadRepoFile("DESIGN.md");

        Assert.Contains("family=Geist:wght@400..700", clientHost, StringComparison.Ordinal);
        Assert.Contains("family=Geist+Mono:wght@400..600", clientHost, StringComparison.Ordinal);
        Assert.Contains("Noto+Sans+Thai", clientHost, StringComparison.Ordinal);
        Assert.Contains("--mud-typography-default-family: 'Geist', 'Noto Sans Thai', sans-serif;", clientHost, StringComparison.Ordinal);

        Assert.Contains("family=Geist:wght@400..700", bffHost, StringComparison.Ordinal);
        Assert.Contains("family=Geist+Mono:wght@400..600", bffHost, StringComparison.Ordinal);
        Assert.Contains("Noto+Sans+Thai", bffHost, StringComparison.Ordinal);

        Assert.Contains("MALIEV adaptation", designBrief, StringComparison.Ordinal);
        Assert.Contains("letter spacing remains 0", designBrief, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operational density", designBrief, StringComparison.OrdinalIgnoreCase);
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

    private static string ExtractCssBlock(string source, string selector)
    {
        var selectorIndex = source.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(selectorIndex >= 0, $"Expected selector '{selector}' to exist.");

        var blockStart = source.IndexOf('{', selectorIndex);
        Assert.True(blockStart >= 0, $"Expected selector '{selector}' to have a declaration block.");

        var blockEnd = source.IndexOf('}', blockStart);
        Assert.True(blockEnd >= 0, $"Expected selector '{selector}' declaration block to close.");

        return source.Substring(blockStart + 1, blockEnd - blockStart - 1);
    }
}
