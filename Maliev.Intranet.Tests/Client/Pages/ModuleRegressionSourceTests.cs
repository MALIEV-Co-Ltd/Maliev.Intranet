using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Maliev.Intranet.Tests.Client.Pages;

public class ModuleRegressionSourceTests
{
    [Fact]
    public void ClientTextInputs_UpdateOnInputInsteadOfBlur()
    {
        var clientRoot = FindRepoDirectory("Maliev.Intranet.Client");
        var offenders = new List<string>();
        var mudInputPattern = new Regex("<Mud(?<component>TextField|NumericField|Autocomplete)\\b");
        var nativeInputPattern = new Regex("<(?<component>input|textarea)\\b");
        var deferredBlazorInputPattern = new Regex("<(?<component>InputText|InputTextArea|InputNumber)\\b");

        foreach (var file in Directory.EnumerateFiles(clientRoot, "*.razor", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var match in mudInputPattern.Matches(source).Cast<System.Text.RegularExpressions.Match>())
            {
                var block = ReadStartTag(source, match.Index);
                if (IsEditableBoundMudInput(block) && !block.Contains("Immediate=\"true\"", StringComparison.Ordinal))
                {
                    offenders.Add(FormatInputOffender(clientRoot, file, source, match.Index, match.Groups["component"].Value));
                }
            }

            foreach (var match in nativeInputPattern.Matches(source).Cast<System.Text.RegularExpressions.Match>())
            {
                var block = ReadStartTag(source, match.Index);
                if ((IsEditableBoundNativeTextInput(block) && !block.Contains("@bind:event=\"oninput\"", StringComparison.Ordinal))
                    || IsEditableNativeTextInputWithChangeHandler(block))
                {
                    offenders.Add(FormatInputOffender(clientRoot, file, source, match.Index, match.Groups["component"].Value));
                }
            }

            foreach (var match in deferredBlazorInputPattern.Matches(source).Cast<System.Text.RegularExpressions.Match>())
            {
                offenders.Add(FormatInputOffender(clientRoot, file, source, match.Index, match.Groups["component"].Value));
            }
        }

        Assert.True(offenders.Count == 0, $"Editable text-like inputs must update on input, not blur:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

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
    public void AdminPage_LinksToChatbotInstructionManagement()
    {
        var adminPage = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "AdminPage.razor");
        var instructionPage = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");
        var permissions = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "MalievPermissions.cs");

        Assert.Contains("Chatbot instructions", adminPage, StringComparison.Ordinal);
        Assert.Contains("Href=\"/admin/chatbot-instructions\"", adminPage, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/chatbot-instructions\"", instructionPage, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MalievPermissions.Chat.InstructionsRead)", instructionPage, StringComparison.Ordinal);
        Assert.Contains("chatbot.instructions.write", instructionPage, StringComparison.Ordinal);
        Assert.Contains("InputFile OnChange=\"ImportPromptFileAsync\"", instructionPage, StringComparison.Ordinal);
        Assert.Contains("Topic entries act as dynamic SKILLS", instructionPage, StringComparison.Ordinal);
        Assert.Contains("public const string InstructionsRead = \"chatbot.instructions.read\";", permissions, StringComparison.Ordinal);
        Assert.Contains("public const string InstructionsWrite = \"chatbot.instructions.write\";", permissions, StringComparison.Ordinal);
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
        Assert.DoesNotContain("aria-label=\"Go to dashboard\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".topbar-logo-button:hover", styles, StringComparison.Ordinal);
        Assert.Contains("<a href=\"/\"", source, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Go to application home\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Navigation.NavigateTo(\"/\")", source, StringComparison.Ordinal);
        Assert.Contains("My Profile", source, StringComparison.Ordinal);
        Assert.Contains("Preferences", source, StringComparison.Ordinal);
        Assert.Contains("Sign out", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SnackbarPolicy_UsesNonBlockingDefaultsAndInlineAiExtractionFeedback()
    {
        var clientProgram = ReadRepoFile("Maliev.Intranet.Client", "Program.cs");
        var bffProgram = ReadRepoFile("Maliev.Intranet.Bff", "Program.cs");
        var snackbarPolicy = ReadRepoFile("Maliev.Intranet.Client", "Services", "MalievMudServices.cs");
        var mainLayout = ReadRepoFile("Maliev.Intranet.Client", "Layout", "MainLayout.razor");
        var customerNew = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerNew.razor");

        Assert.Contains("builder.Services.AddMalievMudServices();", clientProgram, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddMalievMudServices();", bffProgram, StringComparison.Ordinal);
        Assert.Contains("PositionClass = Defaults.Classes.Position.BottomLeft", snackbarPolicy, StringComparison.Ordinal);
        Assert.Contains("MaxDisplayedSnackbars = 2", snackbarPolicy, StringComparison.Ordinal);
        Assert.Contains("PreventDuplicates = true", snackbarPolicy, StringComparison.Ordinal);
        Assert.Contains("ShowCloseIcon = true", snackbarPolicy, StringComparison.Ordinal);
        Assert.Contains("VisibleStateDuration = 2600", snackbarPolicy, StringComparison.Ordinal);
        Assert.Contains("ClearAfterNavigation = true", snackbarPolicy, StringComparison.Ordinal);
        Assert.Contains("config.VisibleStateDuration = 5000;", mainLayout, StringComparison.Ordinal);
        Assert.DoesNotContain("config.VisibleStateDuration = 10000;", mainLayout, StringComparison.Ordinal);
        Assert.Contains("AI extraction review", customerNew, StringComparison.Ordinal);
        Assert.Contains("DismissExtractionSummary", customerNew, StringComparison.Ordinal);
        Assert.Contains("GetMissingExtractionItems", customerNew, StringComparison.Ordinal);
        Assert.Contains("Needs input", customerNew, StringComparison.Ordinal);
        Assert.DoesNotContain("Autofilled from AI extraction", customerNew, StringComparison.Ordinal);
        Assert.DoesNotContain("Customer fields were autofilled from the highest-confidence AI extraction.", customerNew, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_ExposesRouteBackedServiceNavigation()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var overrides = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "mudblazor-overrides.css");
        var navMenu = ReadRepoFile("Maliev.Intranet.Client", "Layout", "NavMenu.razor");
        var appNavigation = ReadRepoFile("Maliev.Intranet.Client", "Layout", "AppNavigation.cs");

        Assert.Contains("AppNavigation.PrimaryGroups", source, StringComparison.Ordinal);
        Assert.Contains("AppNavigation.DesktopGroups", source, StringComparison.Ordinal);
        Assert.Contains("AppNavigation.DesktopOverflowGroups", source, StringComparison.Ordinal);
        Assert.Contains("internal sealed record AppNavGroup", appNavigation, StringComparison.Ordinal);
        Assert.Contains("DesktopGroups", appNavigation, StringComparison.Ordinal);
        Assert.Contains("DesktopOverflowGroups", appNavigation, StringComparison.Ordinal);
        Assert.Contains("topbar-nav-menu-trigger", source, StringComparison.Ordinal);
        Assert.Contains("topbar-nav-more-trigger", source, StringComparison.Ordinal);
        Assert.Contains("topbar-nav-more-popover", source, StringComparison.Ordinal);
        Assert.Contains("topbar-nav-more-section-title", source, StringComparison.Ordinal);
        Assert.Contains("Storefront catalog", appNavigation, StringComparison.Ordinal);
        Assert.Contains("\"commerce/catalog\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"Materials\", \"mfg/materials\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"Equipment\", \"mfg/equipment\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"Production schedule\", \"mfg/production-schedule\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"Suppliers\", \"purchasing/suppliers\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"Reference dashboard\", \"admin/reference-data\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"System health\", \"admin/system-health\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"IAM\", \"iam\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("new(\"Leave\", \"hr/leave\"", appNavigation, StringComparison.Ordinal);
        Assert.Contains("Navigation.LocationChanged += OnLocationChanged", source, StringComparison.Ordinal);
        Assert.Contains("IsNavGroupActive", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav-menu-trigger", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav-more-trigger", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav-more-section-title", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-mobile-nav-group", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav-popover", overrides, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav-more-popover", overrides, StringComparison.Ordinal);
        Assert.Contains("Href=\"@item.Href\"", navMenu, StringComparison.Ordinal);
        Assert.Contains("AppNavigation.PrimaryGroups", navMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/admin/blog\"", appNavigation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LeavePage_UsesSegmentedDurationControlAndEqualDateGrid()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Hr", "Leave.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Hr", "Leave.razor.css");

        Assert.Contains("Class=\"leave-request-card\"", source, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", source, StringComparison.Ordinal);
        Assert.Contains("leave-period-option", source, StringComparison.Ordinal);
        Assert.Contains("LeavePeriodOptions", source, StringComparison.Ordinal);
        Assert.Contains("SetLeavePeriod", source, StringComparison.Ordinal);
        Assert.Contains("leave-date-grid", source, StringComparison.Ordinal);
        Assert.Contains("ReadApiErrorMessageAsync", source, StringComparison.Ordinal);
        Assert.Contains("ApiErrorResponse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Half day\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@bind=\"_request.HalfDayPeriod\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("style=\"justify-content:flex-end\"", source, StringComparison.Ordinal);

        Assert.Contains("grid-template-columns: repeat(3, minmax(0, 1fr));", ExtractCssBlock(styles, ".leave-period-options {"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: auto minmax(0, 1fr);", ExtractCssBlock(styles, ".leave-period-option {"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(2, minmax(0, 1fr));", ExtractCssBlock(styles, ".leave-date-grid {"), StringComparison.Ordinal);
        Assert.Contains("justify-content: flex-end;", ExtractCssBlock(styles, ".leave-actions {"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: 1fr;", ExtractCssBlock(styles, "@media (max-width: 760px)"), StringComparison.Ordinal);
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
    public void CustomerDetail_PaymentTermOptionsStretchToMenuWidth()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerDetail.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerDetail.razor.css");
        var normalizedStyles = styles.ReplaceLineEndings("\n");
        var accountGridBlock = ExtractCssBlock(styles, ".customer-account-grid");
        var accountControlBlock = ExtractCssBlock(styles, ".customer-account-grid .customer-input,");
        var selectInputBlock = ExtractCssBlock(styles, "select.customer-input");
        var pickerBlock = ExtractCssBlock(styles, ".customer-payment-term-picker");
        var triggerBlock = ExtractCssBlock(styles, "\n.customer-payment-term-trigger {");
        var triggerCardBlock = ExtractCssBlock(styles, "\n.customer-payment-term-trigger .customer-payment-term-card {");
        var triggerParagraphBlock = ExtractCssBlock(styles, "\n.customer-payment-term-trigger .customer-payment-term-card p {");
        var menuBlock = ExtractCssBlock(styles, ".customer-payment-term-menu");

        Assert.Contains("customer-panel customer-account-panel", source, StringComparison.Ordinal);
        Assert.Contains("customer-form-grid customer-account-grid", source, StringComparison.Ordinal);
        Assert.Contains("customer-field customer-account-manager-field", source, StringComparison.Ordinal);
        Assert.Contains("customer-field customer-payment-terms-field customer-account-payment-field", source, StringComparison.Ordinal);
        Assert.Contains("<span>Company branch</span>", source, StringComparison.Ordinal);
        Assert.Contains("value=\"@CompanyBranchLabel\"", source, StringComparison.Ordinal);
        Assert.Contains("private string CompanyBranchLabel => HasCompany ? \"Head office / สำนักงานใหญ่\" : \"-\";", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Credit limit", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CreditLimit", source, StringComparison.Ordinal);
        Assert.Contains("align-items: start;", accountGridBlock, StringComparison.Ordinal);
        Assert.Contains("min-height: 42px;", accountControlBlock, StringComparison.Ordinal);
        Assert.Contains("appearance: none;", selectInputBlock, StringComparison.Ordinal);
        Assert.Contains("padding-right: 2.4rem;", selectInputBlock, StringComparison.Ordinal);
        Assert.Contains("background-position: right 0.85rem center;", selectInputBlock, StringComparison.Ordinal);
        Assert.Contains("width: 100%;", pickerBlock, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) 32px;", triggerBlock, StringComparison.Ordinal);
        Assert.Contains("border-radius: var(--maliev-radius-sm);", triggerBlock, StringComparison.Ordinal);
        Assert.Contains("min-height: 40px;", triggerCardBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", triggerCardBlock, StringComparison.Ordinal);
        Assert.Contains("display: none;", triggerParagraphBlock, StringComparison.Ordinal);
        Assert.Contains(".customer-payment-term-trigger .customer-payment-term-card strong,\n.customer-payment-term-trigger .customer-payment-term-card-head span {\n    overflow: hidden;\n    text-overflow: ellipsis;\n    white-space: nowrap;", normalizedStyles, StringComparison.Ordinal);
        Assert.Contains("justify-items: stretch;", menuBlock, StringComparison.Ordinal);
        Assert.Contains("box-sizing: border-box;", menuBlock, StringComparison.Ordinal);
        Assert.Contains(".customer-payment-term-option {\n    display: block;\n    box-sizing: border-box;", normalizedStyles, StringComparison.Ordinal);
        Assert.Contains(".customer-payment-term-card {\n    display: grid;\n    gap: 8px;\n    width: 100%;\n    min-width: 0;\n    box-sizing: border-box;", normalizedStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void ModulePages_NativeSelectsUseInsetChevronSpacing()
    {
        var moduleStyles = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "module-pages.css");
        var customerNewStyles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerNew.razor.css");
        var sharedSelectBlock = ExtractCssBlock(moduleStyles, "select.mlv-form-input,");
        var pageSizeSelectBlock = ExtractCssBlock(moduleStyles, ".mlv-page-size-select {");
        var customerNewSelectBlock = ExtractCssBlock(customerNewStyles, ".customer-create-page select.mlv-form-input,");

        Assert.Contains("padding-left: 10px;", pageSizeSelectBlock, StringComparison.Ordinal);
        Assert.Contains("select.mlv-form-select,", moduleStyles, StringComparison.Ordinal);
        Assert.Contains(".mlv-filter-field select,", moduleStyles, StringComparison.Ordinal);
        Assert.Contains(".mlv-page-size-select {", moduleStyles, StringComparison.Ordinal);
        Assert.Contains("appearance: none;", sharedSelectBlock, StringComparison.Ordinal);
        Assert.Contains("padding-right: 2.35rem;", sharedSelectBlock, StringComparison.Ordinal);
        Assert.Contains("background-position: right 0.85rem center;", sharedSelectBlock, StringComparison.Ordinal);
        Assert.Contains("background-size: 0.9rem;", sharedSelectBlock, StringComparison.Ordinal);
        Assert.Contains("padding-right: 2.35rem;", customerNewSelectBlock, StringComparison.Ordinal);
        Assert.Contains("background-position: right 0.85rem center;", customerNewSelectBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerDetail_InternalNotesPreserveMultilineFormatting()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerDetail.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerDetail.razor.css");
        var noteTextBlock = ExtractCssBlock(styles, ".customer-note-text");

        Assert.Contains("class=\"customer-note-text\"", source, StringComparison.Ordinal);
        Assert.Contains("white-space: pre-wrap;", noteTextBlock, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere;", noteTextBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerDetail_AddressDialogPreservesDraftsAndUsesLanguageAwareRegistrySuggestions()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerDetail.razor");

        Assert.Contains("CloseOnBackdropClick=\"false\"", source, StringComparison.Ordinal);
        Assert.Contains("customer-address-lookup-input", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/customers/locations/thai/multi", source, StringComparison.Ordinal);
        Assert.Contains("DetectAddressLanguage", source, StringComparison.Ordinal);
        Assert.Contains("_applyingLocation", source, StringComparison.Ordinal);
        Assert.Contains("malievAddressMap.update", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerDetail_EmailDialogStaysOpenAndUsesNotificationTemplates()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerDetail.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerDetail.razor.css");

        Assert.Contains("<ConfirmModal Open=\"@_emailComposerOpen\"", source, StringComparison.Ordinal);
        Assert.Contains("Class=\"customer-modal-email\"", source, StringComparison.Ordinal);
        Assert.Contains("CloseOnBackdropClick=\"false\"", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/notifications/templates?page=1&pageSize=50&filter=customer-email", source, StringComparison.Ordinal);
        Assert.Contains("SaveEmailTemplateAsync", source, StringComparison.Ordinal);
        Assert.Contains("CreateNotificationTemplateRequest", source, StringComparison.Ordinal);
        Assert.Contains("UpdateNotificationTemplateRequest", source, StringComparison.Ordinal);
        Assert.Contains(".customer-email-template-panel", styles, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(260px, 0.42fr) minmax(0, 1fr);", styles, StringComparison.Ordinal);
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
        var moduleStyles = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "module-pages.css");

        Assert.Contains("Journal Entries", source, StringComparison.Ordinal);
        Assert.Contains("Income Entry", source, StringComparison.Ordinal);
        Assert.Contains("Expense Entry", source, StringComparison.Ordinal);
        Assert.Contains("Payroll Journals", source, StringComparison.Ordinal);
        Assert.Contains("InputFile", source, StringComparison.Ordinal);
        Assert.Contains("accounting-ai-panel", source, StringComparison.Ordinal);
        Assert.Contains("Drag and drop receipts, transfer slips, invoices, or screenshots", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/aiprocessing/extract-accounting-entry", source, StringComparison.Ordinal);
        Assert.Contains("ExtractAccountingEntryAsync", source, StringComparison.Ordinal);
        Assert.Contains("ApplyAccountingExtraction", source, StringComparison.Ordinal);
        Assert.Contains("LoadCurrenciesAsync", source, StringComparison.Ordinal);
        Assert.Contains("OnCurrencyChangedAsync", source, StringComparison.Ordinal);
        Assert.Contains("ExchangeRateToBase", source, StringComparison.Ordinal);
        Assert.Contains("TransactionDebit", source, StringComparison.Ordinal);
        Assert.Contains("Busy=\"@_incomeForm.IsSubmitting\"", source, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", source, StringComparison.Ordinal);
        Assert.Contains("ReadFailureMessageAsync", source, StringComparison.Ordinal);
        Assert.Contains("The selected document could not be read", source, StringComparison.Ordinal);
        Assert.Contains("accounting-report-toolbar", source, StringComparison.Ordinal);
        Assert.Contains("accounting-report-statement", source, StringComparison.Ordinal);
        Assert.Contains("accounting-report-actions", source, StringComparison.Ordinal);
        Assert.Contains("accounting-report-pdf-button", source, StringComparison.Ordinal);
        Assert.Contains("Download PDF", source, StringComparison.Ordinal);
        Assert.Contains("ReportPdfDownloadUrl", source, StringComparison.Ordinal);
        Assert.Contains("ReportPdfDownloadName", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/accounting/reports/{Uri.EscapeDataString(_reportType)}/pdf", source, StringComparison.Ordinal);
        Assert.Contains("ReportPeriodLabel()", source, StringComparison.Ordinal);
        Assert.Contains("<span>Source ledger</span>", source, StringComparison.Ordinal);
        Assert.Contains("accounting-reconciliation-source-select", source, StringComparison.Ordinal);
        Assert.Contains("SelectedReconciliationSourceDescription", source, StringComparison.Ordinal);
        Assert.Contains("new(\"Sales\", \"Sales\", \"Invoices, receipts, and customer receivables\")", source, StringComparison.Ordinal);
        Assert.Contains("new(\"Procurement\", \"Procurement\", \"Supplier invoices and purchasing payables\")", source, StringComparison.Ordinal);
        Assert.Contains("new(\"Inventory\", \"Inventory\", \"Stock movements and inventory valuation\")", source, StringComparison.Ordinal);
        Assert.Contains("new(\"Payroll\", \"Payroll\", \"CompensationService payroll journal impact\")", source, StringComparison.Ordinal);
        Assert.Contains("new(\"System\", \"System imports\", \"Opening balances and administrative imports\")", source, StringComparison.Ordinal);
        Assert.Contains("min-width: 320px;", ExtractCssBlock(moduleStyles, ".accounting-reconciliation-source-field"), StringComparison.Ordinal);
        Assert.Contains("Payroll journals are produced by CompensationService payroll runs", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/invoices?page={_invoicePage}&pageSize={_pageSize}", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/accounting/journal-entries?page={_journalPage}&pageSize={_pageSize}", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/referenceData/currencies/rate?from=", source, StringComparison.Ordinal);
        Assert.Contains("display: flex;", ExtractCssBlock(moduleStyles, ".accounting-report-actions"), StringComparison.Ordinal);
        Assert.Contains("min-height: 36px;", ExtractCssBlock(moduleStyles, ".accounting-report-pdf-button"), StringComparison.Ordinal);
        Assert.DoesNotContain("<input class=\"mlv-form-input\" @bind=\"_reconciliationSource\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreatePayrollAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Enter valid debit and credit account IDs", source, StringComparison.Ordinal);
        Assert.DoesNotContain("page=1&pageSize=50", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EquipmentPages_UseFacilityBackedModulePaginationAndDetailWorkstreams()
    {
        var list = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "EquipmentList.razor");
        var listStyles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "EquipmentList.razor.css");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "EquipmentDetail.razor");
        var detailStyles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "EquipmentDetail.razor.css");
        var facilityDtos = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "FacilityDtos.cs");
        var equipmentsController = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "EquipmentsController.cs");

        Assert.Contains("@page \"/mfg/equipment\"", list, StringComparison.Ordinal);
        Assert.Contains("PaginationFooter", list, StringComparison.Ordinal);
        Assert.Contains("api/v1/equipments?{string.Join", list, StringComparison.Ordinal);
        Assert.DoesNotContain("page=1&pageSize=50", list, StringComparison.Ordinal);
        Assert.Contains("equipment-filter-toolbar", list, StringComparison.Ordinal);
        Assert.Contains("equipment-filter-left", list, StringComparison.Ordinal);
        Assert.Contains("equipment-search-filter", list, StringComparison.Ordinal);
        Assert.Contains("Class=\"equipment-search-box\"", list, StringComparison.Ordinal);
        Assert.Contains(".equipment-filter-toolbar", listStyles, StringComparison.Ordinal);
        Assert.Contains("align-items: end;", listStyles, StringComparison.Ordinal);
        Assert.Contains("::deep .equipment-search-box", listStyles, StringComparison.Ordinal);
        Assert.Contains("height: 36px;", listStyles, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(220px, 1.1fr) repeat(2, minmax(140px, 0.75fr));", listStyles, StringComparison.Ordinal);
        Assert.Contains("/notes", detail, StringComparison.Ordinal);
        Assert.Contains("/maintenance", detail, StringComparison.Ordinal);
        Assert.Contains("/loans", detail, StringComparison.Ordinal);
        Assert.Contains("/attachments", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("PanelCard Title=\"Add note\"", detail, StringComparison.Ordinal);
        Assert.Contains("PanelCard Title=\"Notes\"", detail, StringComparison.Ordinal);
        Assert.Contains("equipment-note-composer", detail, StringComparison.Ordinal);
        Assert.Contains("equipment-notes-list", detail, StringComparison.Ordinal);
        Assert.True(
            detail.IndexOf("PanelCard Title=\"Notes\"", StringComparison.Ordinal) < detail.IndexOf("PanelCard Title=\"Maintenance\"", StringComparison.Ordinal),
            "Equipment notes should be grouped with the note composer near the equipment summary instead of being split into a bottom-only history panel.");
        Assert.Contains("<InputFile OnChange=\"OnMaintenanceFilesSelected\" multiple", detail, StringComparison.Ordinal);
        Assert.Contains("MultipartFormDataContent", detail, StringComparison.Ordinal);
        Assert.Contains("maintenance-document-list", detail, StringComparison.Ordinal);
        Assert.Contains("OpenMaintenanceDocumentAsync", detail, StringComparison.Ordinal);
        Assert.Contains("api/v1/equipments/{Id}/maintenance/download-url", detail, StringComparison.Ordinal);
        Assert.Contains(".equipment-note-composer", detailStyles, StringComparison.Ordinal);
        Assert.Contains(".equipment-notes-list", detailStyles, StringComparison.Ordinal);
        Assert.Contains(".maintenance-document-list", detailStyles, StringComparison.Ordinal);
        Assert.Contains(".maintenance-file-input", detailStyles, StringComparison.Ordinal);
        Assert.Contains("public List<MaintenanceLogDocumentDto> Documents { get; set; } = [];", facilityDtos, StringComparison.Ordinal);
        Assert.Contains("public List<CreateMaintenanceLogDocumentDto> Documents { get; set; } = [];", facilityDtos, StringComparison.Ordinal);
        Assert.Contains("[FromForm] List<IFormFile>? files", equipmentsController, StringComparison.Ordinal);
        Assert.Contains("equipment-maintenance/{id}", equipmentsController, StringComparison.Ordinal);
        Assert.Contains("maintenance/download-url", equipmentsController, StringComparison.Ordinal);
    }

    [Fact]
    public void MaterialPages_UseSharedPageBodySpacing()
    {
        var list = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "MaterialList.razor");
        var listStyles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "MaterialList.razor.css");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "MaterialDetail.razor");
        var detailStyles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Manufacturing", "MaterialDetail.razor.css");
        var inventoryDtos = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "InventoryDtos.cs");
        var materialClient = ReadRepoFile("Maliev.Intranet.Bff", "Clients", "MaterialServiceClient.cs");
        var materialsController = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "MaterialsController.cs");

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
        Assert.DoesNotContain("Aggregate stock; barcode lots pending", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard>", list, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Inventory control\">", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Profile\">", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Properties\">", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Stock audit\">", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Inventory label\">", detail, StringComparison.Ordinal);
        Assert.Contains("<PanelCard Title=\"Suppliers\">", detail, StringComparison.Ordinal);
        Assert.Contains("BuildCode39Svg", detail, StringComparison.Ordinal);
        Assert.Contains("RecentTransactions", detail, StringComparison.Ordinal);
        Assert.Contains("Suppliers", detail, StringComparison.Ordinal);
        Assert.Contains("CreatePurchaseOrder", detail, StringComparison.Ordinal);
        Assert.Contains("No count or adjustment history has been returned for this material yet.", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("MaterialService does not currently", detail, StringComparison.Ordinal);
        Assert.Contains("material-detail-layout", detailStyles, StringComparison.Ordinal);
        Assert.Contains("material-inventory-status", detailStyles, StringComparison.Ordinal);
        Assert.Contains("material-barcode-svg", detailStyles, StringComparison.Ordinal);
        Assert.Contains("material-audit-entry", detailStyles, StringComparison.Ordinal);
        Assert.Contains("material-supplier-entry", detailStyles, StringComparison.Ordinal);
        Assert.Contains("material-color-picker", detail, StringComparison.Ordinal);
        Assert.Contains("SelectedColorIds", detail, StringComparison.Ordinal);
        Assert.Contains("LoadColorOptionsAsync", detail, StringComparison.Ordinal);
        Assert.Contains("api/v1/materials/reference/colors", detail, StringComparison.Ordinal);
        Assert.Contains("ToggleColorSelection", detail, StringComparison.Ordinal);
        Assert.Contains("ColorIds = _editModel.SelectedColorIds.ToList()", detail, StringComparison.Ordinal);
        Assert.Contains("material-color-option", detailStyles, StringComparison.Ordinal);
        Assert.Contains("public List<Guid>? ColorIds { get; set; }", inventoryDtos, StringComparison.Ordinal);
        Assert.Contains("GetColorsAsync", materialClient, StringComparison.Ordinal);
        Assert.Contains("request.ColorIds ?? current.AvailableColors.Select(color => color.Id).ToList()", materialClient, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"reference/colors\")]", materialsController, StringComparison.Ordinal);
        Assert.Contains("class=\"mlv-table\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("<th>Code</th>", list, StringComparison.Ordinal);
        Assert.DoesNotContain("<td class=\"mlv-mono\">@material.SKU</td>", list, StringComparison.Ordinal);
        Assert.Contains("Stock alerts", list, StringComparison.Ordinal);
        Assert.Contains("NeedsStockAction", list, StringComparison.Ordinal);
        Assert.Contains("Order more", list, StringComparison.Ordinal);
        Assert.Contains("Update stock", list, StringComparison.Ordinal);
        Assert.Contains("material-filter-panel", list, StringComparison.Ordinal);
        Assert.Contains("material-filter-grid", listStyles, StringComparison.Ordinal);
        Assert.Contains("material-search-input", list, StringComparison.Ordinal);
        Assert.Contains("material-process-filter", list, StringComparison.Ordinal);
        Assert.Contains("material-color-filter", list, StringComparison.Ordinal);
        Assert.Contains("material-sort-select", list, StringComparison.Ordinal);
        Assert.Contains("ApplyFiltersAsync", list, StringComparison.Ordinal);
        Assert.Contains("ClearFiltersAsync", list, StringComparison.Ordinal);
        Assert.Contains("LoadFilterOptionsAsync", list, StringComparison.Ordinal);
        Assert.Contains("api/v1/catalog/processes", list, StringComparison.Ordinal);
        Assert.Contains("api/v1/materials/reference/colors", list, StringComparison.Ordinal);
        Assert.Contains("material-stock-alert--critical", listStyles, StringComparison.Ordinal);
        Assert.Contains("material-stock-alert--warning", listStyles, StringComparison.Ordinal);
        Assert.Contains("class=\"mlv-detail-list\"", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-stats-grid", list, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-stats-grid", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-panel", list, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-panel", detail, StringComparison.Ordinal);
        Assert.Contains("PaginationFooter", list, StringComparison.Ordinal);
        Assert.Contains("BuildMaterialsRequestUri", list, StringComparison.Ordinal);
        Assert.Contains("AddQueryParameter(query, \"search\", _search)", list, StringComparison.Ordinal);
        Assert.Contains("manufacturingProcess", list, StringComparison.Ordinal);
        Assert.Contains("sortBy", list, StringComparison.Ordinal);
        Assert.Contains("search, sortBy, sortDesc, minPrice, maxPrice, supplierId, manufacturingProcess, color", materialClient, StringComparison.Ordinal);
        Assert.Contains("[FromQuery] string? search = null", materialsController, StringComparison.Ordinal);
        Assert.Contains("api/v1/materials/{Id}", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemHealthPage_RendersAllServiceHealthFields()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "SystemHealth.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "SystemHealth.razor.css");
        var controller = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "SystemHealthController.cs");
        var probeService = ReadRepoFile("Maliev.Intranet.Bff", "Services", "SystemHealthProbeService.cs");
        var bffProgram = ReadRepoFile("Maliev.Intranet.Bff", "Program.cs");

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
        Assert.Contains("\"ServiceHealthCheck\"", bffProgram, StringComparison.Ordinal);
        Assert.Contains("\"ServiceHealthCheck-standard\"", bffProgram, StringComparison.Ordinal);
        Assert.Contains("CircuitBreaker.MinimumThroughput = int.MaxValue", bffProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("aspire-liveness", probeService, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminReferenceDataPage_UsesExistingReferenceDataServices()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ReferenceData.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ReferenceData.razor.css");
        var admin = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "AdminPage.razor");

        Assert.Contains("@page \"/admin/reference-data\"", page, StringComparison.Ordinal);
        Assert.Contains("IReferenceDataService ReferenceDataService", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.GetCountriesAsync()", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.GetCurrenciesAsync()", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.GetPrimaryCurrencyAsync()", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.GetCountryPageAsync", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.GetCurrencyPageAsync", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.GetRegistryLocationPageAsync", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.CreateCountryAsync", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.UpdateCurrencyAsync", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceDataService.DeleteRegistryLocationAsync", page, StringComparison.Ordinal);
        Assert.Contains("MudSkeleton", page, StringComparison.Ordinal);
        Assert.Contains("ReferenceTableSkeleton", page, StringComparison.Ordinal);
        Assert.Contains("ReferencePager", page, StringComparison.Ordinal);
        Assert.Contains("reference-management-panel", page, StringComparison.Ordinal);
        Assert.Contains("reference-registry-toolbar", page, StringComparison.Ordinal);
        Assert.Contains("reference-editor", page, StringComparison.Ordinal);
        Assert.Contains("Href=\"/admin/reference-data\"", admin, StringComparison.Ordinal);
        Assert.Contains("Href=\"/commerce/catalog\"", admin, StringComparison.Ordinal);
        Assert.Contains("Href=\"/mfg/materials\"", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("api/v1/ReferenceData/locations", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("flex: 1 1 320px", page, StringComparison.Ordinal);

        Assert.Contains("display: grid;", ExtractCssBlock(styles, ".reference-workbench"), StringComparison.Ordinal);
        Assert.Contains("max-width: 78rem;", ExtractCssBlock(styles, ".reference-exchange-panel,"), StringComparison.Ordinal);
        Assert.Contains("flex: 0 1 28rem;", ExtractCssBlock(styles, ".reference-search-field {"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(2, minmax(0, 1fr));", ExtractCssBlock(styles, ".reference-form-grid"), StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto;", ExtractCssBlock(styles, ".reference-table-shell"), StringComparison.Ordinal);
        Assert.Contains("font-size: var(--mud-typography-caption-size);", ExtractCssBlock(styles, ".reference-muted"), StringComparison.Ordinal);
        Assert.Contains("flex-direction: column;", ExtractCssBlock(styles, "@media (max-width: 760px)"), StringComparison.Ordinal);
    }

    [Fact]
    public void PageLoadingStates_UseProgressiveSkeletonsInsteadOfIndeterminateLinearBars()
    {
        var skeleton = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "ProgressiveSkeleton.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "ProgressiveSkeleton.razor.css");
        var clientRoot = FindRepoDirectory("Maliev.Intranet.Client");
        var componentFiles = Directory.EnumerateFiles(clientRoot, "*.razor", SearchOption.AllDirectories);
        var offenders = new List<string>();
        var indeterminateLinearPattern = new Regex("<MudProgressLinear\\b[^>]*Indeterminate\\s*=\\s*\"true\"");

        Assert.Contains("mlv-progressive-skeleton", skeleton, StringComparison.Ordinal);
        Assert.Contains("MudSkeleton", skeleton, StringComparison.Ordinal);
        Assert.Contains("Animation=\"Animation.Wave\"", skeleton, StringComparison.Ordinal);
        Assert.Contains(".mlv-progressive-skeleton", styles, StringComparison.Ordinal);

        foreach (var componentFile in componentFiles)
        {
            var source = File.ReadAllText(componentFile);
            if (indeterminateLinearPattern.IsMatch(source))
            {
                offenders.Add(Path.GetRelativePath(clientRoot, componentFile));
            }
        }

        Assert.True(offenders.Count == 0, $"Indeterminate linear progress bars should use ProgressiveSkeleton or an inline operation indicator:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void CommerceCatalog_SearchToolbarUsesCompactBoundedLayout()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "Catalog.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "Catalog.razor.css");

        Assert.Contains("catalog-product-toolbar", source, StringComparison.Ordinal);
        Assert.Contains("Class=\"catalog-product-search\"", source, StringComparison.Ordinal);
        Assert.Contains("Class=\"catalog-product-filter\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("flex: 1 1 260px", source, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(15rem, 1fr) minmax(13rem, 17rem) max-content max-content;", styles, StringComparison.Ordinal);
        Assert.Contains("width: 100%;", ExtractCssBlock(styles, ".catalog-product-toolbar"), StringComparison.Ordinal);
        Assert.Contains(".catalog-product-toolbar ::deep .mud-input-control", styles, StringComparison.Ordinal);
        Assert.Contains("min-height: 40px;", ExtractCssBlock(styles, ".catalog-product-toolbar ::deep .mud-input.mud-input-outlined"), StringComparison.Ordinal);
        Assert.Contains("min-height: 40px;", ExtractCssBlock(styles, ".catalog-product-toolbar ::deep .mud-button-root"), StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap;", ExtractCssBlock(styles, ".catalog-product-toolbar ::deep .mud-button-root"), StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 620px)", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void MudAlertIconOverride_PreservesTopPaddingForDenseAlerts()
    {
        var styles = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "mudblazor-overrides.css");

        Assert.Contains(".mud-alert-icon", styles, StringComparison.Ordinal);
        Assert.Contains("align-items: flex-start;", styles, StringComparison.Ordinal);
        Assert.Contains("padding: 2px 0 0;", styles, StringComparison.Ordinal);
        Assert.Contains("line-height: 1.45;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void CommerceCatalog_UsesDedicatedListingPageWithBomExport()
    {
        var catalog = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "Catalog.razor");
        var listing = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "CatalogListing.razor");
        var controller = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "CommerceController.cs");
        var commerceDtos = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "CommerceDtos.cs");
        var pdfDtos = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "PdfDataDtos.cs");

        Assert.Contains("Navigation.NavigateTo(\"/commerce/catalog/new\")", catalog, StringComparison.Ordinal);
        Assert.Contains("Navigation.NavigateTo($\"/commerce/catalog/{Uri.EscapeDataString(product.Handle)}\")", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("Edit listing", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("_productForm.Title", catalog, StringComparison.Ordinal);

        Assert.Contains("@page \"/commerce/catalog/new\"", listing, StringComparison.Ordinal);
        Assert.Contains("@page \"/commerce/catalog/{Handle}\"", listing, StringComparison.Ordinal);
        Assert.Contains("Bill of materials", listing, StringComparison.Ordinal);
        Assert.Contains("AddBomItem", listing, StringComparison.Ordinal);
        Assert.Contains("Export BOM PDF", listing, StringComparison.Ordinal);
        Assert.Contains("BomPdfHref", listing, StringComparison.Ordinal);
        Assert.Contains("commerce-bom-card", listing, StringComparison.Ordinal);
        Assert.Contains("commerce-bom-image", listing, StringComparison.Ordinal);
        Assert.Contains("Part no", listing, StringComparison.Ordinal);
        Assert.Contains("Assembly", listing, StringComparison.Ordinal);
        Assert.Contains("Subassembly", listing, StringComparison.Ordinal);
        Assert.Contains("Supplier URL", listing, StringComparison.Ordinal);
        Assert.Contains("Drawing URL", listing, StringComparison.Ordinal);
        Assert.Contains("Lead time", listing, StringComparison.Ordinal);
        Assert.Contains("Sourcing time", listing, StringComparison.Ordinal);
        Assert.Contains("BomSourcingDays", listing, StringComparison.Ordinal);

        Assert.Contains("List<CommerceProductBomItemDto> BomItems", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("List<CommerceProductBomItemMutationRequest> BomItems", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public string? PartNumber", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public string? AssemblyName", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public string? SubassemblyName", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public string? ImageUrl", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public string? DrawingUrl", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public string? SupplierName", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public string? SupplierUrl", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public int? LeadTimeDays", commerceDtos, StringComparison.Ordinal);
        Assert.Contains("public int? SourcingTimeDays", commerceDtos, StringComparison.Ordinal);

        Assert.Contains("PdfDocumentType.CommerceBom", controller, StringComparison.Ordinal);
        Assert.Contains("products/{handle}/bom/pdf", controller, StringComparison.Ordinal);
        Assert.Contains("PartNumber = item.PartNumber", controller, StringComparison.Ordinal);
        Assert.Contains("SourcingTimeDays = CalculateBomItemSourcingDays(item)", controller, StringComparison.Ordinal);
        Assert.Contains("CommerceBomPdfData", pdfDtos, StringComparison.Ordinal);
        Assert.Contains("public int SourcingTimeDays", pdfDtos, StringComparison.Ordinal);
        Assert.Contains("public string? DrawingUrl", pdfDtos, StringComparison.Ordinal);
    }

    [Fact]
    public void CommerceCatalog_UsesDedicatedCollectionsManagementPage()
    {
        var catalog = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "Catalog.razor");
        var collections = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Commerce", "Collections.razor");
        var navigation = ReadRepoFile("Maliev.Intranet.Client", "Layout", "AppNavigation.cs");

        Assert.Contains("Navigation.NavigateTo(\"/commerce/collections\")", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveCollectionAsync", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("_collectionForm", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("Save collection", catalog, StringComparison.Ordinal);

        Assert.Contains("@page \"/commerce/collections\"", collections, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MalievPermissions.Commerce.CollectionsRead)", collections, StringComparison.Ordinal);
        Assert.Contains("CommerceCollectionMutationRequest", collections, StringComparison.Ordinal);
        Assert.Contains("api/v1/commerce/collections", collections, StringComparison.Ordinal);
        Assert.Contains("CollectionsLoadTimeout", collections, StringComparison.Ordinal);
        Assert.Contains("CancellationTokenSource(CollectionsLoadTimeout)", collections, StringComparison.Ordinal);
        Assert.Contains("_collectionsLoadError", collections, StringComparison.Ordinal);
        Assert.Contains("commerce-collections-load-error", collections, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", collections, StringComparison.Ordinal);
        Assert.Contains("OperationCanceledException", collections, StringComparison.Ordinal);
        Assert.Contains("SaveCollectionAsync", collections, StringComparison.Ordinal);
        Assert.Contains("UnpublishCollectionAsync", collections, StringComparison.Ordinal);

        Assert.Contains("new(\"Product collections\", \"commerce/collections\"", navigation, StringComparison.Ordinal);
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
        Assert.Contains("height: 32px;", ExtractCssBlock(source, ".topbar-root ::deep .topbar-global-search"), StringComparison.Ordinal);
        Assert.Contains("height: 32px;", searchBlock, StringComparison.Ordinal);
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
        var topbarRightBlock = ExtractCssBlock(source, ".topbar-right");
        var iconButtonBlock = ExtractCssBlock(source, ".topbar-right ::deep .mud-button-root.mud-icon-button");

        Assert.Contains("gap: 8px;", topbarRightBlock, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 auto;", topbarRightBlock, StringComparison.Ordinal);
        Assert.Contains("height: 32px;", currencyFieldBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", currencyFieldBlock, StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-currency-autocomplete .mud-input-adornment", source, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", iconButtonBlock, StringComparison.Ordinal);
        Assert.Contains("background: transparent;", iconButtonBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", iconButtonBlock, StringComparison.Ordinal);
        Assert.Contains("display: none;", ExtractCssBlock(source, ".topbar-divider"), StringComparison.Ordinal);
        Assert.Contains("margin: 0;", ExtractCssBlock(source, ".topbar-divider"), StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .mud-button-root.mud-icon-button:hover", source, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .mud-button-root.mud-icon-button:focus-visible", source, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-focus-ring) !important;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_TabletAndMobileLayoutKeepsNavSearchAndProfileInBounds()
    {
        var razor = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var mainLayoutStyles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "MainLayout.razor.css");
        var searchStyles = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "GlobalSearchBox.razor.css");
        var wideDesktopStyles = styles[styles.IndexOf("@media (max-width: 1680px)", StringComparison.Ordinal)..];
        var compactNavStyles = styles[styles.IndexOf("@media (max-width: 1600px)", StringComparison.Ordinal)..];
        var compactBottomBarStyles = styles[styles.LastIndexOf("@media (max-width: 1280px)", StringComparison.Ordinal)..];
        var compactLayoutStyles = mainLayoutStyles[mainLayoutStyles.IndexOf("@media (max-width: 1280px)", StringComparison.Ordinal)..];

        Assert.Contains("class=\"topbar-spacer\"", razor, StringComparison.Ordinal);
        Assert.Contains("class=\"topbar-mobile-menu-button\"", razor, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"topbar-mobile-nav\"", razor, StringComparison.Ordinal);
        Assert.Contains("id=\"topbar-mobile-nav\"", razor, StringComparison.Ordinal);
        Assert.Contains("class=\"topbar-mobile-drawer-backdrop\"", razor, StringComparison.Ordinal);
        Assert.Contains("class=\"topbar-mobile-nav-drawer\"", razor, StringComparison.Ordinal);
        Assert.Contains("class=\"topbar-mobile-nav-list\"", razor, StringComparison.Ordinal);
        Assert.Contains("Class=\"topbar-profile-chevron\"", razor, StringComparison.Ordinal);
        Assert.Contains("GetMobileNavClass", razor, StringComparison.Ordinal);
        Assert.Contains("CloseMobileNav", razor, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1680px)", styles, StringComparison.Ordinal);
        Assert.Contains("overflow: visible;", ExtractCssBlock(styles, ".topbar-nav"), StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 auto;", ExtractCssBlock(styles, ".topbar-right"), StringComparison.Ordinal);
        Assert.Contains("margin-left: auto;", ExtractCssBlock(styles, ".topbar-right"), StringComparison.Ordinal);
        Assert.Contains(".topbar-nav-more-trigger", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav-more-popover", ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "mudblazor-overrides.css"), StringComparison.Ordinal);
        Assert.Contains("display: none;", ExtractCssBlock(wideDesktopStyles, ".topbar-profile-info"), StringComparison.Ordinal);
        Assert.Contains("width: 36px;", ExtractCssBlock(wideDesktopStyles, ".topbar-profile {"), StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1280px)", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-profile-info { display: none; }", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-profile ::deep .topbar-profile-chevron", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1600px)", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-mobile-menu-button", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-mobile-drawer-backdrop", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-mobile-nav-drawer", styles, StringComparison.Ordinal);
        var drawerBlock = ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-drawer");
        Assert.Contains("position: fixed;", drawerBlock, StringComparison.Ordinal);
        Assert.Contains("right: 0;", drawerBlock, StringComparison.Ordinal);
        Assert.Contains("left: auto;", drawerBlock, StringComparison.Ordinal);
        Assert.Contains("width: min(390px, calc(100vw - 52px));", drawerBlock, StringComparison.Ordinal);
        Assert.Contains("padding: 10px 12px calc(16px + env(safe-area-inset-bottom));", drawerBlock, StringComparison.Ordinal);
        Assert.Contains("border-radius: 0;", drawerBlock, StringComparison.Ordinal);
        var bottomDockedRootBlock = ExtractCssBlock(compactBottomBarStyles, ".topbar-root");
        Assert.Contains("position: fixed;", bottomDockedRootBlock, StringComparison.Ordinal);
        Assert.Contains("top: auto;", bottomDockedRootBlock, StringComparison.Ordinal);
        Assert.Contains("bottom: 0;", bottomDockedRootBlock, StringComparison.Ordinal);
        Assert.Contains("height: calc(52px + env(safe-area-inset-bottom));", bottomDockedRootBlock, StringComparison.Ordinal);
        Assert.Contains("padding: 0 8px env(safe-area-inset-bottom);", bottomDockedRootBlock, StringComparison.Ordinal);
        Assert.Contains("border-top: 1px solid var(--maliev-border);", bottomDockedRootBlock, StringComparison.Ordinal);
        Assert.Contains("padding-bottom: calc(52px + env(safe-area-inset-bottom));", ExtractCssBlock(compactLayoutStyles, ".body-area"), StringComparison.Ordinal);
        Assert.Contains("inset: 0 0 calc(52px + env(safe-area-inset-bottom)) 0;", ExtractCssBlock(compactBottomBarStyles, ".topbar-mobile-drawer-backdrop"), StringComparison.Ordinal);
        Assert.Contains("bottom: calc(52px + env(safe-area-inset-bottom));", ExtractCssBlock(compactBottomBarStyles, ".topbar-mobile-nav-drawer"), StringComparison.Ordinal);
        Assert.Contains("bottom: calc(100% + 8px);", ExtractCssBlock(compactBottomBarStyles, ".topbar-profile-popover"), StringComparison.Ordinal);
        Assert.Contains(".topbar-root ::deep .topbar-global-search .global-search-panel", compactBottomBarStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 48px;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-drawer-head"), StringComparison.Ordinal);
        Assert.Contains("width: 44px;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-drawer-close"), StringComparison.Ordinal);
        var mobileNavLinkBlock = ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-link");
        Assert.Contains("display: flex;", mobileNavLinkBlock, StringComparison.Ordinal);
        Assert.Contains("align-items: center;", mobileNavLinkBlock, StringComparison.Ordinal);
        Assert.Contains("gap: 10px;", mobileNavLinkBlock, StringComparison.Ordinal);
        Assert.Contains("min-height: 48px;", mobileNavLinkBlock, StringComparison.Ordinal);
        Assert.Contains("padding: 10px 12px;", mobileNavLinkBlock, StringComparison.Ordinal);
        Assert.Contains("display: flex;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-group-title"), StringComparison.Ordinal);
        Assert.Contains("gap: 8px;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-group-title"), StringComparison.Ordinal);
        Assert.Contains("min-height: 34px;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-group-title"), StringComparison.Ordinal);
        Assert.Contains("padding: 8px 12px 4px;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-group-title"), StringComparison.Ordinal);
        Assert.Contains(".topbar-mobile-nav-link ::deep .mud-icon-root", compactNavStyles, StringComparison.Ordinal);
        Assert.Contains(".topbar-mobile-nav-group-title ::deep .mud-icon-root", compactNavStyles, StringComparison.Ordinal);
        var mobileNavLinkIconBlock = ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-link ::deep .mud-icon-root");
        Assert.Contains("width: 24px;", mobileNavLinkIconBlock, StringComparison.Ordinal);
        Assert.Contains("align-self: center;", mobileNavLinkIconBlock, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 24px;", mobileNavLinkIconBlock, StringComparison.Ordinal);
        Assert.Contains(".topbar-mobile-nav-link > span", compactNavStyles, StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-link > span"), StringComparison.Ordinal);
        Assert.Contains("align-items: center;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-link > span"), StringComparison.Ordinal);
        Assert.Contains("min-height: 24px;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-link > span"), StringComparison.Ordinal);
        Assert.Contains("width: 22px;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-nav-group-title ::deep .mud-icon-root"), StringComparison.Ordinal);
        Assert.Contains("flex-wrap: nowrap;", styles, StringComparison.Ordinal);
        Assert.Contains("display: none;", ExtractCssBlock(compactNavStyles, ".topbar-nav"), StringComparison.Ordinal);
        Assert.Contains(".topbar-search", styles, StringComparison.Ordinal);
        Assert.Contains("display: block;", styles, StringComparison.Ordinal);
        Assert.Contains("flex: 1 1 clamp(160px, 32vw, 260px);", styles, StringComparison.Ordinal);
        Assert.Contains("gap: 6px;", ExtractCssBlock(compactNavStyles, ".topbar-right"), StringComparison.Ordinal);
        Assert.Contains("width: min(96px, 100%);", ExtractCssBlock(styles, ".topbar-logo-button ::deep img"), StringComparison.Ordinal);
        Assert.Contains("width: clamp(92px, 17vw, 100px);", ExtractCssBlock(compactNavStyles, ".topbar-logo-button"), StringComparison.Ordinal);
        Assert.DoesNotContain("overflow: hidden;", ExtractCssBlock(compactNavStyles, ".topbar-logo-button"), StringComparison.Ordinal);
        Assert.DoesNotContain("max-width: 86px;", styles, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .topbar-theme-toggle", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 420px)", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("flex-wrap: wrap;", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("flex: 1 1 100%;", styles, StringComparison.Ordinal);
        Assert.DoesNotContain(".topbar-search { display: none; }", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("@media (max-width: 960px)", searchStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("display: none", searchStyles, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_DesktopNavigationCannotOverlapSearchAndUtilityControls()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var appNavigation = ReadRepoFile("Maliev.Intranet.Client", "Layout", "AppNavigation.cs");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var overrides = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "mudblazor-overrides.css");
        var navBlock = ExtractCssBlock(styles, ".topbar-nav");
        var spacerBlock = ExtractCssBlock(styles, ".topbar-spacer");
        var rightBlock = ExtractCssBlock(styles, ".topbar-right");
        var compactNavStyles = styles[styles.IndexOf("@media (max-width: 1600px)", StringComparison.Ordinal)..];

        Assert.Contains("AppNavigation.DesktopGroups", source, StringComparison.Ordinal);
        Assert.Contains("AppNavigation.DesktopOverflowGroups", source, StringComparison.Ordinal);
        Assert.Contains("topbar-nav-more-trigger", source, StringComparison.Ordinal);
        Assert.Contains("topbar-nav-more-section-title", source, StringComparison.Ordinal);
        Assert.Contains("DesktopOverflowGroups", appNavigation, StringComparison.Ordinal);
        Assert.Contains("PrimaryGroups[8]", appNavigation, StringComparison.Ordinal);
        Assert.Contains("flex: 0 1 auto;", navBlock, StringComparison.Ordinal);
        Assert.Contains("min-width: 0;", navBlock, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%;", navBlock, StringComparison.Ordinal);
        Assert.Contains("overflow: visible;", navBlock, StringComparison.Ordinal);
        Assert.Contains("margin-left: auto;", rightBlock, StringComparison.Ordinal);
        Assert.Contains("topbar-nav-more-popover", overrides, StringComparison.Ordinal);
        Assert.Contains("max-height: min(720px, calc(100vh - 78px));", overrides, StringComparison.Ordinal);
        Assert.Contains("display: inline-grid;", ExtractCssBlock(compactNavStyles, ".topbar-mobile-menu-button"), StringComparison.Ordinal);
        Assert.Contains("display: none;", ExtractCssBlock(compactNavStyles, ".topbar-nav"), StringComparison.Ordinal);
        Assert.Contains("display: none;", spacerBlock, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 auto;", spacerBlock, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 auto;", rightBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_QuoteNavigationAction_UsesPrimaryColor()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var quoteBlock = ExtractCssBlock(source, ".topbar-nav ::deep .mud-nav-link.topbar-nav-quote");
        var navBlock = ExtractCssBlock(source, ".topbar-nav");

        Assert.Contains("gap: 8px;", navBlock, StringComparison.Ordinal);
        Assert.Contains("--topbar-quote-accent: var(--mud-palette-primary);", quoteBlock, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", quoteBlock, StringComparison.Ordinal);
        Assert.Contains("background: var(--mud-palette-primary) !important;", quoteBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring)", quoteBlock, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav ::deep .topbar-nav-quote .mud-nav-link", source, StringComparison.Ordinal);
        Assert.DoesNotContain("--maliev-accent", quoteBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("#f97316", quoteBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void HrProfile_PreferencesSignatureFieldsUseScopedTextareaBordersAndSpacing()
    {
        var profile = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Hr", "Profile.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Hr", "Profile.razor.css");
        var profileFormBlock = ExtractCssBlock(styles, ".profile-form");
        var overviewGridBlock = ExtractCssBlock(styles, ".profile-overview-grid");
        var inputBlock = ExtractCssBlock(styles, ".profile-field ::deep input.profile-input");
        var preferenceGridBlock = ExtractCssBlock(styles, ".preference-form-grid");
        var toggleGridBlock = ExtractCssBlock(styles, ".preference-toggle-grid");
        var signatureGridBlock = ExtractCssBlock(styles, ".signature-field-grid");
        var textareaBlock = ExtractCssBlock(styles, ".profile-field ::deep textarea.profile-input");
        var signatureTextareaBlock = ExtractCssBlock(styles, ".profile-field ::deep textarea.preferences-signature");

        Assert.Contains("margin-bottom: 1.65rem;", profileFormBlock, StringComparison.Ordinal);
        Assert.Contains("class=\"mlv-grid profile-overview-grid\"", profile, StringComparison.Ordinal);
        Assert.Contains("row-gap: 1.35rem;", overviewGridBlock, StringComparison.Ordinal);
        Assert.Contains("border: 1px solid var(--maliev-border);", inputBlock, StringComparison.Ordinal);
        Assert.Contains("background: var(--maliev-panel);", inputBlock, StringComparison.Ordinal);
        Assert.Contains("name=\"emailSignature\"", profile, StringComparison.Ordinal);
        Assert.Contains("name=\"shortEmailSignature\"", profile, StringComparison.Ordinal);
        Assert.Contains("BuildDefaultFullEmailSignature", profile, StringComparison.Ordinal);
        Assert.Contains("BuildDefaultShortEmailSignature", profile, StringComparison.Ordinal);
        Assert.Contains("row-gap: 1.45rem;", preferenceGridBlock, StringComparison.Ordinal);
        Assert.Contains("column-gap: 1rem;", preferenceGridBlock, StringComparison.Ordinal);
        Assert.Contains("gap: 1rem;", toggleGridBlock, StringComparison.Ordinal);
        Assert.Contains("margin: 0.25rem 0;", toggleGridBlock, StringComparison.Ordinal);
        Assert.Contains("gap: 1rem;", signatureGridBlock, StringComparison.Ordinal);
        Assert.Contains("border: 1px solid var(--maliev-border);", textareaBlock, StringComparison.Ordinal);
        Assert.Contains("background: var(--maliev-panel);", textareaBlock, StringComparison.Ordinal);
        Assert.Contains("min-height: 8.5rem;", signatureTextareaBlock, StringComparison.Ordinal);
        Assert.Contains(".profile-field ::deep textarea.preferences-signature.short", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerNew_UsesTabbedWorkflowAndIntegratedCompanyLayout()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerNew.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerNew.razor.css");

        Assert.Contains("class=\"customer-create-tabs\"", page, StringComparison.Ordinal);
        Assert.Contains("CreateTab.Company", page, StringComparison.Ordinal);
        Assert.Contains("CreateTab.Documents", page, StringComparison.Ordinal);
        Assert.Contains("class=\"customer-create-tab-panel\"", page, StringComparison.Ordinal);
        Assert.Contains("Class=\"styled-autocomplete company-lookup-input\"", page, StringComparison.Ordinal);
        Assert.Contains("private enum DocumentPanel", page, StringComparison.Ordinal);
        Assert.Contains("private DocumentPanel _activeDocumentPanel = DocumentPanel.Standard;", page, StringComparison.Ordinal);
        Assert.Contains("class=\"documents-workspace\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"document-type-tabs\"", page, StringComparison.Ordinal);
        Assert.Contains("GetDocumentTabClass(DocumentPanel.Standard)", page, StringComparison.Ordinal);
        Assert.Contains("GetDocumentTabClass(DocumentPanel.Nda)", page, StringComparison.Ordinal);
        Assert.Contains("DocumentUploadList(_customerDocumentUploads, DocumentPanel.Standard)", page, StringComparison.Ordinal);
        Assert.Contains("DocumentUploadList(_ndaDocumentUploads, DocumentPanel.Nda)", page, StringComparison.Ordinal);
        Assert.Contains("private string _ndaStatus = \"Draft\";", page, StringComparison.Ordinal);
        Assert.True(
            page.IndexOf("<option value=\"Draft\">Draft</option>", StringComparison.Ordinal)
                < page.IndexOf("<option value=\"Signed\">Signed</option>", StringComparison.Ordinal),
            "Expected Draft to be the first selectable NDA lifecycle status.");
        Assert.Contains("UploadDocumentGroupAsync(files, _ndaDocumentUploads, DocumentCategories.NDA, _ndaStatus, 10)", page, StringComparison.Ordinal);
        Assert.Contains("DocumentSubType = string.Equals(upload.Category, DocumentCategories.NDA", page, StringComparison.Ordinal);
        Assert.Contains("class=\"document-preview-panel persistent\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"document-preview-empty error\"", page, StringComparison.Ordinal);
        Assert.Contains("PreviewUploadedDocumentAsync(drafts.FirstOrDefault(draft => draft.IsUploaded) ?? drafts[0])", page, StringComparison.Ordinal);
        Assert.Contains("public string? SignedUrl { get; set; }", page, StringComparison.Ordinal);
        Assert.Contains("private string? _documentPreviewError;", page, StringComparison.Ordinal);
        Assert.Contains("draft.SignedUrl = upload.SignedUrl;", page, StringComparison.Ordinal);
        Assert.Contains("if (!string.IsNullOrWhiteSpace(document.SignedUrl))", page, StringComparison.Ordinal);
        Assert.Contains("_documentPreviewUrl = document.SignedUrl;", page, StringComparison.Ordinal);
        Assert.Contains("ReadPreviewUrlAsync(document.LinkReference)", page, StringComparison.Ordinal);
        Assert.Contains("download-url?fileReference=", page, StringComparison.Ordinal);
        Assert.DoesNotContain("upload completed.", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"aria-label\", $\"Preview {document.FileName}\"", page, StringComparison.Ordinal);
        Assert.Contains("\"class\", \"document-upload-type\"", page, StringComparison.Ordinal);
        Assert.Contains("GetDocumentTypeLabel(document)", page, StringComparison.Ordinal);
        Assert.Contains("MaxItems=\"8\"", page, StringComparison.Ordinal);
        Assert.Contains("ReturnedItemsCountChanged=\"UpdateCompanySuggestionCount\"", page, StringComparison.Ordinal);
        Assert.Contains("<BeforeItemsTemplate>", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-suggestion-header\"", page, StringComparison.Ordinal);
        Assert.Contains("@GetCompanySuggestionCountText()", page, StringComparison.Ordinal);
        Assert.Contains("<NoItemsTemplate>", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-suggestion-empty\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"suggestion-card company-suggestion-card\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-suggestion-registry\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-suggestion-status\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-suggestion-tax\"", page, StringComparison.Ordinal);
        Assert.Contains("private int _companySuggestionCount;", page, StringComparison.Ordinal);
        Assert.Contains("private void UpdateCompanySuggestionCount(int count)", page, StringComparison.Ordinal);
        Assert.Contains("\"PopoverClass\", \"address-lookup-popover\"", page, StringComparison.Ordinal);
        Assert.Contains("\"ListClass\", \"address-lookup-results\"", page, StringComparison.Ordinal);
        Assert.Contains("\"ListItemClass\", \"address-lookup-result-item\"", page, StringComparison.Ordinal);
        Assert.Contains("\"class\", \"suggestion-card address-suggestion-card\"", page, StringComparison.Ordinal);
        Assert.Contains("\"class\", \"address-suggestion-marker\"", page, StringComparison.Ordinal);
        Assert.Contains("\"class\", \"address-suggestion-meta\"", page, StringComparison.Ordinal);
        Assert.Contains("\"class\", \"address-suggestion-postal-badge\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-core-row\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-address-block\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"company-address-header\"", page, StringComparison.Ordinal);
        Assert.Contains("class=\"customer-classification-default\"", page, StringComparison.Ordinal);
        Assert.Contains("Managed automatically from order history", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Segment\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Tier\"", page, StringComparison.Ordinal);
        Assert.Contains("top: 0;", ExtractCssBlock(styles, ".customer-create-side"), StringComparison.Ordinal);
        Assert.Contains("class=\"customer-create-actions\"", page, StringComparison.Ordinal);
        Assert.Contains("min-height: 0;", ExtractCssBlock(styles, ".customer-create-tab-panel"), StringComparison.Ordinal);
        Assert.Contains("margin-top: 1rem;", ExtractCssBlock(styles, ".customer-create-actions"), StringComparison.Ordinal);
        Assert.DoesNotContain("min-height: 430px;", ExtractCssBlock(styles, ".customer-create-tab-panel"), StringComparison.Ordinal);
        Assert.DoesNotContain("style=\"justify-content:flex-end\"", page, StringComparison.Ordinal);
        Assert.Contains("grid-column: 1 / -1;", ExtractCssBlock(styles, ".customer-classification-default"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1.25fr) minmax(170px, 0.8fr) minmax(132px, 0.5fr) minmax(118px, 0.45fr);", ExtractCssBlock(styles, ".company-core-row"), StringComparison.Ordinal);
        Assert.Contains("max-width: 180px;", ExtractCssBlock(styles, ".company-branch-field"), StringComparison.Ordinal);
        Assert.Contains("height: 44px;", ExtractCssBlock(styles, "::deep .company-lookup-input .mud-input.mud-input-outlined"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) auto;", ExtractCssBlock(styles, "::deep .company-suggestion-header"), StringComparison.Ordinal);
        Assert.Contains("border-left: 3px solid var(--mud-palette-primary);", ExtractCssBlock(styles, "::deep .company-suggestion-card"), StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", ExtractCssBlock(styles, "::deep .company-suggestion-registry"), StringComparison.Ordinal);
        Assert.Contains("border-radius: 999px;", ExtractCssBlock(styles, "::deep .company-suggestion-status"), StringComparison.Ordinal);
        Assert.Contains("font-family: var(--mud-typography-default-family);", ExtractCssBlock(styles, "::deep .company-suggestion-tax"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(280px, 0.88fr) minmax(340px, 1.12fr);", ExtractCssBlock(styles, ".documents-workspace"), StringComparison.Ordinal);
        Assert.Contains("display: flex;", ExtractCssBlock(styles, ".document-type-tabs"), StringComparison.Ordinal);
        Assert.Contains("padding: 0.45rem;", ExtractCssBlock(styles, ".document-upload-list"), StringComparison.Ordinal);
        Assert.Contains("background:", ExtractCssBlock(styles, ".document-upload-list"), StringComparison.Ordinal);
        Assert.Contains("border-left: 3px solid var(--mud-palette-primary);", ExtractCssBlock(styles, ".document-upload-item"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: 2rem minmax(0, 1fr) auto 2rem;", ExtractCssBlock(styles, ".document-upload-item"), StringComparison.Ordinal);
        Assert.Contains("min-height: 58px;", ExtractCssBlock(styles, ".document-upload-item"), StringComparison.Ordinal);
        Assert.Contains("min-height: 340px;", ExtractCssBlock(styles, ".document-preview-panel.persistent"), StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-error);", ExtractCssBlock(styles, ".document-preview-empty.error"), StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", ExtractCssBlock(styles, ".document-upload-size"), StringComparison.Ordinal);
        Assert.Contains("text-transform: uppercase;", ExtractCssBlock(styles, ".document-upload-type"), StringComparison.Ordinal);
        Assert.Contains("justify-content: flex-end;", ExtractCssBlock(styles, ".document-upload-meta"), StringComparison.Ordinal);
        Assert.Contains("width: 2rem;", ExtractCssBlock(styles, ".document-upload-state"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: auto minmax(0, 1fr) auto;", ExtractCssBlock(styles, "::deep .address-suggestion-card"), StringComparison.Ordinal);
        Assert.Contains("border-left: 3px solid var(--mud-palette-secondary);", ExtractCssBlock(styles, "::deep .address-suggestion-card"), StringComparison.Ordinal);
        Assert.Contains("border-radius: 999px;", ExtractCssBlock(styles, "::deep .address-suggestion-marker"), StringComparison.Ordinal);
        Assert.Contains("display: grid;", ExtractCssBlock(styles, "::deep .address-suggestion-meta"), StringComparison.Ordinal);
        Assert.Contains("font-family: var(--mud-typography-default-family);", ExtractCssBlock(styles, "::deep .address-suggestion-postal-badge"), StringComparison.Ordinal);
        Assert.Contains("border-bottom: 1px solid var(--maliev-border);", ExtractCssBlock(styles, ".company-address-header"), StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerNew_AiExtractionTextInput_UsesBalancedEditorSurface()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerNew.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Customers", "CustomerNew.razor.css");
        var responsiveStyles = styles[styles.IndexOf("@media (max-width: 1100px)", StringComparison.Ordinal)..];

        Assert.Contains("<label class=\"ai-text-label\" for=\"customer-extraction-text\">Paste customer text</label>", page, StringComparison.Ordinal);
        Assert.Contains("InputId=\"customer-extraction-text\"", page, StringComparison.Ordinal);
        Assert.Contains("Sizing=\"InputSizing.Auto\"", page, StringComparison.Ordinal);
        Assert.Contains("MaxLines=\"10\"", page, StringComparison.Ordinal);
        Assert.Contains("Margin=\"Margin.None\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Paste customer text\"", page, StringComparison.Ordinal);
        Assert.Contains("role=\"status\" aria-live=\"polite\"", page, StringComparison.Ordinal);
        Assert.Contains("AI extraction in progress", page, StringComparison.Ordinal);
        Assert.Contains("GetExtractionProgressLabel()", page, StringComparison.Ordinal);
        Assert.Contains("GetExtractionProgressDescription()", page, StringComparison.Ordinal);
        Assert.Contains("Improving extraction", page, StringComparison.Ordinal);
        Assert.Contains("Refining autofill", page, StringComparison.Ordinal);
        Assert.Contains("Refinement passes", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Extracting attempt", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Attempt @", page, StringComparison.Ordinal);
        Assert.DoesNotContain("return $\"Attempt", page, StringComparison.Ordinal);
        Assert.Contains("Class=\"ai-processing-spinner\"", page, StringComparison.Ordinal);
        Assert.Contains("_showExtractionSummary = false;", page, StringComparison.Ordinal);
        Assert.Contains("_showExtractionSummary = true;", page, StringComparison.Ordinal);
        Assert.Contains("_extractionFiles.Clear();", page, StringComparison.Ordinal);
        Assert.Contains("DeduplicateExtractedAddresses(extracted.Addresses)", page, StringComparison.Ordinal);
        Assert.Contains("_addresses.Add(CreateEmptyAddress(\"Shipping\"));", page, StringComparison.Ordinal);
        Assert.Contains("_addresses.Add(CreateEmptyAddress(\"Company Billing\"));", page, StringComparison.Ordinal);
        Assert.DoesNotContain("_addresses.Add(CloneAddress(_addresses[0], \"Shipping\"));", page, StringComparison.Ordinal);
        Assert.DoesNotContain("_addresses.Add(CloneAddress(_addresses[0], \"Company Billing\"));", page, StringComparison.Ordinal);
        Assert.Contains("class=\"extraction-summary-dismiss\"", page, StringComparison.Ordinal);
        Assert.Contains("AI extraction review", page, StringComparison.Ordinal);
        Assert.Contains("Needs input", page, StringComparison.Ordinal);
        Assert.Contains("GetExtractionSummaryText(extractedItems.Count, missingItems.Count)", page, StringComparison.Ordinal);
        Assert.Contains("GetExtractedSummaryItems", page, StringComparison.Ordinal);
        Assert.Contains("GetMissingExtractionItems", page, StringComparison.Ordinal);

        Assert.Contains("--ai-extraction-surface-min-height: 132px;", ExtractCssBlock(styles, ".customer-create-page"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(360px, 1fr) minmax(320px, 0.72fr);", ExtractCssBlock(styles, ".ai-intake {"), StringComparison.Ordinal);
        Assert.Contains("gap: 0.75rem;", ExtractCssBlock(styles, ".ai-intake {"), StringComparison.Ordinal);
        Assert.Contains("gap: 0.35rem;", ExtractCssBlock(styles, ".ai-text-column {"), StringComparison.Ordinal);
        Assert.Contains("font-size: var(--mud-typography-caption-size);", ExtractCssBlock(styles, ".ai-text-label"), StringComparison.Ordinal);
        Assert.Contains("padding-top: 1.25rem;", styles, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--ai-extraction-surface-min-height);", ExtractCssBlock(styles, "::deep .ai-text-input .mud-input {"), StringComparison.Ordinal);
        Assert.Contains("margin-top: 0 !important;", ExtractCssBlock(styles, "::deep .ai-text-input textarea.mud-input-slot {"), StringComparison.Ordinal);
        Assert.Contains("padding: 0.95rem 1rem !important;", ExtractCssBlock(styles, "::deep .ai-text-input textarea.mud-input-slot {"), StringComparison.Ordinal);
        Assert.Contains("display: grid;", ExtractCssBlock(styles, ".ai-action-row {"), StringComparison.Ordinal);
        Assert.Contains("width: 100%;", ExtractCssBlock(styles, ".ai-action-row {"), StringComparison.Ordinal);
        Assert.Contains("width: 100%;", ExtractCssBlock(styles, "::deep .ai-extract-action.mlv-button.secondary {"), StringComparison.Ordinal);
        Assert.Contains("min-height: 40px;", ExtractCssBlock(styles, "::deep .ai-extract-action.mlv-button.secondary {"), StringComparison.Ordinal);
        Assert.Contains("justify-content: center;", ExtractCssBlock(styles, "::deep .ai-extract-action.mlv-button.secondary {"), StringComparison.Ordinal);
        Assert.Contains("--ai-dropzone-cyan: #0891b2;", ExtractCssBlock(styles, ".customer-create-page"), StringComparison.Ordinal);
        Assert.Contains("--ai-dropzone-fuchsia: #c026d3;", ExtractCssBlock(styles, ".customer-create-page"), StringComparison.Ordinal);
        Assert.Contains("min-height: var(--ai-extraction-surface-min-height);", ExtractCssBlock(styles, ".document-dropzone.ai-dropzone {"), StringComparison.Ordinal);
        Assert.Contains("height: var(--ai-extraction-surface-min-height);", ExtractCssBlock(styles, ".document-dropzone.ai-dropzone {"), StringComparison.Ordinal);
        Assert.Contains("var(--ai-dropzone-cyan)", ExtractCssBlock(styles, ".document-dropzone.ai-dropzone {"), StringComparison.Ordinal);
        Assert.Contains("var(--ai-dropzone-fuchsia)", ExtractCssBlock(styles, ".document-dropzone.ai-dropzone {"), StringComparison.Ordinal);
        Assert.Contains("background: linear-gradient(135deg, var(--ai-dropzone-cyan), var(--ai-dropzone-violet) 58%, var(--ai-dropzone-fuchsia));", ExtractCssBlock(styles, ".document-dropzone.ai-dropzone .mud-icon-root"), StringComparison.Ordinal);
        Assert.Contains(".document-dropzone.ai-dropzone:hover,", styles, StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", ExtractCssBlock(styles, ".ai-intake-toggle-meta"), StringComparison.Ordinal);
        Assert.Contains("grid-column: 1 / -1;", ExtractCssBlock(styles, ".ai-processing-state {"), StringComparison.Ordinal);
        Assert.Contains("var(--ai-dropzone-violet)", ExtractCssBlock(styles, ".ai-processing-state {"), StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 auto;", ExtractCssBlock(styles, ".ai-processing-spinner"), StringComparison.Ordinal);
        Assert.Contains("margin-top: 0.85rem;", ExtractCssBlock(styles, ".extraction-summary {"), StringComparison.Ordinal);
        Assert.Contains("padding: 0.65rem 0.75rem;", ExtractCssBlock(styles, ".extraction-summary {"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1.1fr) minmax(0, 0.9fr);", ExtractCssBlock(styles, ".extraction-summary-grid"), StringComparison.Ordinal);
        Assert.Contains("width: 1.75rem;", ExtractCssBlock(styles, ".extraction-summary-dismiss"), StringComparison.Ordinal);
        Assert.Contains("padding-top: 0;", ExtractCssBlock(responsiveStyles, ".ai-file-column {"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: 1fr;", ExtractCssBlock(responsiveStyles, ".extraction-summary-grid"), StringComparison.Ordinal);
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
        Assert.Contains("aria-label=\"Sign in to MALIEV\"", source, StringComparison.Ordinal);
        Assert.Contains("class=\"form-title login-title\"", source, StringComparison.Ordinal);
        Assert.Contains("class=\"login-title-logo\"", source, StringComparison.Ordinal);
        Assert.Contains("footer-note-link", source, StringComparison.Ordinal);
        Assert.Contains("MALIEV CO., LTD.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Support", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System Status", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MALIEV INC. ALL RIGHTS RESERVED.", source, StringComparison.Ordinal);
        var loginHeaderBlock = ExtractCssBlock(styles, ".login-header {");
        var loginMainBlock = ExtractCssBlock(styles, ".login-main {");
        var themeToggleBlock = ExtractCssBlock(styles, ".theme-toggle-btn {");
        var bffThemeToggleRootIndex = bffLogin.LastIndexOf(".theme-toggle-btn {", StringComparison.Ordinal);
        Assert.True(bffThemeToggleRootIndex >= 0, "Expected the BFF login page to have a root theme-toggle block.");
        var bffThemeToggleBlock = ExtractCssBlock(bffLogin[bffThemeToggleRootIndex..], ".theme-toggle-btn {");
        var bffLoginMainBlock = ExtractCssBlock(bffLogin, ".login-main {");
        Assert.Contains("background: var(--maliev-bg);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", styles, StringComparison.Ordinal);
        Assert.Contains("border-radius: var(--maliev-radius-md);", styles, StringComparison.Ordinal);
        Assert.Contains("background: var(--mud-palette-primary);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", loginHeaderBlock, StringComparison.Ordinal);
        Assert.Contains("place-items: center;", loginMainBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("align-items: start;", styles, StringComparison.Ordinal);
        Assert.Contains("display: flex;", ExtractCssBlock(styles, ".login-title"), StringComparison.Ordinal);
        Assert.Contains("height: 0.76em;", ExtractCssBlock(styles, ".login-title-logo"), StringComparison.Ordinal);
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
        Assert.Contains("aria-label=\"Sign in to MALIEV\"", bffLogin, StringComparison.Ordinal);
        Assert.Contains("login-title-logo login-title-logo--light", bffLogin, StringComparison.Ordinal);
        Assert.Contains("login-title-logo login-title-logo--dark", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain(">Sign in to MALIEV</h1>", bffLogin, StringComparison.Ordinal);
        Assert.Contains("MALIEV CO., LTD.", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("Support", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("System Status", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("MALIEV INC. ALL RIGHTS RESERVED.", bffLogin, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: auto minmax(0, 1fr) auto;", bffLogin, StringComparison.Ordinal);
        Assert.Contains("--maliev-shadow-card", bffLogin, StringComparison.Ordinal);
        Assert.Contains("background: var(--maliev-bg);", bffLogin, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", ExtractCssBlock(bffLogin, ".login-header {"), StringComparison.Ordinal);
        Assert.Contains("place-items: center;", bffLoginMainBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("align-items: start;", bffLogin, StringComparison.Ordinal);
        Assert.Contains("display: flex;", ExtractCssBlock(bffLogin, ".login-title"), StringComparison.Ordinal);
        Assert.Contains("height: 0.76em;", ExtractCssBlock(bffLogin, ".login-title-logo"), StringComparison.Ordinal);
        Assert.Contains("background: transparent;", bffThemeToggleBlock, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", bffThemeToggleBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("JetBrains+Mono", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("--accent-hue", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("SIGN IN", bffLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("box-shadow: 0 18px", bffLogin, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSchedule_LoadsCurrentTimeAutoScrollHelper()
    {
        var appShell = ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");
        var index = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "index.html");
        var helper = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "js", "production-schedule-board.js");

        Assert.Contains("js/production-schedule-board.js", appShell, StringComparison.Ordinal);
        Assert.Contains("js/production-schedule-board.js", index, StringComparison.Ordinal);
        Assert.Contains("malievProductionSchedule.scrollCurrentTimeIntoView", helper, StringComparison.Ordinal);
        Assert.Contains(".psb-time-heading", helper, StringComparison.Ordinal);
        Assert.Contains("measuredOneHourOffset", helper, StringComparison.Ordinal);
        Assert.Contains("desiredViewportLeft", helper, StringComparison.Ordinal);
        Assert.Contains("board.scrollTo({ left: nextScrollLeft, behavior: 'auto' });", helper, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectNewDelegatedCss_UsesSharedSurfaceTokens()
    {
        var projectNew = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.css");
        var partRow = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartListRow.razor.css");
        var sidebar = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartConfigSidebar.razor.css");
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor.css");
        var bulkTable = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "ProjectPartsBulkTable.razor.css");

        Assert.Contains("border-top: 1px solid var(--maliev-border);", projectNew, StringComparison.Ordinal);
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
        var startDirectories = new List<string>();
        var configuredRoot = Environment.GetEnvironmentVariable("MALIEV_INTRANET_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            startDirectories.Add(configuredRoot);
        }

        startDirectories.Add(GetSourceDirectory());
        startDirectories.Add(AppContext.BaseDirectory);
        startDirectories.Add(Directory.GetCurrentDirectory());

        foreach (var startDirectory in startDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }

    private static string GetSourceDirectory([CallerFilePath] string sourceFile = "") => Path.GetDirectoryName(sourceFile) ?? Directory.GetCurrentDirectory();

    private static string FindRepoDirectory(string directoryName)
    {
        var startDirectories = new List<string>();
        var configuredRoot = Environment.GetEnvironmentVariable("MALIEV_INTRANET_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            startDirectories.Add(configuredRoot);
        }

        startDirectories.Add(GetSourceDirectory());
        startDirectories.Add(AppContext.BaseDirectory);
        startDirectories.Add(Directory.GetCurrentDirectory());

        foreach (var startDirectory in startDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, directoryName);
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException($"Unable to locate {directoryName} from {AppContext.BaseDirectory}.");
    }

    private static string ReadStartTag(string source, int startIndex)
    {
        var inQuote = false;
        for (var index = startIndex; index < source.Length; index++)
        {
            if (source[index] == '"')
            {
                inQuote = !inQuote;
            }
            else if (!inQuote && source[index] == '>')
            {
                return source[startIndex..(index + 1)];
            }
        }

        return source[startIndex..];
    }

    private static bool IsEditableBoundMudInput(string block)
    {
        return HasEditableBinding(block)
            && !HasTrueAttribute(block, "ReadOnly")
            && !HasTrueAttribute(block, "Disabled");
    }

    private static bool IsEditableBoundNativeTextInput(string block)
    {
        return block.Contains("@bind", StringComparison.Ordinal)
            && !HasTrueAttribute(block, "readonly")
            && !HasTrueAttribute(block, "disabled")
            && !HasNonTextInputType(block);
    }

    private static bool IsEditableNativeTextInputWithChangeHandler(string block)
    {
        return block.Contains("value=", StringComparison.Ordinal)
            && block.Contains("@onchange", StringComparison.Ordinal)
            && !block.Contains("@oninput", StringComparison.Ordinal)
            && !HasTrueAttribute(block, "readonly")
            && !HasTrueAttribute(block, "disabled")
            && !HasNonTextInputType(block);
    }

    private static bool HasEditableBinding(string block)
    {
        return block.Contains("@bind-Value", StringComparison.Ordinal)
            || block.Contains("@bind-Text", StringComparison.Ordinal)
            || block.Contains("ValueChanged", StringComparison.Ordinal)
            || block.Contains("TextChanged", StringComparison.Ordinal);
    }

    private static bool HasTrueAttribute(string block, string attributeName)
    {
        return block.Contains($"{attributeName}=\"true\"", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasNonTextInputType(string block)
    {
        return Regex.IsMatch(block, "\\btype\\s*=\\s*\"(?:checkbox|radio|file|date|datetime-local|color)\"", RegexOptions.IgnoreCase);
    }

    private static string FormatInputOffender(string root, string file, string source, int index, string component)
    {
        var line = source[..index].Count(c => c == '\n') + 1;
        return $"{Path.GetRelativePath(root, file)}:{line} {component}";
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
