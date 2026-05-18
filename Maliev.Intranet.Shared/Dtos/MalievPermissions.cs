namespace Maliev.Intranet.Shared;

/// <summary>
/// Central authority for all permissions available in the Maliev platform.
/// </summary>
public static class MalievPermissions
{
    /// <summary>
    /// Permissions for Identity and Access Management operations.
    /// </summary>
    public static class IAM
    {
        /// <summary>Permission to manage IAM policies, users, and roles.</summary>
        public const string Manage = "iam.principals.manage";
        /// <summary>Permission to read IAM information.</summary>
        public const string Read = "iam.principals.read";

        /// <summary>
        /// Permissions for IAM principals.
        /// </summary>
        public static class Principals
        {
            /// <summary>Permission to create a principal.</summary>
            public const string Create = "iam.principals.create";
            /// <summary>Permission to read a principal.</summary>
            public const string Read = "iam.principals.read";
            /// <summary>Permission to update a principal.</summary>
            public const string Update = "iam.principals.update";
            /// <summary>Permission to delete a principal.</summary>
            public const string Delete = "iam.principals.delete";
            /// <summary>Permission to list principals.</summary>
            public const string List = "iam.principals.list";
        }

        /// <summary>
        /// Permissions for IAM roles.
        /// </summary>
        public static class Roles
        {
            /// <summary>Permission to create a role.</summary>
            public const string Create = "iam.roles.create";
            /// <summary>Permission to read a role.</summary>
            public const string Read = "iam.roles.read";
            /// <summary>Permission to update a role.</summary>
            public const string Update = "iam.roles.update";
            /// <summary>Permission to delete a role.</summary>
            public const string Delete = "iam.roles.delete";
            /// <summary>Permission to list roles.</summary>
            public const string List = "iam.roles.list";
        }

        /// <summary>
        /// Permissions for IAM permission catalog entries.
        /// </summary>
        public static class Permissions
        {
            /// <summary>Permission to create a permission.</summary>
            public const string Create = "iam.permissions.create";
            /// <summary>Permission to read a permission.</summary>
            public const string Read = "iam.permissions.read";
            /// <summary>Permission to update a permission.</summary>
            public const string Update = "iam.permissions.update";
            /// <summary>Permission to delete a permission.</summary>
            public const string Delete = "iam.permissions.delete";
            /// <summary>Permission to list permissions.</summary>
            public const string List = "iam.permissions.list";
        }

        /// <summary>
        /// Permissions for IAM role bindings.
        /// </summary>
        public static class Bindings
        {
            /// <summary>Permission to create a role binding.</summary>
            public const string Create = "iam.bindings.create";
            /// <summary>Permission to read a role binding.</summary>
            public const string Read = "iam.bindings.read";
            /// <summary>Permission to delete a role binding.</summary>
            public const string Delete = "iam.bindings.delete";
            /// <summary>Permission to list role bindings.</summary>
            public const string List = "iam.bindings.list";
        }

        /// <summary>
        /// Permissions for IAM audit records.
        /// </summary>
        public static class Audit
        {
            /// <summary>Permission to read audit records.</summary>
            public const string Read = "iam.audit.read";
            /// <summary>Permission to list audit records.</summary>
            public const string List = "iam.audit.list";
        }
    }

    /// <summary>
    /// Permissions for Identity and Access Management operations (casing-insensitive alias).
    /// </summary>
    public static class Iam
    {
        /// <summary>Permission to manage IAM policies, users, and roles.</summary>
        public const string Manage = "iam.principals.manage";
        /// <summary>Permission to read IAM information.</summary>
        public const string Read = "iam.principals.read";
        /// <summary>Permission to create IAM principals.</summary>
        public const string PrincipalsCreate = IAM.Principals.Create;
        /// <summary>Permission to read IAM principals.</summary>
        public const string PrincipalsRead = IAM.Principals.Read;
        /// <summary>Permission to update IAM principals.</summary>
        public const string PrincipalsUpdate = IAM.Principals.Update;
        /// <summary>Permission to delete IAM principals.</summary>
        public const string PrincipalsDelete = IAM.Principals.Delete;
        /// <summary>Permission to list IAM principals.</summary>
        public const string PrincipalsList = IAM.Principals.List;
        /// <summary>Permission to list IAM roles.</summary>
        public const string RolesList = IAM.Roles.List;
        /// <summary>Permission to list IAM permissions.</summary>
        public const string PermissionsList = IAM.Permissions.List;
        /// <summary>Permission to create IAM role bindings.</summary>
        public const string BindingsCreate = IAM.Bindings.Create;
        /// <summary>Permission to list IAM role bindings.</summary>
        public const string BindingsList = IAM.Bindings.List;
        /// <summary>Permission to delete IAM role bindings.</summary>
        public const string BindingsDelete = IAM.Bindings.Delete;
    }

    /// <summary>
    /// Permissions for BFF authentication session endpoints.
    /// </summary>
    public static class Auth
    {
        /// <summary>Permission to read the current authenticated session.</summary>
        public const string SessionsRead = "auth.sessions.read";
    }

    /// <summary>
    /// Permissions for chat sessions and messages.
    /// </summary>
    public static class Chat
    {
        /// <summary>Permission to create chat sessions and messages.</summary>
        public const string SessionsCreate = "chat.sessions.create";
        /// <summary>Permission to read chat session history.</summary>
        public const string SessionsRead = "chat.sessions.read";
        /// <summary>Permission to read chatbot system instructions and skill prompts.</summary>
        public const string InstructionsRead = "chatbot.instructions.read";
        /// <summary>Permission to create or update chatbot system instructions and skill prompts.</summary>
        public const string InstructionsWrite = "chatbot.instructions.write";
    }

    /// <summary>
    /// Permissions for customer management and profile operations.
    /// </summary>
    public static class Customer
    {
        /// <summary>Permission to read individual customer records.</summary>
        public const string Read = "customer.customers.read";
        /// <summary>Permission to list and search customers.</summary>
        public const string List = "customer.customers.list";
        /// <summary>Permission to create or update customer records.</summary>
        public const string Write = "customer.customers.write";

        /// <summary>
        /// Permissions for customer profile specific operations.
        /// </summary>
        public static class Profile
        {
            /// <summary>Permission to read customer profile details.</summary>
            public const string Read = "customer.profile.read";
            /// <summary>Permission to update customer profile information.</summary>
            public const string Write = "customer.profile.write";
        }
    }

    /// <summary>
    /// Permissions for order lifecycle management.
    /// </summary>
    public static class Order
    {
        /// <summary>Permission to view order details and history.</summary>
        public const string Read = "order.orders.read";
        /// <summary>Permission to create, update, or cancel orders.</summary>
        public const string Write = "order.orders.write";
    }

    /// <summary>
    /// Permissions for accessing the central registry of locations and companies.
    /// </summary>
    public static class Registry
    {
        /// <summary>Permission to view registered facility and site locations.</summary>
        public const string LocationsRead = "registry.locations.read";
        /// <summary>Permission to create registered facility and site locations.</summary>
        public const string LocationsCreate = "registry.locations.create";
        /// <summary>Permission to update registered facility and site locations.</summary>
        public const string LocationsUpdate = "registry.locations.update";
        /// <summary>Permission to delete registered facility and site locations.</summary>
        public const string LocationsDelete = "registry.locations.delete";
        /// <summary>Permission to view registered external companies and partners.</summary>
        public const string CompaniesRead = "registry.companies.read";
    }

    /// <summary>
    /// Permissions for country catalog management.
    /// </summary>
    public static class Country
    {
        /// <summary>Permission to read country details.</summary>
        public const string CountriesRead = "country.countries.read";
        /// <summary>Permission to list country reference data.</summary>
        public const string CountriesList = "country.countries.list";
        /// <summary>Permission to search country reference data.</summary>
        public const string CountriesSearch = "country.countries.search";
        /// <summary>Permission to create country reference records.</summary>
        public const string CountriesCreate = "country.countries.create";
        /// <summary>Permission to update country reference records.</summary>
        public const string CountriesUpdate = "country.countries.update";
        /// <summary>Permission to soft delete country reference records.</summary>
        public const string CountriesDelete = "country.countries.delete";
        /// <summary>Permission to restore country reference records.</summary>
        public const string CountriesRestore = "country.countries.restore";
    }

    /// <summary>
    /// Permissions for currency catalog and exchange-rate lookups.
    /// </summary>
    public static class Currency
    {
        /// <summary>Permission to view currency metadata.</summary>
        public const string CurrenciesRead = "currency.currencies.read";
        /// <summary>Permission to create currency metadata.</summary>
        public const string CurrenciesCreate = "currency.currencies.create";
        /// <summary>Permission to update currency metadata.</summary>
        public const string CurrenciesUpdate = "currency.currencies.update";
        /// <summary>Permission to delete currency metadata.</summary>
        public const string CurrenciesDelete = "currency.currencies.delete";
        /// <summary>Permission to activate or deactivate currency metadata.</summary>
        public const string CurrenciesActivate = "currency.currencies.activate";
        /// <summary>Permission to view exchange rates.</summary>
        public const string RatesRead = "currency.rates.read";
    }

    /// <summary>
    /// Permissions for managing external supplier relationships.
    /// </summary>
    public static class Supplier
    {
        /// <summary>Permission to view supplier profiles and ratings.</summary>
        public const string Read = "supplier.suppliers.read";
        /// <summary>Permission to onboard or update suppliers.</summary>
        public const string Write = "supplier.suppliers.write";
        /// <summary>Permission to remove supplier records from the system.</summary>
        public const string Delete = "supplier.suppliers.delete";
    }

    /// <summary>
    /// Permissions for quotation and proposal management.
    /// </summary>
    public static class Quotation
    {
        /// <summary>Permission to view quotations and pricing proposals.</summary>
        public const string Read = "quotation.quotations.read";
        /// <summary>Permission to create or edit quotations.</summary>
        public const string Write = "quotation.quotations.write";
        /// <summary>Permission to formally approve quotations for conversion to orders.</summary>
        public const string Approve = "quotation.quotations.approve";
    }

    /// <summary>
    /// Permissions for raw material and inventory catalog management.
    /// </summary>
    public static class Material
    {
        /// <summary>Permission to view material specifications and stock levels.</summary>
        public const string Read = "material.materials.read";
        /// <summary>Permission to define new materials.</summary>
        public const string Create = "material.materials.create";
        /// <summary>Permission to update material specifications or inventory details.</summary>
        public const string Update = "material.materials.update";
        /// <summary>Permission to remove material records.</summary>
        public const string Delete = "material.materials.delete";
        /// <summary>Legacy alias for material update permissions.</summary>
        public const string Write = Update;
    }

    /// <summary>
    /// Permissions for financial invoice processing.
    /// </summary>
    public static class Invoice
    {
        /// <summary>Permission to view sales and purchase invoices.</summary>
        public const string Read = "invoice.invoices.read";
        /// <summary>Permission to update existing invoice records.</summary>
        public const string Write = "invoice.invoices.update";
        /// <summary>Permission to generate new invoices.</summary>
        public const string Create = "invoice.invoices.create";
        /// <summary>Permission to finalize draft invoices.</summary>
        public const string Finalize = "invoice.invoices.finalize";
        /// <summary>Permission to void or cancel invoices.</summary>
        public const string Void = "invoice.invoices.void";
        /// <summary>Permission to split invoices.</summary>
        public const string Split = "invoice.splits.create";
        /// <summary>Permission to upload files to invoices.</summary>
        public const string FileUpload = "invoice.files.upload";
    }

    /// <summary>
    /// Permissions for employee directory and HR record management.
    /// </summary>
    public static class Employee
    {
        /// <summary>Permission to view employee profiles and organization structure.</summary>
        public const string Read = "employee.employees.read";
        /// <summary>Permission to manage employee data and assignments.</summary>
        public const string Write = "employee.employees.write";
        /// <summary>Permission to view employee self-service profile records.</summary>
        public const string ProfileRead = "employee.profiles.read";
        /// <summary>Permission to update employee self-service profile fields.</summary>
        public const string ProfileUpdate = "employee.profiles.update";
    }

    /// <summary>
    /// Permissions for leave management and balance tracking.
    /// </summary>
    public static class Leave
    {
        /// <summary>Permission to view personal or team leave balances.</summary>
        public const string BalanceRead = "leave.balances.read";
        /// <summary>Permission to view personal or team leave requests.</summary>
        public const string RequestsRead = "leave.requests.read";
        /// <summary>Legacy alias for leave balance read access.</summary>
        public const string Read = BalanceRead;
        /// <summary>Permission to submit new leave requests.</summary>
        public const string Write = "leave.requests.create";
        /// <summary>Permission to approve or reject leave requests assigned to the employee.</summary>
        public const string Approve = "leave.requests.approve";
    }

    /// <summary>
    /// Permissions for recruitment, job postings, and career reports.
    /// </summary>
    public static class Career
    {
        /// <summary>Permission to view active job vacancies.</summary>
        public const string Read = "career.job-postings.read";
        /// <summary>Permission to manage job postings and applicant data.</summary>
        public const string Write = "career.job-postings.write";
        /// <summary>Permission to view recruitment and career advancement statistics.</summary>
        public const string Stats = "career.reports.read";
    }

    /// <summary>
    /// Permissions for payment processing and transaction history.
    /// </summary>
    public static class Payment
    {
        /// <summary>Permission to view payment transactions and status.</summary>
        public const string Read = "payment.payments.read";
        /// <summary>Permission to initiate or process payments.</summary>
        public const string Write = "payment.payments.write";
    }

    /// <summary>
    /// Permissions for Commerce storefront catalog and shop order management.
    /// </summary>
    public static class Commerce
    {
        /// <summary>Permission to view Commerce products.</summary>
        public const string ProductsRead = "commerce.products.read";
        /// <summary>Permission to create Commerce products.</summary>
        public const string ProductsCreate = "commerce.products.create";
        /// <summary>Permission to update Commerce products.</summary>
        public const string ProductsUpdate = "commerce.products.update";
        /// <summary>Permission to archive Commerce products.</summary>
        public const string ProductsDelete = "commerce.products.delete";
        /// <summary>Permission to view Commerce collections.</summary>
        public const string CollectionsRead = "commerce.collections.read";
        /// <summary>Permission to create Commerce collections.</summary>
        public const string CollectionsCreate = "commerce.collections.create";
        /// <summary>Permission to update Commerce collections.</summary>
        public const string CollectionsUpdate = "commerce.collections.update";
        /// <summary>Permission to unpublish Commerce collections.</summary>
        public const string CollectionsDelete = "commerce.collections.delete";
    }

    /// <summary>
    /// Permissions for MALIEV public website content operations.
    /// </summary>
    public static class WebContent
    {
        /// <summary>Permission to view public website content operations.</summary>
        public const string Read = "web.contents.read";
        /// <summary>Permission to update public website content operations.</summary>
        public const string Write = "web.contents.update";
    }

    /// <summary>
    /// Permissions for AI-driven data extraction and analysis services.
    /// </summary>
    public static class Prediction
    {
        /// <summary>Permission to trigger automated data extraction from documents.</summary>
        public const string Extract = "prediction.extractions.extract";
    }

    /// <summary>
    /// Permissions for viewing management dashboards.
    /// </summary>
    public static class Dashboard
    {
        /// <summary>Permission to access and view system-wide dashboards.</summary>
        public const string View = "dashboard.view";
    }

    /// <summary>
    /// Permissions for system health monitoring and diagnostic tools.
    /// </summary>
    public static class System
    {
        /// <summary>Permission to view system health status.</summary>
        public const string HealthRead = "system.health.read";
        /// <summary>Permission to access system diagnostic logs and metrics.</summary>
        public const string DiagnosticsRead = "system.diagnostics.read";
    }

    /// <summary>
    /// Permissions for global search.
    /// </summary>
    public static class Search
    {
        /// <summary>Permission to query the global search index.</summary>
        public const string Read = "search.documents.read";
    }

    // New Domains

    /// <summary>
    /// Permissions for general accounting and ledger operations.
    /// </summary>
    public static class Accounting
    {
        /// <summary>Permission to view chart of accounts and balances.</summary>
        public const string Read = "accounting.accounts.read";
        /// <summary>Permission to update accounts and financial configurations.</summary>
        public const string Write = "accounting.accounts.update";
        /// <summary>Permission to open accounting periods.</summary>
        public const string PeriodsOpen = "accounting.periods.open";
        /// <summary>Permission to close accounting periods.</summary>
        public const string PeriodsClose = "accounting.periods.close";
        /// <summary>Permission to reopen accounting periods.</summary>
        public const string PeriodsReopen = "accounting.periods.reopen";
        /// <summary>Permission to run reconciliation.</summary>
        public const string ReconciliationRun = "accounting.reconciliation.run";

        /// <summary>
        /// Permissions for accounting reports.
        /// </summary>
        public static class Reports
        {
            /// <summary>Permission to export accounting reports.</summary>
            public const string Export = "accounting.reports.export";
        }

        /// <summary>
        /// Permissions for journal entry management.
        /// </summary>
        public static class Journal
        {
            /// <summary>Permission to view accounting journal entries.</summary>
            public const string Read = "accounting.journal-entries.read";
            /// <summary>Permission to create journal entries.</summary>
            public const string Create = "accounting.journal-entries.create";
            /// <summary>Permission to update journal entries.</summary>
            public const string Write = "accounting.journal-entries.update";
            /// <summary>Permission to post journal entries.</summary>
            public const string Post = "accounting.journal-entries.post";
        }
    }

    /// <summary>
    /// Permissions for managing payment receipts and evidence.
    /// </summary>
    public static class Receipt
    {
        /// <summary>Permission to view payment receipts.</summary>
        public const string Read = "receipt.receipts.read";
        /// <summary>Permission to upload or update receipt documentation.</summary>
        public const string Write = "receipt.receipts.write";
    }

    /// <summary>
    /// Permissions for employee onboarding tasks and workflows.
    /// </summary>
    public static class Onboarding
    {
        /// <summary>Permission to view assigned onboarding tasks.</summary>
        public const string Read = "onboarding.tasks.read";
        /// <summary>Permission to manage onboarding checklists and task status.</summary>
        public const string Write = "onboarding.tasks.write";
    }

    /// <summary>
    /// Permissions for procurement via purchase orders.
    /// </summary>
    public static class PurchaseOrder
    {
        /// <summary>Permission to view purchase order details.</summary>
        public const string Read = "purchase-order.orders.read";
        /// <summary>Permission to update purchase orders.</summary>
        public const string Write = "purchase-order.orders.update";
        /// <summary>Permission to create purchase orders.</summary>
        public const string Create = "purchase-order.orders.create";
        /// <summary>Permission to approve purchase orders.</summary>
        public const string Approve = "purchase-order.orders.approve";
        /// <summary>Permission to send purchase orders to suppliers.</summary>
        public const string Send = "purchase-order.orders.send";
        /// <summary>Permission to receive purchase order items.</summary>
        public const string Receive = "purchase-order.orders.receive";
        /// <summary>Permission to cancel purchase orders.</summary>
        public const string Cancel = "purchase-order.orders.cancel";
        /// <summary>Permission to upload files to purchase orders.</summary>
        public const string FileUpload = "purchase-order.files.upload";
    }

    /// <summary>
    /// Permissions for regulatory compliance and audit record management.
    /// </summary>
    public static class Compliance
    {
        /// <summary>Permission to view compliance records and audit trails.</summary>
        public const string Read = "compliance.records.read";
        /// <summary>Permission to log compliance activities and updates.</summary>
        public const string Write = "compliance.records.write";
    }

    /// <summary>
    /// Permissions for employee performance reviews and appraisals.
    /// </summary>
    public static class Performance
    {
        /// <summary>Permission to view performance review outcomes.</summary>
        public const string Read = "performance.reviews.read";
        /// <summary>Permission to conduct appraisals and manage review cycles.</summary>
        public const string Write = "performance.reviews.write";
    }

    /// <summary>
    /// Permissions for managing employee compensation, salaries, and benefits.
    /// </summary>
    public static class Compensation
    {
        /// <summary>Permission to view salary and benefit information.</summary>
        public const string Read = "compensation.salaries.read";
        /// <summary>Permission to update compensation packages and payroll data.</summary>
        public const string Write = "compensation.salaries.write";
    }

    /// <summary>
    /// Permissions for managing billing notes and associated documentation.
    /// </summary>
    public static class BillingNote
    {
        /// <summary>Permission to view billing notes.</summary>
        public const string Read = "billing.notes.read";
        /// <summary>Permission to create or update billing-related annotations.</summary>
        public const string Write = "billing.notes.write";
    }

    /// <summary>
    /// Permissions for managing platform-wide and user-specific preferences.
    /// </summary>
    public static class Preference
    {
        /// <summary>Permission to read system or user settings.</summary>
        public const string Read = "notification.preferences.read";
        /// <summary>Permission to update configuration and preference values.</summary>
        public const string Write = "notification.preferences.update";
    }

    /// <summary>
    /// Permissions for notification template management.
    /// </summary>
    public static class Notification
    {
        /// <summary>Permission to view email and message templates.</summary>
        public const string ReadTemplate = "notification.templates.read";
        /// <summary>Permission to create notification templates.</summary>
        public const string CreateTemplate = "notification.templates.create";
        /// <summary>Permission to update notification templates.</summary>
        public const string UpdateTemplate = "notification.templates.update";
        /// <summary>Permission to delete notification templates.</summary>
        public const string DeleteTemplate = "notification.templates.delete";
        /// <summary>Permission to manage notification templates.</summary>
        public const string WriteTemplate = "notification.templates.manage";
        /// <summary>Permission to view notification delivery logs.</summary>
        public const string ReadLogs = "notification.logs.read";
    }

    /// <summary>
    /// Permissions for core pricing engine configuration.
    /// </summary>
    public static class Pricing
    {
        /// <summary>Permission to view pricing rules and configurations.</summary>
        public const string ReadConfig = "pricing.configuration.read";
        /// <summary>Permission to manage pricing strategies and parameter values.</summary>
        public const string WriteConfig = "pricing.configuration.write";
    }

    /// <summary>
    /// Permissions for logistics and delivery note management.
    /// </summary>
    public static class Delivery
    {
        /// <summary>Permission to view delivery notes and shipping status.</summary>
        public const string Read = "delivery.deliverynotes.read";
        /// <summary>Permission to create new delivery documentation.</summary>
        public const string Create = "delivery.deliverynotes.create";
        /// <summary>Permission to update existing delivery details.</summary>
        public const string Update = "delivery.deliverynotes.update";
        /// <summary>Permission to delete delivery records.</summary>
        public const string Delete = "delivery.deliverynotes.delete";
        /// <summary>Permission to modify the lifecycle status of a delivery.</summary>
        public const string UpdateStatus = "delivery.deliverynotes.update";
        /// <summary>Permission to generate PDF representations of delivery notes.</summary>
        public const string GeneratePdf = "delivery.deliverynotes.generate";
    }

    /// <summary>
    /// Permissions for managing physical facilities, equipment, and maintenance.
    /// </summary>
    public static class Facility
    {
        /// <summary>Read equipment information, notes, loans, maintenance logs and attachments.</summary>
        public const string Read = "facility.equipments.read";

        /// <summary>Create and update equipment, add notes, manage loans, log maintenance and manage attachments.</summary>
        public const string Write = "facility.equipments.write";

        /// <summary>Delete equipment and manage equipment lifecycle (decommission).</summary>
        public const string Manage = "facility.equipments.manage";

        /// <summary>Approve or reject equipment loan requests.</summary>
        public const string LoansApprove = "facility.loans.approve";
    }

    /// <summary>
    /// Permissions for project management, parts tracking, and pricing.
    /// </summary>
    public static class Project
    {
        /// <summary>View projects, parts, pricing, and quotation status.</summary>
        public const string Read = "project.projects.read";

        /// <summary>Create and update projects, add parts, confirm prices, and generate quotations.</summary>
        public const string Write = "project.projects.write";
    }

    /// <summary>
    /// Permissions for manufacturing job management.
    /// </summary>
    public static class Job
    {
        /// <summary>View jobs in production queue, stats, and QR codes.</summary>
        public const string Read = "job.jobs.read";

        /// <summary>Update job status, assign machines, and modify job details.</summary>
        public const string Write = "job.jobs.write";
    }
}
