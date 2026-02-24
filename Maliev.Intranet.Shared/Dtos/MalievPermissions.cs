namespace Maliev.Intranet.Shared;

/// <summary>
/// Central authority for all permissions available in the Maliev platform.
/// </summary>
public static class MalievPermissions
{
    /// <summary>Identity and Access Management permissions.</summary>
    public static class IAM
    {
        /// <summary>Permission to manage IAM resources.</summary>
        public const string Manage = "iam.manage";
        /// <summary>Permission to read IAM resources.</summary>
        public const string Read = "iam.read";
    }

    /// <summary>Alias for consistency with different casings found in code.</summary>
    public static class Iam
    {
        /// <summary>Permission to manage IAM resources.</summary>
        public const string Manage = "iam.manage";
        /// <summary>Permission to read IAM resources.</summary>
        public const string Read = "iam.read";
    }

    /// <summary>Customer management permissions.</summary>
    public static class Customer
    {
        /// <summary>Permission to read customer data.</summary>
        public const string Read = "customer.customers.read";
        /// <summary>Permission to write customer data.</summary>
        public const string Write = "customer.customers.write";

        /// <summary>Customer profile permissions.</summary>
        public static class Profile
        {
            /// <summary>Permission to read customer profiles.</summary>
            public const string Read = "customer.profile.read";
            /// <summary>Permission to write customer profiles.</summary>
            public const string Write = "customer.profile.write";
        }
    }

    /// <summary>Order management permissions.</summary>
    public static class Order
    {
        /// <summary>Permission to read orders.</summary>
        public const string Read = "order.orders.read";
        /// <summary>Permission to write orders.</summary>
        public const string Write = "order.orders.write";
    }

    /// <summary>Registry and reference data permissions.</summary>
    public static class Registry
    {
        /// <summary>Permission to read locations.</summary>
        public const string LocationsRead = "registry.locations.read";
        /// <summary>Permission to read companies.</summary>
        public const string CompaniesRead = "registry.companies.read";
    }

    /// <summary>Supplier management permissions.</summary>
    public static class Supplier
    {
        /// <summary>Permission to read suppliers.</summary>
        public const string Read = "supplier.suppliers.read";
        /// <summary>Permission to write suppliers.</summary>
        public const string Write = "supplier.suppliers.write";
        /// <summary>Permission to delete suppliers.</summary>
        public const string Delete = "supplier.suppliers.delete";
    }

    /// <summary>Quotation management permissions.</summary>
    public static class Quotation
    {
        /// <summary>Permission to read quotations.</summary>
        public const string Read = "quotation.quotations.read";
        /// <summary>Permission to write quotations.</summary>
        public const string Write = "quotation.quotations.write";
        /// <summary>Permission to approve quotations.</summary>
        public const string Approve = "quotation.quotations.approve";
    }

    /// <summary>Material management permissions.</summary>
    public static class Material
    {
        /// <summary>Permission to read materials.</summary>
        public const string Read = "material.materials.read";
        /// <summary>Permission to write materials.</summary>
        public const string Write = "material.materials.write";
    }

    /// <summary>Invoice management permissions.</summary>
    public static class Invoice
    {
        /// <summary>Permission to read invoices.</summary>
        public const string Read = "invoice.invoices.read";
        /// <summary>Permission to write invoices.</summary>
        public const string Write = "invoice.invoices.write";
        /// <summary>Permission to create invoices.</summary>
        public const string Create = "invoice.invoices.write"; // Map to Write
    }

    /// <summary>Employee management permissions.</summary>
    public static class Employee
    {
        /// <summary>Permission to read employee data.</summary>
        public const string Read = "employee.employees.read";
        /// <summary>Permission to write employee data.</summary>
        public const string Write = "employee.employees.write";
    }

    /// <summary>Leave management permissions.</summary>
    public static class Leave
    {
        /// <summary>Permission to read leave balances.</summary>
        public const string Read = "leave.balances.read";
        /// <summary>Permission to create leave requests.</summary>
        public const string Write = "leave.requests.create";
    }

    /// <summary>Career and job posting permissions.</summary>
    public static class Career
    {
        /// <summary>Permission to read job postings.</summary>
        public const string Read = "career.job-postings.read";
        /// <summary>Permission to write job postings.</summary>
        public const string Write = "career.job-postings.write";
        /// <summary>Permission to view career statistics.</summary>
        public const string Stats = "career.reports.read";
    }

    /// <summary>Payment management permissions.</summary>
    public static class Payment
    {
        /// <summary>Permission to read payments.</summary>
        public const string Read = "payment.payments.read";
        /// <summary>Permission to write payments.</summary>
        public const string Write = "payment.payments.write";
    }

    /// <summary>Prediction and AI extraction permissions.</summary>
    public static class Prediction
    {
        /// <summary>Permission to run AI extractions.</summary>
        public const string Extract = "prediction.extract";
    }

    /// <summary>Dashboard permissions.</summary>
    public static class Dashboard
    {
        /// <summary>Permission to view the dashboard.</summary>
        public const string View = "dashboard.view";
    }

    /// <summary>System health and diagnostics permissions.</summary>
    public static class System
    {
        /// <summary>Permission to read system health.</summary>
        public const string HealthRead = "system.health.read";
        /// <summary>Permission to read system diagnostics.</summary>
        public const string DiagnosticsRead = "system.diagnostics.read";
    }

    // New Domains

    /// <summary>Accounting permissions.</summary>
    public static class Accounting
    {
        /// <summary>Permission to read accounts.</summary>
        public const string Read = "accounting.accounts.read";
        /// <summary>Permission to write accounts.</summary>
        public const string Write = "accounting.accounts.write";

        /// <summary>Journal entry permissions.</summary>
        public static class Journal
        {
            /// <summary>Permission to read journal entries.</summary>
            public const string Read = "accounting.journals.read";
            /// <summary>Permission to write journal entries.</summary>
            public const string Write = "accounting.journals.write";
        }
    }

    /// <summary>Receipt management permissions.</summary>
    public static class Receipt
    {
        /// <summary>Permission to read receipts.</summary>
        public const string Read = "receipt.receipts.read";
        /// <summary>Permission to write receipts.</summary>
        public const string Write = "receipt.receipts.write";
    }

    /// <summary>Onboarding permissions.</summary>
    public static class Onboarding
    {
        /// <summary>Permission to read onboarding tasks.</summary>
        public const string Read = "onboarding.tasks.read";
        /// <summary>Permission to write onboarding tasks.</summary>
        public const string Write = "onboarding.tasks.write";
    }

    /// <summary>Purchase order permissions.</summary>
    public static class PurchaseOrder
    {
        /// <summary>Permission to read purchase orders.</summary>
        public const string Read = "purchase-order.orders.read";
        /// <summary>Permission to write purchase orders.</summary>
        public const string Write = "purchase-order.orders.write";
    }

    /// <summary>Compliance permissions.</summary>
    public static class Compliance
    {
        /// <summary>Permission to read compliance records.</summary>
        public const string Read = "compliance.records.read";
        /// <summary>Permission to write compliance records.</summary>
        public const string Write = "compliance.records.write";
    }

    /// <summary>Performance review permissions.</summary>
    public static class Performance
    {
        /// <summary>Permission to read performance reviews.</summary>
        public const string Read = "performance.reviews.read";
        /// <summary>Permission to write performance reviews.</summary>
        public const string Write = "performance.reviews.write";
    }

    /// <summary>Compensation and salary permissions.</summary>
    public static class Compensation
    {
        /// <summary>Permission to read compensation data.</summary>
        public const string Read = "compensation.salaries.read";
        /// <summary>Permission to write compensation data.</summary>
        public const string Write = "compensation.salaries.write";
    }

    /// <summary>Billing note permissions.</summary>
    public static class BillingNote
    {
        /// <summary>Permission to read billing notes.</summary>
        public const string Read = "billing.notes.read";
        /// <summary>Permission to write billing notes.</summary>
        public const string Write = "billing.notes.write";
    }

    /// <summary>User preference permissions.</summary>
    public static class Preference
    {
        /// <summary>Permission to read preferences.</summary>
        public const string Read = "preference.preferences.read";
        /// <summary>Permission to write preferences.</summary>
        public const string Write = "preference.preferences.write";
    }

    /// <summary>Notification permissions.</summary>
    public static class Notification
    {
        /// <summary>Permission to read notification templates.</summary>
        public const string ReadTemplate = "notification.templates.read";
        /// <summary>Permission to write notification templates.</summary>
        public const string WriteTemplate = "notification.templates.write";
    }

    /// <summary>Pricing configuration permissions.</summary>
    public static class Pricing
    {
        /// <summary>Permission to read pricing configuration.</summary>
        public const string ReadConfig = "pricing.configuration.read";
        /// <summary>Permission to write pricing configuration.</summary>
        public const string WriteConfig = "pricing.configuration.write";
    }

    /// <summary>Delivery and shipping permissions.</summary>
    public static class Delivery
    {
        /// <summary>Permission to read delivery notes.</summary>
        public const string Read = "delivery.delivery-notes.read";
        /// <summary>Permission to create delivery notes.</summary>
        public const string Create = "delivery.delivery-notes.create";
        /// <summary>Permission to update delivery notes.</summary>
        public const string Update = "delivery.delivery-notes.update";
        /// <summary>Permission to delete delivery notes.</summary>
        public const string Delete = "delivery.delivery-notes.delete";
        /// <summary>Permission to update delivery status.</summary>
        public const string UpdateStatus = "delivery.delivery-notes.update-status";
        /// <summary>Permission to generate delivery note PDFs.</summary>
        public const string GeneratePdf = "delivery.delivery-notes.generate-pdf";
    }

    /// <summary>Manufacturing job permissions.</summary>
    public static class Job
    {
        /// <summary>Permission to read job data.</summary>
        public const string Read = "job.jobs.read";
        /// <summary>Permission to write/update job data.</summary>
        public const string Write = "job.jobs.write";
        /// <summary>Permission to manage job assignments.</summary>
        public const string Manage = "job.jobs.manage";
    }

    /// <summary>Inventory and stock permissions.</summary>
    public static class Inventory
    {
        /// <summary>Permission to read inventory data.</summary>
        public const string Read = "inventory.stock.read";
        /// <summary>Permission to write/update inventory data.</summary>
        public const string Write = "inventory.stock.write";
        /// <summary>Permission to read batch data.</summary>
        public const string BatchesRead = "inventory.batches.read";
        /// <summary>Permission to write/update batch data.</summary>
        public const string BatchesWrite = "inventory.batches.write";
    }
}
