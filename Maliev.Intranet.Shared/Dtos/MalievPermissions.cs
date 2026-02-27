namespace Maliev.Intranet.Shared;

/// <summary>
/// Central authority for all permissions available in the Maliev platform.
/// </summary>
public static class MalievPermissions
{
    public static class IAM
    {
        public const string Manage = "iam.manage";
        public const string Read = "iam.read";
    }

    // Alias for consistency with different casings found in code
    public static class Iam
    {
        public const string Manage = "iam.manage";
        public const string Read = "iam.read";
    }

    public static class Customer
    {
        public const string Read = "customer.customers.read";
        public const string Write = "customer.customers.write";

        public static class Profile
        {
            public const string Read = "customer.profile.read";
            public const string Write = "customer.profile.write";
        }
    }

    public static class Order
    {
        public const string Read = "order.orders.read";
        public const string Write = "order.orders.write";
    }

    public static class Registry
    {
        public const string LocationsRead = "registry.locations.read";
        public const string CompaniesRead = "registry.companies.read";
    }

    public static class Supplier
    {
        public const string Read = "supplier.suppliers.read";
        public const string Write = "supplier.suppliers.write";
        public const string Delete = "supplier.suppliers.delete";
    }

    public static class Quotation
    {
        public const string Read = "quotation.quotations.read";
        public const string Write = "quotation.quotations.write";
        public const string Approve = "quotation.quotations.approve";
    }

    public static class Material
    {
        public const string Read = "material.materials.read";
        public const string Write = "material.materials.write";
    }

    public static class Invoice
    {
        public const string Read = "invoice.invoices.read";
        public const string Write = "invoice.invoices.write";
        public const string Create = "invoice.invoices.write"; // Map to Write
    }

    public static class Employee
    {
        public const string Read = "employee.employees.read";
        public const string Write = "employee.employees.write";
    }

    public static class Leave
    {
        public const string Read = "leave.balances.read";
        public const string Write = "leave.requests.create";
    }

    public static class Career
    {
        public const string Read = "career.job-postings.read";
        public const string Write = "career.job-postings.write";
        public const string Stats = "career.reports.read";
    }

    public static class Payment
    {
        public const string Read = "payment.payments.read";
        public const string Write = "payment.payments.write";
    }

    public static class Prediction
    {
        public const string Extract = "prediction.extract";
    }

    public static class Dashboard
    {
        public const string View = "dashboard.view";
    }

    public static class System
    {
        public const string HealthRead = "system.health.read";
        public const string DiagnosticsRead = "system.diagnostics.read";
    }

    // New Domains

    public static class Accounting
    {
        public const string Read = "accounting.accounts.read";
        public const string Write = "accounting.accounts.write";

        public static class Journal
        {
            public const string Read = "accounting.journals.read";
            public const string Write = "accounting.journals.write";
        }
    }

    public static class Receipt
    {
        public const string Read = "receipt.receipts.read";
        public const string Write = "receipt.receipts.write";
    }

    public static class Onboarding
    {
        public const string Read = "onboarding.tasks.read";
        public const string Write = "onboarding.tasks.write";
    }

    public static class PurchaseOrder
    {
        public const string Read = "purchase-order.orders.read";
        public const string Write = "purchase-order.orders.write";
    }

    public static class Compliance
    {
        public const string Read = "compliance.records.read";
        public const string Write = "compliance.records.write";
    }

    public static class Performance
    {
        public const string Read = "performance.reviews.read";
        public const string Write = "performance.reviews.write";
    }

    public static class Compensation
    {
        public const string Read = "compensation.salaries.read";
        public const string Write = "compensation.salaries.write";
    }

    public static class BillingNote
    {
        public const string Read = "billing.notes.read";
        public const string Write = "billing.notes.write";
    }

    public static class Preference
    {
        public const string Read = "preference.preferences.read";
        public const string Write = "preference.preferences.write";
    }

    public static class Notification
    {
        public const string ReadTemplate = "notification.templates.read";
        public const string WriteTemplate = "notification.templates.write";
    }

    public static class Pricing
    {
        public const string ReadConfig = "pricing.configuration.read";
        public const string WriteConfig = "pricing.configuration.write";
    }

    public static class Delivery
    {
        public const string Read = "delivery.delivery-notes.read";
        public const string Create = "delivery.delivery-notes.create";
        public const string Update = "delivery.delivery-notes.update";
        public const string Delete = "delivery.delivery-notes.delete";
        public const string UpdateStatus = "delivery.delivery-notes.update-status";
        public const string GeneratePdf = "delivery.delivery-notes.generate-pdf";
    }

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
}
